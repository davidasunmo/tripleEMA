using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo
{
    public static class MyExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
    }

    public class ListDefault : IndicatorDataSeries
    {
        private Dictionary<int, double> Values { get; set; }
        public ListDefault()
        {
            Values = new Dictionary<int, double>();
        }

        public double this[int index]
        {
            get
            {
                return Values.GetValueOrDefault(index, double.NaN);
            }
            set
            {
                Values[index] = value;
            }
        }


        public double LastValue { get { throw new NotImplementedException(); } }

        public int Count { get { throw new NotImplementedException(); } }

        public double Last(int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<double> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
