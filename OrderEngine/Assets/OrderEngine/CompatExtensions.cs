#nullable enable
using System;
using System.Collections.Generic;
namespace System
{
    internal static class ArgumentNullExceptionExtensions
    {
        public static void ThrowIfNull(object? argument, string? paramName = null)
        {
            if (argument is null)
                throw new ArgumentNullException(paramName);
        }
    }
}
namespace Ordering
{
    internal static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default!) where TKey : notnull
        {
            return dict.TryGetValue(key, out TValue? value) ? value : defaultValue;
        }
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value) where TKey : notnull
        {
            if (dict.ContainsKey(key))
                return false;
            dict[key] = value;
            return true;
        }
    }
    /// <summary>
    /// Simple priority queue for netstandard2.0 (uses sorted list internally)
    /// </summary>
    internal class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
    {
        private readonly SortedList<(TPriority Priority, int Order), TElement> _list = new();
        private int _insertOrder;
        public PriorityQueue() { }
        public PriorityQueue(int initialCapacity) { }
        public void Enqueue(TElement element, TPriority priority)
        {
            _list.Add((priority, _insertOrder++), element);
        }
        public bool TryDequeue(out TElement element, out TPriority priority)
        {
            if (_list.Count == 0)
            {
                element = default!;
                priority = default!;
                return false;
            }
            var key = _list.Keys[0];
            element = _list.Values[0];
            priority = key.Priority;
            _list.RemoveAt(0);
            return true;
        }
        public int Count => _list.Count;
    }
}