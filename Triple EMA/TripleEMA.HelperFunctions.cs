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
        private Dictionary<string, Func<bool, double>> RateLookups { get; set; }

        private void InitRateLookups()
        {
            RateLookups = new Dictionary<string, Func<bool, double>>
            {
                { "XAUGBP", (x) => SymbolRate("XAUGBP", x) },
                { "GBPMXN", (x) => 1},
                { "GBPTRY", (x) => 1},
                { "GBPSEK", (x) => 1},
                { "GBPNOK", (x) => 1},
                { "GBPSGD", (x) => 1},
                { "GBPNZD", (x) => 1},
                { "GBPCAD", (x) => 1},
                { "GBPJPY", (x) => 1},
                { "EURGBP", (x) => SymbolRate("EURGBP", x) },
                { "GBPCHF", (x) => 1},
                { "GBPAUD", (x) => 1},
                { "GBPUSD", (x) => 1},

                //NON-FOREX "PAIRS"

                { "SpotCrude", (x) => SymbolRate("SpotCrude", x) / SymbolRate("GBPUSD", x) },
                { "GER40", (x) => SymbolRate("GER40", x) * SymbolRate("EURGBP", x) },
            };
        }

        private double GetMarginRate(string symbolName, bool isBuy)
        {
            //GENERAL METHOD FOR NON-FOREX
            //rate / accountCurrency-quotecurrency
            //OR rate * quote-accountCurrency if account-quote doesn't exist.

            //if the quote of the non-forex "pair" is gbp then just return the rate, no divison or multiplication
            //TODO: change to dictionary for better performance.


            Func<bool, double> rateFunc;
            if (RateLookups.TryGetValue(symbolName, out rateFunc))
            {
                return rateFunc(isBuy);
            }

            return -1;
            /*switch (symbolName)
            {
                //USD PART
                case "SpotCrude":
                    ret = SymbolRate(symbolName, isBuy) / SymbolRate("GBPUSD", isBuy);
                    break;
                //EURO QUOTE
                case "GER40":
                    ret = SymbolRate(symbolName, isBuy) * SymbolRate("EURGBP", isBuy);
                    break;
            }
            return ret;*/
        }

        private double SymbolRate(string symbolName, bool isBuy = true)
        {
            if (Symbols.Exists(symbolName))
            {
                var symbol = Symbols.GetSymbol(symbolName);

                return isBuy ? symbol.Bid : symbol.Ask;
            }
            else if (symbolName.Length == 6)
            {
                var baseCurrency = symbolName.Substring(0, 3);
                var quoteCurrency = symbolName.Substring(3, 3);
                var invertedCurrency = quoteCurrency + baseCurrency;

                if (Symbols.Exists(invertedCurrency))
                {
                    var symbol = Symbols.GetSymbol(invertedCurrency);
                    return 1 / (isBuy ? symbol.Bid : symbol.Ask);
                }
            }
            return 0;
        }

        /*private double GetMaxVolumeForRiskOrMargin(Symbol symbol, double stopLossPips, bool isBuy)
        {
            var riskVolume = VolForRiskPercentage(symbol, stopLossPips, RiskPercentage);
            var marginVolume = GetVolumeForMargin(symbol.Name, MaxMarginPerPosition, Account.Asset.Name, symbol.DynamicLeverage[0].Leverage, isBuy);

            return Math.Min(marginVolume, riskVolume);
        }*/

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

        private double GetVolumeForMargin(string symbolName, double marginVolume, string currency, double leverage, bool isBuy)
        {
            var lots = GenericMarginMethod(symbolName, currency, isBuy,
                                       () => marginVolume * leverage / Symbol.LotSize,
                                       (rate) => marginVolume * leverage / (rate * Symbol.LotSize));

            return Symbol.NormalizeVolumeInUnits(Symbol.QuantityToVolumeInUnits(lots), RoundingMode.Down);
        }

        private double CalculateMargin(string symbolName, double lots, string currency, double leverage, bool isBuy)
        {
            var margin = GenericMarginMethod(symbolName, currency, isBuy,
                                       () => lots * Symbol.LotSize / leverage,
                                       (rate) => lots * Symbol.LotSize / leverage * rate);
            return Math.Round(margin, 2);
        }



        private double GenericMarginMethod(string symbolName, string currency, bool isBuy,
                                           Func<double> baseMethod,
                                           Func<double, double> rateMethod)
        {
            //TODO: IMPLEMENT MARGIN TABLE FOR ALL SYMBOLS NOT JUST CURRENCIES
            //if symbolName == SpotCrude
            double rate;
            double retVal = 0.0;

            currency = currency.ToUpper();
            // how to find if symbol is a currency?
            // will be 6 chars long.

            rate = GetMarginRate(symbolName, isBuy);
            if (rate > 0)
            {
                return rateMethod(rate);
            }


            //e.g. GBPUSD, EURGBP, USDCAD, SpotCrude/USD
            string baseCurrency = symbolName.Substring(0, 3).ToUpper();
            string subaCurrency = symbolName.Substring(3, 3).ToUpper();

            if (currency.Equals(baseCurrency))
            {
                retVal = baseMethod();
            }
            else if (currency.Equals(subaCurrency))
            {
                rate = SymbolRate(symbolName, isBuy);
                retVal = rateMethod(rate);
            }
            else
            {
                //USDCAD in here

                //is baseCurrency is not forex, then what do?


                rate = SymbolRate(baseCurrency + currency, isBuy);
                //USDGBP doesn't exist
                if (rate == 0)
                {
                    //GBPUSD does
                    //sell 1.31766
                    //buy 1,31777
                    //val = 30 / (rate)
                    //rate = 30/297.38
                    rate = SymbolRate(currency + baseCurrency, isBuy);

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
