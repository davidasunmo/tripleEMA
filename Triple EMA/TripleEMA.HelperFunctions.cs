using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo
{
    public partial class TripleEMA
    {
        #region Helper Functions

        private double symbolRate(string symbolCode, bool isBid = true)
        {
            if (Symbols.Exists(symbolCode))
            {
                var symbol = Symbols.GetSymbol(symbolCode);

                switch (isBid)
                {
                    case true: return symbol.Bid;
                    case false: return symbol.Ask;
                }
            }
            return 0;
        }

        private double VolForRiskAmount(Symbol symbol, double stopLossPips, double riskAmount)
        {
            var riskPerUnit = stopLossPips * symbol.PipValue;
            var riskUnits = riskAmount / riskPerUnit;
            var riskResult = Symbol.NormalizeVolumeInUnits(riskUnits, RoundingMode.Down);

            return riskResult;
        }

        private double VolForRiskPercentage(Symbol symbol, double stopLossPips, double riskPercentage)
        {
            var riskAmount = Account.Balance * riskPercentage / 100;
            return VolForRiskAmount(symbol, stopLossPips, riskAmount);
        }

        private double getVolumeForMargin(string symbolName, double marginVolume, string currency, double leverage, bool isBid)
        {
            var lots = genericMarginMethod(symbolName, currency, isBid,
                                       () => marginVolume * leverage / Symbol.LotSize,
                                       (rate) => marginVolume * leverage / (rate * Symbol.LotSize));

            return Symbol.NormalizeVolumeInUnits(Symbol.QuantityToVolumeInUnits(lots), RoundingMode.Down);
        }

        private double calculateMargin(string symbolName, double lots, string currency, double leverage, bool isBid)
        {
            var margin = genericMarginMethod(symbolName, currency, isBid,
                                       () => lots * Symbol.LotSize / leverage,
                                       (rate) => lots * Symbol.LotSize / leverage * rate);
            return Math.Round(margin, 2);
        }

        private double genericMarginMethod(string symbolName, string currency, bool isBid,
                                           Func<double> baseMethod,
                                           Func<double, double> rateMethod)
        {
            double retVal = 0.0;
            if (!Symbols.Exists(symbolName) || symbolName.Length != 6) return retVal;

            currency = currency.ToUpper();

            double rate = symbolRate(symbolName, isBid);
            if (rate == 0) return retVal;

            string baseCurrency = symbolName.Substring(0, 3).ToUpper();
            string subaCurrency = symbolName.Substring(3, 3).ToUpper();

            if (currency.Equals(baseCurrency))
            {
                retVal = baseMethod();
            }
            else if (currency.Equals(subaCurrency))
            {
                retVal = rateMethod(rate);
            }
            else
            {
                rate = symbolRate(baseCurrency + currency, isBid);
                if (rate == 0)
                {
                    rate = symbolRate(currency + baseCurrency, isBid);

                    if (rate == 0) return retVal;

                    rate = 1 / rate;
                }

                retVal = rateMethod(rate);
            }
            return (retVal > 0) ? retVal : 0;
        }

        #endregion

    }
}
