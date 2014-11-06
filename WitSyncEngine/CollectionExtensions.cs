using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    // helpers to apply LINQ on TFS Client API xxxCollection
    public static class CollectionExtensions
    {
        public static void ForEach(this WorkItemTypeCollection collection, Action<WorkItemType> action)
        {
            foreach (WorkItemType item in collection)
            {
                action(item);
            }
        }

        public static void ForEach(this WorkItemMap[] array, Action<WorkItemMap> action)
        {
            foreach (WorkItemMap item in array)
            {
                action(item);
            }
        }

        public static IEnumerable<TOutput> ConvertAll<TOutput>(this WorkItemTypeCollection collection, Converter<WorkItemType, TOutput> converter)
        {
            foreach (WorkItemType item in collection)
            {
                yield return converter(item);
            }
        }

        public static IEnumerable<WorkItemType> Where<WorkItemType>(this WorkItemTypeCollection collection, Func<WorkItemType, bool> predicate)
        {
            foreach (WorkItemType item in collection)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
             this IEnumerable<TSource> source,
             Func<TSource, TKey> keySelector,
             IEqualityComparer<TKey> comparer)
        {
            HashSet<TKey> knownKeys = new HashSet<TKey>(comparer);
            foreach (TSource element in source)
            {
                if (knownKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
