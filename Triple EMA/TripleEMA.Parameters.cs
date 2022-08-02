using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using System.Collections.Generic;

namespace cAlgo
{
    public partial class TripleEMA
    {
        #region Parameters

        #region General Parameters

        [Parameter("Source", DefaultValue = "Close", Group = "General")]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 7, Step = 1, Group = "Slope Average Parameters")]
        public int AngleThreshold { get; set; }

        [Parameter(DefaultValue = 5, MinValue = 1, Step = 1, Group = "Slope Average Parameters")]
        public int AngleAveragePeriods { get; set; }

        [Parameter(DefaultValue = MovingAverageType.Exponential, Group = "Slope Average Parameters")]
        public MovingAverageType OverallSlopeAverageMAType { get; set; }

        #endregion



        #region Tuning Parameters

        [Parameter("Cloud pip distance. Increase or decrease proportional to spread.", DefaultValue = 2.7)]
        public double PipDistance { get; set; }

        [Parameter(DefaultValue = 6, Step = 0.1)]
        public double MedEMAEpsilonPercent { get; set; }

        [Parameter(DefaultValue = 2.5, Step = 0.1)]
        public double FastEMAEpsilonPercent { get; set; }

        [Parameter("Min bars needed after crossing slow EMA, and has closed outside zone", DefaultValue = 3)]
        public int MinBarsAfterClose { get; set; }

        [Parameter("Min bars needed after crossing slow EMA, but hasn't closed outside zone", DefaultValue = 7)]
        public int MinBarsAfterCross { get; set; }

        [Parameter(DefaultValue = 7)]
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

        [Parameter(DefaultValue = MovingAverageType.Simple, Group = "Fast MA Parameters")]
        public MovingAverageType FastSlopeAverageMAType { get; set; }

        [Parameter(DefaultValue = 50, Step = 1, Group = "Med MA Parameters")]
        public int MedMAPeriods { get; set; }

        [Parameter(DefaultValue = 5, Step = 1, Group = "Med MA Parameters")]
        public int MedMAAveragePeriods { get; set; }

        [Parameter(DefaultValue = 1.4, Step = 0.1, Group = "Med MA Parameters")]
        public double MedMAAverageWeighting { get; set; }

        [Parameter(DefaultValue = MovingAverageType.Simple, Group = "Med MA Parameters")]
        public MovingAverageType MedSlopeAverageMAType { get; set; }

        [Parameter(DefaultValue = 100, Step = 1, Group = "Slow MA Parameters")]
        public int SlowMAPeriods { get; set; }

        [Parameter(DefaultValue = 2, Step = 1, Group = "Slow MA Parameters")]
        public int SlowMAAveragePeriods { get; set; }

        [Parameter(DefaultValue = 1.8, Step = 0.1, Group = "Slow MA Parameters")]
        public double SlowMAAverageWeighting { get; set; }

        [Parameter(DefaultValue = MovingAverageType.Simple, Group = "Slow MA Parameters")]
        public MovingAverageType SlowSlopeAverageMAType { get; set; }

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

        [Output("Take Profit 1", LineColor = "#9C61C7", Thickness = 2, PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries TakeProfitLine1 { get; set; }

        [Output("Fast EMA open position line", LineColor = "#02AFF1", Thickness = 1.5f, PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries FastEMAOpenPositionLine { get; set; }

        [Output("Fast EMA TP1", LineColor = "#2FFF22", Thickness = 2, PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries FastEMATakeProfitLine1 { get; set; }

        [Output("Inner Epsilon Line", LineColor = "Green", Thickness = 1, PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries InnerEpsilonLine { get; set; }

        [Output("Outer Epsilon Line", LineColor = "Green", Thickness = 1, PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries OuterEpsilonLine { get; set; }

        #endregion
    }
}
