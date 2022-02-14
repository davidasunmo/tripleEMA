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

namespace cAlgo
{
    public enum TrendDirection
    {
        Up = 1,
        Straight = 0,
        Down = -1
    }



    [Cloud("Fast Cloud", "Slow Cloud", FirstColor = "Green", SecondColor = "Green")]
    [Cloud("Fast Cloud Full", "Slow Cloud Full", FirstColor = "Blue", SecondColor = "Blue")]
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = false, AccessRights = AccessRights.None)]
    public partial class TripleEMA : Indicator
    {

        #region Parameters

        [Parameter("Source", DefaultValue = "Close", Group = "General")]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 7, Step = 1, Group = "General")]
        public int AngleThreshold { get; set; }

        [Parameter(DefaultValue = 5, MinValue = 1, Step = 1, Group = "General")]
        public int AngleAveragePeriods { get; set; }

        [Parameter("Moving average type", DefaultValue = MovingAverageType.Exponential, Group = "General")]
        public MovingAverageType MAType { get; set; }

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

        [Parameter("Cloud pip distance. Increase or decrease proportional to spread.", DefaultValue = 2.7)]
        public double PipDistance { get; set; }

        [Parameter("Min bars needed after crossing slow EMA, and has closed outside zone", DefaultValue = 3)]
        public int MinBarsAfterClose { get; set; }

        [Parameter("Min bars needed after crossing slow EMA, but hasn't closed outside zone", DefaultValue = 7)]
        public int MinBarsAfterCross { get; set; }

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

        [Output("Take Profit 1", LineColor = "#00E08A", Thickness = 2,PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries TakeProfitLine1 { get; set; }

        [Output("EMA Distance", LineColor = "Transparent")]
        public IndicatorDataSeries EMADistanceOutput { get; set; }

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
        private bool IsTrending { get { return TrendCount >= 3; } }

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
        private void DrawZone(int index)
        {
            //Set values for cloud EMAs

            TakeProfitLine1[index] = MedEMA.Result[index] + (FastEMA.Result[index] - MedEMA.Result[index]) * 2;

            var angleAbove = TripleMASlopeAverage.AngleAbove(index);
            var fastCloud = angleAbove ? FastCloudFull : FastCloud;
            var slowCloud = angleAbove ? SlowCloudFull : SlowCloud;

            fastCloud[index - 1] = FastEMA.Result[index - 1];
            fastCloud[index] = FastEMA.Result[index];

            slowCloud[index - 1] = SlowEMA.Result[index - 1];
            slowCloud[index] = SlowEMA.Result[index];
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
            //currently it's not doing that because trendcount still 0, so is trending is false and so it makes no difference.
            //so basically if it crosses, but then goes straight back out, then crosses it again, we want to reset it.
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
            // Okay so WaitingToCrossEMA is handling the cooldonw stuff.

            //TODO: change trend conditions to properly terminate zone when it crosses slowEMA on the LAST BAR
            //TODO: so basically, if it's last bar, and bid/ask crosses slowEMA, then we go on cooldown.
            //can set CrossedSlowEMA or do something else.

            //So at beginning, TrendCount is 0.
            //Okay so we just got the trend direction.
            //TrendCount is incremented when the zone is open. simple.

            //|a - c| = 100
            //A >= 4B or vice versa? or is it and?
            //So neither distance smaller than percentage/ratio/whatever

            

            var trendDirection =  GetTrendDirection(index);

            var emaDistance = (double)trendDirection * EMADistanceOutput[index];

            ConditionMet = emaDistance >= PipDistance;

            return ConditionMet;
        }

        private TrendDirection GetTrendDirection(int index)
        {
            if (TrendDownCondition(index))
            {
                //Debugger.Break();
                Print("DOWN {0} - Result1: {1}; Result2: {2}; Result3: {3}; Bid/Ask: {4},{5}",
                    index, Result1[index], Result2[index], Result3[index], Symbol.Bid, Symbol.Ask);
                Direction = TrendDirection.Down;
            }
            else if (TrendUpCondition(index))
            {
                //Debugger.Break();
                Print("UP {0} - Result1: {1}; Result2: {2}; Result3: {3}; Bid/Ask: {4},{5}",
                    index, Result1[index], Result2[index], Result3[index], Symbol.Bid, Symbol.Ask);
                Direction = TrendDirection.Up;
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
                return Result1[index] <= Result3[index];
            }
            else if (Direction == TrendDirection.Down)
            {
                return Result1[index] >= Result3[index];
            }
            return false;
        }

        private bool TrendDownCondition(int index)
        {
            //bar close or price is inside the zone
            //Crossed100EMADown = (index) => Bars[index].High >= Result3[index];
            return Result1[index] < Result2[index] 
                && Result2[index] < Result3[index] 
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
