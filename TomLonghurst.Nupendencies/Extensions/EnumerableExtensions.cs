namespace TomLonghurst.Nupendencies.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<TResult> SortBy<TResult, TKey>(
        this IEnumerable<TResult> itemsToSort,
        IEnumerable<TKey> sortKeys,
        Func<TResult, TKey> matchFunc)
    {
        return sortKeys.Join(itemsToSort,
            key => key,
            matchFunc,
            (_, iitem) => iitem);
    }
    
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
    {
        return items.GroupBy(property).Select(x => x.First());
    }
}