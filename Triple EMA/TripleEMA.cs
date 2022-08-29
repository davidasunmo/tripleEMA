// -------------------------------------------------------------------------------------------------
//
//    This code is a cTrader Automate API example.
//    
//    All changes to this file might be lost on the next application update.
//    If you are going to modify this file please make a copy using the "Duplicate" command.
//
// -------------------------------------------------------------------------------------------------

using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using System.Diagnostics;
using System.Collections.Generic;

namespace cAlgo
{
    public enum TrendDirection
    {
        Up = 1,
        Straight = 0,
        Down = -1
    }



    [Cloud("Fast Cloud", "Slow Cloud", FirstColor = "Red", SecondColor = "Red")]
    [Cloud("Fast Cloud Full", "Slow Cloud Full", FirstColor = "Blue", SecondColor = "Blue")]
    [Cloud("Fast Cloud In Zone", "Slow Cloud In Zone", FirstColor = "#FF01FF01", SecondColor = "#FF01FF01")]
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public partial class TripleEMA : Indicator
    {
        #region Internal Indicators

        public ExponentialMovingAverage FastEma { get; private set; }
        public ExponentialMovingAverage MedEma { get; private set; }
        public ExponentialMovingAverage SlowEma { get; private set; }

        //TODO: optimize these later
        public ListDefault InnerEpsilon { get; private set; }

        public ListDefault OuterEpsilon { get; private set; }

        public ListDefault EMADistanceValues { get; private set; }

        public ListDefault ZonePercentValues { get; private set; }

        public TripleMASlopeAverage TripleMASlopeAverage { get; set; }

        #endregion

        #region Calc values

        public bool ConditionMet { get; set; }
        public bool CanTrade
        {
            get { return IsTrending && ConditionMet && TripleMASlopeAverage.AngleAbove(Bars.Count - 1); }
        }
        public TrendDirection Direction { get; set; }
        private bool CoolingDown { get; set; }
        private bool ClosedAfterCross { get; set; }
        private bool WasTrending { get; set; }
        public int TrendCount { get; set; }
        private int RestartTrendCount { get; set; }
        public bool IsTrending
        {
            get { return TrendCount >= MinimumBarsForTrend; }
        }

        public double Price
        {
            get { return Direction == TrendDirection.Up ? Symbol.Ask : Symbol.Bid; }
        }

        public bool TrendingUp
        {
            get { return ConditionMet && Direction == TrendDirection.Up; }
        }
        public bool TrendingDown
        {
            get { return ConditionMet && Direction == TrendDirection.Down; }
        }

        private int BarCrossedIndex { get; set; }

        private List<IndicatorDataSeries> ZoneOutputs { get; set; }

        #endregion

        protected override void Initialize()
        {
            InitRateLookups();

            InnerEpsilon = new ListDefault();
            OuterEpsilon = new ListDefault();
            EMADistanceValues = new ListDefault();
            ZonePercentValues = new ListDefault();
            FastEma = Indicators.ExponentialMovingAverage(Source, FastMAPeriods);
            MedEma = Indicators.ExponentialMovingAverage(Source, MedMAPeriods);
            SlowEma = Indicators.ExponentialMovingAverage(Source, SlowMAPeriods);

            TripleMASlopeAverage = Indicators.GetIndicator<TripleMASlopeAverage>(Source, AngleThreshold, AngleAveragePeriods, OverallSlopeAverageMAType, FastMAPeriods, FastMAAveragePeriods, FastMAAverageWeighting, FastSlopeAverageMAType, MedMAPeriods, MedMAAveragePeriods,
            MedMAAverageWeighting, MedSlopeAverageMAType, SlowMAPeriods, SlowMAAveragePeriods, SlowMAAverageWeighting, SlowSlopeAverageMAType);
            
            ZoneOutputs = new List<IndicatorDataSeries> 
            {
                FastCloud,
                FastCloudFull,
                FastCloudInZone,
                SlowCloud,
                SlowCloudFull,
                SlowCloudInZone,
                TakeProfitLine1,
                FastEMAOpenPositionLine,
                FastEMATakeProfitLine1,
                InnerEpsilonLine,
                OuterEpsilonLine
            };

            Bars.BarOpened += Bars_BarOpened;
        }

        private void Bars_BarOpened(BarOpenedEventArgs obj)
        {
            //new bar is opened.
            //so we care about the previous bar, right?
            var prevBarIndex = Bars.Count - 2;

            if (CoolingDown)
            {
                CooldownAfterCrossedSlowEma(prevBarIndex);
            }
            else if (IsConditionMet(prevBarIndex))
            {
                TrendCount++;
                DrawZone(prevBarIndex);
            }
            else
            {
                TrendCount = 0;
            }
        }


        #region cTrader Events
        public override void Calculate(int index)
        {
            FastEmaOutput[index] = FastEma.Result[index];
            MedEmaOutput[index] = MedEma.Result[index];
            SlowEmaOutput[index] = SlowEma.Result[index];

            EMADistanceValues[index] = (FastEma.Result[index] - SlowEma.Result[index]) / Symbol.PipSize;

            InnerEpsilon[index] = MedEma.Result[index] + EMADistanceValues[index] * (MedEMAEpsilonPercent * Symbol.PipSize) / 100;
            OuterEpsilon[index] = FastEma.Result[index] + EMADistanceValues[index] * (FastEMAEpsilonPercent * Symbol.PipSize) / 100;
            var firstZoneDistance = FastEma.Result[index] - MedEma.Result[index];
            ZonePercentValues[index] = 100 * firstZoneDistance / Symbol.PipSize / EMADistanceValues[index];

            Direction = CalculateDirection(index);

            if (Direction == TrendDirection.Straight)
            {
                ConditionMet = false;

                if (CoolingDown)
                {
                    StopCooldown();
                }

                if (!IsLastBar)
                {
                    TrendCount = 0;
                }
            }
            else if (HasCrossedSlowEma(index))
            {
                ClearZone(index);
                //If trending, then enter cooldown. leave trace that we entered cooldown while we were trending

                if (TrendCount > 0 || IsConditionMet(index))
                {
                    //Start cool down
                    StartCooldown(index);
                }
                else if (WasTrending)
                {
                    StartCooldown(index);
                }

                //Check cooldown stuff here

                //only cooldown in here if we were already trending, or already on a cooldown

                //only stop cooldown if fast has crossed slow ema, or if number of bars

            }
            else if (CoolingDown)
            {
                //Do cooldown stuff in here


                //stop cooldown condition
                //if X bars have passed and it has closed outside the zone then we can stop cooldown
                //if we wait only for close outside

                //really only care about onBar, so I think in here if it's last bar we can just ignore it?

                if (!IsLastBar)
                {
                    //historical data here, so can treat it as onBar.
                    //If not, then don't do anything here
                    CooldownAfterCrossedSlowEma(index);
                }
            }
            else
            {
                var zoneOpen = IsConditionMet(index);
                if (zoneOpen)
                {
                    DrawZone(index);

                    if (!IsLastBar)
                    {
                        ++TrendCount;
                    }
                    else
                    {
                        DrawText(index);
                    }

                }
                else
                {
                    ClearZone(index);
                    if (!IsLastBar)
                    {
                        TrendCount = 0;
                    }
                }
            }

            //if zone open and is last bar, don't increment trend count, but draw zone
            //if zone open and not last bar, increment trendcount and draw zone
            //if zone is not open and is last bar, don't reset trendcount, don't draw zone
            //if zone is not open and is not last bar, reset trendcount, don't draw zone

            //if zone is open, draw zone
            //if zone is closed, don't draw zone
            //if is last bar, don't touch trend count (basically only care if it's a whole bar)
            //if is not last bar, do touch trendcount
        }


        private double PriceWick(int index)
        {
            if (IsLastBar && index == Bars.Count - 1)
                return Price;
            else
            {
                return Direction == TrendDirection.Up ? Bars[index].Low : Bars[index].High;
            }
        }

        public bool HasCrossedSlowEma(int index)
        {
            return PriceCrossedLineAgainstDirection(index, SlowEma.Result);
        }

        public bool HasCrossedMedEma(int index)
        {
            return PriceCrossedLineAgainstDirection(index, MedEma.Result);

        }

        private double WickCross(int index)
        {
            return Direction == TrendDirection.Up ? Bars[index].Low : Bars[index].High;
        }

        private bool PriceCrossedZoneLine(int index, DataSeries series)
        {
            var price = PriceWick(index);
            var dir = (int)Direction;

            return dir * (price - series[index]) <= 0;
        }

        public bool PriceInOuterZone(int index)
        {
            return PriceCrossedLineAgainstDirection(index, OuterEpsilon);
        }

        public bool PriceInInnerZone(int index)
        {
            return PriceCrossedLineAgainstDirection(index, InnerEpsilon);
        }


        //TODO: put elsewhere
        public double EMADistance(int index)
        {
            return Math.Abs(EMADistanceValues[index]);
        }

        public double OuterZoneDistance(int index)
        {
            return FastEma.Result[index] - MedEma.Result[index];
        }

        private IndicatorDataSeries PrevFastCloud { get; set; }
        private IndicatorDataSeries PrevSlowCloud { get; set; }

        /// <summary>
        /// Set values for the clouds
        /// </summary>
        /// <param name="index"></param>
        private void DrawZone(int index)
        {
            var outerZoneDistance = FastEma.Result[index] - MedEma.Result[index];

            TakeProfitLine1[index] = MedEma.Result[index] + outerZoneDistance * 2;
            FastEMAOpenPositionLine[index] = MedEma.Result[index] + outerZoneDistance * 1.5;
            FastEMATakeProfitLine1[index] = MedEma.Result[index] + outerZoneDistance * 3;
            InnerEpsilonLine[index] = InnerEpsilon[index];
            OuterEpsilonLine[index] = OuterEpsilon[index];

            var priceIsInZone = PriceInOuterZone(index);

            var angleAbove = TripleMASlopeAverage.AngleAbove(index);
            var fastCloud = !angleAbove ? FastCloud : priceIsInZone ? FastCloudInZone : FastCloudFull;
            var slowCloud = !angleAbove ? SlowCloud : priceIsInZone ? SlowCloudInZone : SlowCloudFull;

            if (PrevFastCloud == null)
            {
                PrevFastCloud = fastCloud;
                PrevSlowCloud = slowCloud;
            }
            else if (PrevFastCloud != fastCloud && double.IsNaN(PrevFastCloud[index]))
            {
                //switch
                PrevFastCloud[index] = FastEma.Result[index];
                PrevSlowCloud[index] = SlowEma.Result[index];
                PrevFastCloud = fastCloud;
                PrevSlowCloud = slowCloud;
            }

            fastCloud[index] = FastEma.Result[index];

            slowCloud[index] = SlowEma.Result[index];
        }

        private void ClearZone(int index)
        {
            foreach (var output in ZoneOutputs)
            {
                output[index] = double.NaN;
            }
        }

        private void DrawText(int index)
        {
            var stopLoss = EMADistanceValues[index] / 2;
            //generally just the 50ma, right?
            var riskAmount = Account.Balance * 0.005;
            var riskPerUnit = stopLoss * Symbol.PipValue;
            var riskUnits = riskAmount / riskPerUnit;

            //risk 0.5% or 300 margin, whichever is smaller.

            //Keep track of where stoploss is for 50ema position.
            //if 50 to 100ema is a lot bigger than 25 to 50 ema, then sl can be that big cus it indicates a big trend which is what you want,
            //but tp should be different, because the 25ema gap can be small while the 100ema gap can be big.
            //so if some stronk pullback happens and 25ema gap squishes, and it touches the 50ema, let's say the gap is 0.8 pips, but the 
            //100ema gap is like 3.8 pips. So the SL would be 3.8 pips, but the tp would be 0.8*2.5 = 2pips. not a 1:1 RR, but who cares.
            //TODO: figure out where to place SL and TP for 50EMA scenario, and how to do trailing stop loss.


            var riskResult = VolForRiskPercentage(Symbol, stopLoss, 0.5);
            var volForMargin = GetVolumeForMargin(Symbol.Name, 300, Account.Asset.Name, Symbol.DynamicLeverage[0].Leverage, TrendingUp);
            var lotsForMargin = volForMargin / Symbol.LotSize;
            var requiredMargin = CalculateMargin(Symbol.Name, riskResult / Symbol.LotSize, Account.Asset.Name, Symbol.DynamicLeverage[0].Leverage, TrendingUp);

            var Text = "Index: " + index + "\n";
            Text += "TrendCount: " + TrendCount + "\n";
            Text += "Symbol: " + Symbol.Name + "\n";
            Text += "Trade Size(Lots) : " + riskResult / Symbol.LotSize + "\n";
            Text += "Trade Size(Units) : " + riskResult + "\n";
            Text += "Desired Risk Amount : " + riskAmount + "\n";
            Text += "Calculated Risk Amount : " + Symbol.NormalizeVolumeInUnits(riskUnits, RoundingMode.Down) * riskPerUnit + "\n";
            Text += "Stop Loss Pips : " + stopLoss + "\n";
            Text += "EMADistance : " + EMADistanceValues[index] + "\n";
            Text += "Inner Zone % : " + ZonePercentValues[index] + "\n";
            Text += "Margin for 0.5%: " + requiredMargin + "\n";
            Text += "Lots for £300: " + lotsForMargin + "\n";
            Text += "Vol for £300: " + volForMargin + "\n";

            Chart.DrawStaticText("Details", Text, VerticalAlignment.Top, HorizontalAlignment.Left, Color.DarkBlue);
        }



        /// <summary>
        /// Method that returns true if the current bars close price is outside of the zone.
        /// I.e. above/below the fast EMA, depending on the trend direction.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool ClosedOutsideZone(int index)
        {
            var dir = (int)Direction;
            var price = index == Bars.Count - 1 ? Price : Bars[index].Close;

            return dir * (price - FastEmaOutput[index]) >= 0;
        }


        private void StartCooldown(int index)
        {
            CoolingDown = true;
            RestartTrendCount = TrendCount;
            TrendCount = 0;
            BarCrossedIndex = index;
            ConditionMet = false;
        }

        public void CoolDown()
        {
            StartCooldown(Bars.Count - 1);
        }

        public void CancelCoolDown()
        {
            CoolingDown = false;
            ClosedAfterCross = false;
            TrendCount = RestartTrendCount + (Bars.Count - 1 - BarCrossedIndex);
            BarCrossedIndex = -1;
        }

        private void StopCooldown()
        {
            CoolingDown = false;
            ClosedAfterCross = false;
            WasTrending = false;
            BarCrossedIndex = -1;
        }

        private void CooldownAfterCrossedSlowEma(int index)
        {
            var barsAfterCross = index - BarCrossedIndex;
            ClosedAfterCross = !ClosedAfterCross ? ClosedOutsideZone(index) : ClosedAfterCross;

            if (ClosedAfterCross && barsAfterCross >= MinBarsAfterClose || !WaitForCloseAfterCooldown && barsAfterCross >= MinBarsAfterCross)
            {
                //cooldown finish, reset flags.
                StopCooldown();
                Direction = TrendDirection.Straight;
            }
        }

        private bool IsConditionMet(int index)
        {
            var emaDistance = (double)Direction * EMADistanceValues[index];
            var fastEmaZoneDistancePips = emaDistance * ZonePercentValues[index] / 100;

            var withinRatio = IsWithinZoneRatio(index);

            ConditionMet = emaDistance >= PipDistance && fastEmaZoneDistancePips >= MinFastEmaZonePips && withinRatio;

            return ConditionMet;
        }

        private bool IsWithinZoneRatio(int index)
        {
            var distancePercent = ZonePercentValues[index];

            var withinRatio = distancePercent >= MinimumFirstZonePercent && (100 - distancePercent) >= MinimumSecondZonePercent;

            return withinRatio;
        }

        private TrendDirection CalculateDirection(int index)
        {
            TrendDirection direction;

            if (EMAsDisjoint(index))
            {
                //get the direction.
                //either use fast - slow, or calculate from EMAsDisjoint
                //is going to ues fast-slow somewhere else then might as well do it here to avoid additional operation, assuming that usage of emasdisjoint and
                direction = EMADistanceValues[index] > 0 ? TrendDirection.Up : TrendDirection.Down;
            }
            else
            {
                direction = TrendDirection.Straight;
            }

            return direction;
        }

        private bool FastCrossedSlowEMA(int index)
        {
            if (Direction == TrendDirection.Up)
            {
                return FastEmaOutput[index] - SlowEmaOutput[index] <= 0;
            }
            else if (Direction == TrendDirection.Down)
            {
                return FastEmaOutput[index] - SlowEmaOutput[index] >= 0;
            }
            return false;
        }

        private bool EMAsDisjoint(int index)
        {
            // 1 1
            // 1 0 
            // 0 1
            // 0 0
            return (FastEmaOutput[index] - MedEmaOutput[index] < 0) == (MedEmaOutput[index] - SlowEmaOutput[index] < 0);
            //if down or up, then first and second condition signs will be the same
        }
    }

    #endregion

}
