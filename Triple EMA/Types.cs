using System;
using System.Collections;
using System.Collections.Generic;
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

        //Ensure you dont call Min Linq extension method.
        public static KeyValuePair<K, V> Min<K, V>(this SortedList<K, V> dict)
        {
            return new KeyValuePair<K, V>(dict.Keys[0], dict.Values[0]); //is O(1)
        }

        //Ensure you dont call Max Linq extension method.
        public static KeyValuePair<K, V> Last<K, V>(this SortedList<K, V> dict)
        {
            var index = dict.Count - 1; //O(1) again
            return new KeyValuePair<K, V>(dict.Keys[index], dict.Values[index]);
        }

        public static V LastValue<K, V>(this SortedList<K, V> dict)
        {
            var index = dict.Count - 1; //O(1) again
            return dict.Values[index];
        }

        public static V LastValueOrDefault<K, V>(this SortedList<K, V> dict, V defaultValue = default(V))
        {
            if (dict.Count == 0)
                return defaultValue;

            var index = dict.Count - 1;
            return dict.Values[index];
        }
    }

    public class ListDefault : IndicatorDataSeries
    {
        private SortedList<int, double> Values { get; set; }
        public ListDefault()
        {
            Values = new SortedList<int, double>();
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


        public double LastValue { get { return Values.LastValueOrDefault(double.NaN); } }

        public int Count { get { return Values.Count; } }

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
