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

        #region Parameters

        #region General Parameters

        [Parameter("Source", DefaultValue = "Close", Group = "General")]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 7, Step = 1, Group = "General")]
        public int AngleThreshold { get; set; }

        [Parameter(DefaultValue = 5, MinValue = 1, Step = 1, Group = "General")]
        public int AngleAveragePeriods { get; set; }

        [Parameter("Moving average type", DefaultValue = MovingAverageType.Exponential, Group = "General")]
        public MovingAverageType MAType { get; set; }

        #endregion




        #region Tuning Parameters

        [Parameter("Cloud pip distance. Increase or decrease proportional to spread.", DefaultValue = 2.7)]
        public double PipDistance { get; set; }

        [Parameter("Pip buffer % to trigger 50ema trades", DefaultValue = 6, Step = 0.1)]
        public double InnerEpsilonPercent { get; set; }

        [Parameter("Pip buffer zone % to trigger 25ema trades", DefaultValue = 2.5, Step = 0.1)]
        public double OuterEpsilonPercent { get; set; }

        [Parameter("Min bars needed after crossing slow EMA, and has closed outside zone", DefaultValue = 3)]
        public int MinBarsAfterClose { get; set; }

        [Parameter("Min bars needed after crossing slow EMA, but hasn't closed outside zone", DefaultValue = 7)]
        public int MinBarsAfterCross { get; set; }

        [Parameter("Minimum bars zone is open to count as trend", DefaultValue = 7)]
        public int MinimumBarsForTrend { get; set; }

        [Parameter("Minimum % of total EMA distance that fast to medium EMA needs to be", DefaultValue = 13)]
        public double MinimumFirstZonePercent { get; set; }

        [Parameter("Minimum % of total EMA distance that medium to slow EMA needs to be", DefaultValue = 13)]
        public double MinimumSecondZonePercent { get; set; }

        #endregion




        #region MA Parameters

        [Parameter(DefaultValue = 25, Step = 1, Group = "Fast MA Parameters")]
        public int FastMAPeriods { get; set; }

        [Parameter(DefaultValue = 18, Step = 1, Group = "Fast MA Parameters")]
        public int FastMAAveragePeriods { get; set; }

        [Parameter(DefaultValue = 1, Step = 0.1, Group = "Fast MA Parameters")]
        public double FastMAAverageWeighting { get; set; }

        [Parameter(DefaultValue = 50, Step = 1, Group = "Med MA Parameters")]
        public int MedMAPeriods { get; set; }

        [Parameter(DefaultValue = 5, Step = 1, Group = "Med MA Parameters")]
        public int MedMAAveragePeriods { get; set; }

        [Parameter(DefaultValue = 1.4, Step = 0.1, Group = "Med MA Parameters")]
        public double MedMAAverageWeighting { get; set; }

        [Parameter(DefaultValue = 100, Step = 1, Group = "Slow MA Parameters")]
        public int SlowMAPeriods { get; set; }

        [Parameter(DefaultValue = 2, Step = 1, Group = "Slow MA Parameters")]
        public int SlowMAAveragePeriods { get; set; }

        [Parameter(DefaultValue = 1.8, Step = 0.1, Group = "Slow MA Parameters")]
        public double SlowMAAverageWeighting { get; set; } 

        #endregion

        #endregion

        #region Output Lines

        [Output("Fast EMA", LineColor = "Turquoise", Thickness = 2)]
        public IndicatorDataSeries Result1 { get; set; }

        [Output("Medium EMA", LineColor = "Black", Thickness = 2)]
        public IndicatorDataSeries Result2 { get; set; }

        [Output("Slow EMA", LineColor = "Red", Thickness = 2)]
        public IndicatorDataSeries Result3 { get; set; }

        [Output("Fast Cloud", LineColor = "Transparent", PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries FastCloud { get; set; }

        //50% alpha red
        [Output("Slow Cloud", LineColor = "Transparent", PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries SlowCloud { get; set; }

        [Output("Fast Cloud Full", LineColor = "Transparent", PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries FastCloudFull { get; set; }

        //50% alpha red
        [Output("Slow Cloud Full", LineColor = "Transparent", PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries SlowCloudFull { get; set; }

        [Output("Fast Cloud In Zone", LineColor = "Transparent", PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries FastCloudInZone { get; set; }

        [Output("Slow Cloud In Zone", LineColor = "Transparent", PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries SlowCloudInZone { get; set; }

        [Output("Take Profit 1", LineColor = "#00E08A", Thickness = 2,PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries TakeProfitLine1 { get; set; }

        [Output("Inner Epsilon Line", LineColor = "Green", Thickness = 1, PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries InnerEpsilonLine { get; set; }

        [Output("Outer Epsilon Line", LineColor = "Green", Thickness = 1, PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries OuterEpsilonLine { get; set; }

        [Output("EMA Distance", LineColor = "Transparent")]
        public IndicatorDataSeries EMADistanceOutput { get; set; }

        [Output("Inner Zone Percentage", LineColor = "Transparent")]
        public IndicatorDataSeries InnerZonePercentOutput { get; set; }

        #endregion

        #region Internal Indicators

        public ExponentialMovingAverage FastEMA { get; private set; }
        public ExponentialMovingAverage MedEMA { get; private set; }
        public ExponentialMovingAverage SlowEMA { get; private set; }

        public TripleMASlopeAverage TripleMASlopeAverage { get; set; }

        #endregion

        #region Calc values

        public double EMADistance { get; set; }
        public bool ConditionMet { get; set; }
        public TrendDirection Direction { get; set; }
        private bool CrossedSlowEMA { get; set; }
        private bool ClosedAfterCross { get; set; }
        private int TrendCount { get; set; }
        public bool IsTrending { get { return TrendCount >= MinimumBarsForTrend; } }

        public double Price { get { return Direction == TrendDirection.Up ? Symbol.Ask : Symbol.Bid; } }

        public bool TrendingUp
        {
            get { return ConditionMet && Direction == TrendDirection.Up; }
        }
        public bool TrendingDown
        {
            get { return ConditionMet && Direction == TrendDirection.Down; }
        }

        private Func<int, bool> CrossedSlowEMADown { get { return (index) => Bars[index].High >= Result3[index]; }  }
        private Func<int, bool> CrossedSlowEMAUp { get { return (index) => Bars[index].Low <= Result3[index]; }  }


        private int BarCrossedIndex { get; set; }

        private List<IndicatorDataSeries> ZoneOutputs { get; set; } 

        #endregion

        protected override void Initialize()
        {
            FastEMA = Indicators.ExponentialMovingAverage(Source, FastMAPeriods);
            MedEMA = Indicators.ExponentialMovingAverage(Source, MedMAPeriods);
            SlowEMA = Indicators.ExponentialMovingAverage(Source, SlowMAPeriods);
            TripleMASlopeAverage = Indicators.GetIndicator<TripleMASlopeAverage>(Source, AngleThreshold, AngleAveragePeriods,
                                                                                 MAType, FastMAPeriods, FastMAAveragePeriods,
                                                                                 FastMAAverageWeighting, MedMAPeriods, MedMAAveragePeriods,
                                                                                 MedMAAverageWeighting, SlowMAPeriods, SlowMAAveragePeriods,
                                                                                 SlowMAAverageWeighting);
            ZoneOutputs = new List<IndicatorDataSeries>
            {
                FastCloud, FastCloudFull, FastCloudInZone,
                SlowCloud, SlowCloudFull, SlowCloudInZone,
                TakeProfitLine1,
                InnerEpsilonLine, OuterEpsilonLine
            };
            //CrossedSlowEMADown = (index) => Bars[index].High >= Result3[index];
            //CrossedSlowEMAUp = (index) => Bars[index].Low <= Result3[index];


            Bars.BarOpened += Bars_BarOpened;

        }

        private void Bars_BarOpened(BarOpenedEventArgs obj)
        {
            //So in here want to have functionality that is checked on bar close/open

            //new bar is opened.
            //so we care about the previous bar, right?
            var prevBarIndex = Bars.Count - 2;
            if (CoolingDownAfterCrossSlowEMA(prevBarIndex, true))
            {
                return;
            }

            if (EMAZoneIsOpen(prevBarIndex))
            {
                TrendCount++;
                DrawZone(prevBarIndex);
                //if switch colour from previous index, then set previous index, and remove the previous cloud one
                //only do that if it changes.
                //e.g. if price goes in zone, then we change from blue to green. but don't want to be "changing" it 
                //on every tick. so check if the current cloud has changed from the previous one.
            }
            else
            {
                TrendCount = 0;
            }


            //so what you're saying is, that when doing calculate on tick, condition can be true and false each tick.
            //
            //open positions if bar close outside zone
            //increment trendcounter
            //kinda care about waiting for bar to close outside zone in here to be honest, cus in calcualte it calls on tick
            //but this won't call for historical bars... so problem
            //would want to have the same method called in here as well as in there, right?
            //do if IsLastBar?
        }


        #region Calculations
        public override void Calculate(int index)
        {
            //Print("In calculate");
            Result1[index] = FastEMA.Result[index];
            Result2[index] = MedEMA.Result[index];
            Result3[index] = SlowEMA.Result[index];

            EMADistanceOutput[index] = (FastEMA.Result[index] - SlowEMA.Result[index]) / Symbol.PipSize;
            var firstZoneDistance = FastEMA.Result[index] - MedEMA.Result[index];
            InnerZonePercentOutput[index] = 100 * firstZoneDistance / Symbol.PipSize / EMADistanceOutput[index];

            //increment trendcount if not lastbar cus then we know.

            //EMADistance is 0 when the fast and slow ema are closer than the PipDistance
            if (CoolingDownAfterCrossSlowEMA(index))
            {
                return;
            }

            var zoneOpen = EMAZoneIsOpen(index);

            //if zone open and is last bar, don't increment trend count, but draw zone
            //if zone open and not last bar, increment trendcount and draw zone
            //if zone is not open and is last bar, don't reset trendcount, don't draw zone
            //if zone is not open and is not last bar, reset trendcount, don't draw zone

            //if zone is open, draw zone
            //if zone is closed, don't draw zone
            //if is last bar, don't touch trend count (basically only care if it's a whole bar)
            //if is not last bar, do touch trendcount
            if (zoneOpen)
            {
                DrawZone(index);

                if (!IsLastBar)
                {
                    ++TrendCount;
                    return;
                }

                DrawText(index);
            }
            else
            {
                ClearZone(index);
                if (!IsLastBar)
                {
                    TrendCount = 0;
                    return;
                }

                //DrawText(index);
            }

        }
        private bool HasCrossedSlowEMA(int index)
        {
            //TODO: refactor later
            return IsTrending && (TrendingUp && CrossedSlowEMAUp(index)
                    || TrendingDown && CrossedSlowEMADown(index));
        }

        private void StartCoolDown(int index)
        {
            CrossedSlowEMA = true;
            TrendCount = 0;
            BarCrossedIndex = index;
            ConditionMet = false;
        }

        private double PriceWick(int index)
        {
            if (IsLastBar)
                return Price;
            else
            {
                return Direction == TrendDirection.Up ? Bars[index].Low : Bars[index].High;
            }
        }

        private bool CrossedZone(int index)
        {
            var dir = (int)Direction;
            return dir * (WickCross(index) - Result3[index]) <= 0;
        }

        private double WickCross(int index)
        {
            return Direction == TrendDirection.Up ? Bars[index].Low : Bars[index].High;
        }

        private bool PriceIsInZone(int index)
        {
            var price = PriceWick(index);
            var dir = (int)Direction;

            return dir * (price - OuterEpsilonLine[index]) <= 0; //Direction = -1
        }


        private IndicatorDataSeries PrevFastCloud { get; set; }
        private IndicatorDataSeries PrevSlowCloud { get; set; }
        /// <summary>
        /// Set values for the clouds
        /// </summary>
        /// <param name="index"></param>
        private void DrawZone(int index)
        {
            TakeProfitLine1[index] = MedEMA.Result[index] + (FastEMA.Result[index] - MedEMA.Result[index]) * 2;

            InnerEpsilonLine[index] = MedEMA.Result[index] + EMADistanceOutput[index] * (InnerEpsilonPercent * Symbol.PipSize) / 100;
            OuterEpsilonLine[index] = FastEMA.Result[index] + EMADistanceOutput[index] * (OuterEpsilonPercent * Symbol.PipSize) / 100;
            
            var priceIsInZone = PriceIsInZone(index);

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
                PrevFastCloud[index] = FastEMA.Result[index];
                PrevSlowCloud[index] = SlowEMA.Result[index];
                PrevFastCloud = fastCloud;
                PrevSlowCloud = slowCloud;
            }

            fastCloud[index] = FastEMA.Result[index];

            slowCloud[index] = SlowEMA.Result[index];
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
            var stopLoss = EMADistanceOutput[index] / 2;
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
            var volForMargin = getVolumeForMargin(Symbol.Name, 300, Account.Asset.Name, Symbol.DynamicLeverage[0].Leverage, TrendingUp);
            var lotsForMargin = volForMargin / Symbol.LotSize;
            var requiredMargin = calculateMargin(Symbol.Name, riskResult / Symbol.LotSize, Account.Asset.Name, Symbol.DynamicLeverage[0].Leverage, TrendingUp);

            var Text = "Index: " + index + "\n";
            Text += "TrendCount: " + TrendCount + "\n";
            Text += "Symbol: " + Symbol.Name + "\n";
            Text += "Trade Size(Lots) : " + riskResult / Symbol.LotSize + "\n";
            Text += "Trade Size(Units) : " + riskResult + "\n";
            Text += "Desired Risk Amount : " + riskAmount + "\n";
            Text += "Calculated Risk Amount : " + Symbol.NormalizeVolumeInUnits(riskUnits, RoundingMode.Down) * riskPerUnit + "\n";
            Text += "Stop Loss Pips : " + stopLoss + "\n";
            Text += "EMADistance : " + EMADistanceOutput[index] + "\n";
            Text += "Inner Zone % : " + InnerZonePercentOutput[index] + "\n";
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
            if (Direction == TrendDirection.Up)
            {
                return Bars[index].Close >= Result1[index];
            }
            else if (Direction == TrendDirection.Down)
            {
                return Bars[index].Close <= Result1[index];
            }

            return false;
        }

        private bool CoolingDownAfterCrossSlowEMA(int index, bool barOpen = false)
        {
            //TODO: do thing where if crosses slow ema before closing outside zone, reset cooldown.
            //TODO: in order to do this, kinda have two states, where first is after it has crossedslow ema,
            //then after it has crossed back we start counting. If it crosses back again we reset the count
            if (HasCrossedSlowEMA(index))
            {
                StartCoolDown(index);
                //check trending up and crossedemaup, and vice versa
                //if either is true, then set CrossedSlowEMA to true
            }
            else if (CrossedSlowEMA)
            {
                //okay so basically, after we have crossedEMA, we have 3 conditions to set it to false
                //1. Has closed back outside of zone at least once, and 3 (or can change it) bars have passed
                //2. 10 bars have passed
                //3. 25ema crossed the 100ema in relevant direction.

                //to check if closedoutside zone, just check if close is above/below fastema

                //TODO: restart cooldown after every slow ema touch if cooldown is still expiring.
                CoolDownAfterCrossedSlowEMA(index, barOpen);

            }
            // WHat happens here?
            //So a flag gets set, and it gets checked here, right?
            //Yes. CrossedSlowEMA

            //Cooldown method. 3 params. wait for closeoutsidezone, with/without X bars, or X bars total.


            //SetCoolDown(true, 3, 10)

            //if CoolingDown()

            //template/generic method that is parameterized for each cooldown type.

            //perhaps need one for positions and one for the indicator
            //
            //returns false if we ARE not waiting.

            //how do we know if we're waiting?

            //What's the conditions?

            //So we were trending, and price touched 100 ema, so we basically
            //want to have a "cooldown".

            //after price has touched 100ema, we wait til price CLOSES outside 
            //of the zone
            //or wait X bars, or wait til 25 crosses 50.

            return CrossedSlowEMA;

        }

        private void CoolDownAfterCrossedSlowEMA(int index, bool barOpen)
        {
            ClosedAfterCross = !ClosedAfterCross && barOpen ? ClosedOutsideZone(index) : ClosedAfterCross;

            var barsAfterCross = index - BarCrossedIndex;
            if (ClosedAfterCross && barsAfterCross >= MinBarsAfterClose
                || FastCrossedSlowEMA(index) || barsAfterCross >= MinBarsAfterCross)
            {
                //cooldown finish, reset flags.
                CrossedSlowEMA = false;
                ClosedAfterCross = false;
                BarCrossedIndex = -1;

                 
                Direction = TrendDirection.Straight;
            }
        }

        /// <summary>
        /// Returns true if it is trending
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool EMAZoneIsOpen(int index)
        {
            //TODO: change trend conditions to properly terminate zone when it crosses slowEMA on the LAST BAR
            //TODO: so basically, if it's last bar, and bid/ask crosses slowEMA, then we go on cooldown.
            //can set CrossedSlowEMA or do something else.

            //So at beginning, TrendCount is 0.
            //Okay so we just got the trend direction.
            //TrendCount is incremented when the zone is open. simple.

            //|a - c| = 100
            //A >= 4B or vice versa? or is it and?
            //So neither distance smaller than percentage/ratio/whatever

            //DOWN
            /*return Result1[index] - Result2[index] < 0
            && Result2[index] - Result3[index] < 0
            && (IsLastBar ? Symbol.Bid : Source[index]) <= Result3[index]
            && Bars[index].High <= Result3[index];*/
            //close hasn't crossed slowema
            //for historical bars, we don't care if it closed inside zone or not, we just don't want the wick to cross it.
            //if wick crosses it doesn't matter if close or not.

            //high/low hasn't crossed slow ema
            var emaDirection =  GetEMADirection(index);
            if (CrossedZone(index))
                return ConditionMet = false;
            //DOWN high cross slowema

            // 6 - 5 >= 0
            // 5 - 5 >= 0
            // 4 - 5 <= 0
            //Bars[index].High - Result3[index] >= 0
            //UP low cross slowema
            //Bars[index].Low - Result3[index] <= 0

            var emaDistance = (double)emaDirection * EMADistanceOutput[index];

            var firstZoneDistance = FastEMA.Result[index] - MedEMA.Result[index];

            var distancePercent = 100 * firstZoneDistance / Symbol.PipSize / EMADistanceOutput[index];

            var withinRatio = distancePercent >= MinimumFirstZonePercent && (100 - distancePercent) >= MinimumSecondZonePercent;

            ConditionMet = emaDistance >= PipDistance && withinRatio;

            return ConditionMet;
        }

        private TrendDirection GetEMADirection(int index)
        {
            var emasDisjoint = EMAsDisjoint(index);

            if (emasDisjoint)
            {
                //get the direction.
                //either use fast - slow, or calculate from EMAsDisjoint
                //is going to ues fast-slow somewhere else then might as well do it here to avoid additional operation, assuming that usage of emasdisjoint and
                //fast - slow is 1 to 1.

                //check close price has not crossed slowema
                //check high price has not crossed slowema

                Direction = EMADistanceOutput[index] > 0 ? TrendDirection.Up : TrendDirection.Down;
                /*return Result1[index] - Result2[index] < 0
               && Result2[index] - Result3[index] < 0
               && (IsLastBar ? Symbol.Bid : Source[index]) <= Result3[index]
               && Bars[index].High <= Result3[index];*/

            }
            else
            {
                Direction = TrendDirection.Straight;
            }

            return Direction;
        }

        private bool FastCrossedSlowEMA(int index)
        {
            if (Direction == TrendDirection.Up)
            {
                return Result1[index] - Result3[index] <= 0;
            }
            else if (Direction == TrendDirection.Down)
            {
                return Result1[index] - Result3[index] >= 0;
            }
            return false;
        }

        private bool EMAsDisjoint(int index)
        {
            // 1 1
            // 1 0 
            // 0 1
            // 0 0
            return (Result1[index] - Result2[index] < 0) == (Result2[index] - Result3[index] < 0);
            //if down or up, then first and second condition signs will be the same
            /*return
                //DOWN
                Result1[index] - Result2[index] < 0
             && Result2[index] - Result3[index] < 0
                //UP
            || Result1[index] - Result2[index] > 0
            && Result2[index] - Result3[index] > 0;*/
        }
        

        private bool TrendDownCondition(int index)
        {
            //bar close or price is inside the zone
            //Crossed100EMADown = (index) => Bars[index].High >= Result3[index];
            //TODO: problem here is that if called on a prehistoric bar, it will still use IsLastBar if it is true, which is not what we want
            //Possible solutions: either pass param


            //Last condition is opposite of crossedslowEMA
            //3rd condition is checking that close price also hasn't crossedSlowEMA
            return Result1[index] - Result2[index] < 0
                && Result2[index] - Result3[index] < 0
                && (IsLastBar ? Symbol.Bid : Source[index]) <= Result3[index] 
                && Bars[index].High <= Result3[index];
            //bar open is also in the zone
        }

        private bool TrendUpCondition(int index)
        {
            //bar close or price is inside the zone
            return Result1[index] > Result2[index] 
                && Result2[index] > Result3[index] 
                && (IsLastBar ? Symbol.Ask : Source[index]) >= Result3[index] 
                && Bars[index].Low >= Result3[index];
            //bar open is also in the zone
        }

        #endregion

    }
}
