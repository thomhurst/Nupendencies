namespace TomLonghurst.Nupendencies.Abstractions.Extensions;

public static class EnumerableExtensions
{
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
    {
        foreach (var t in enumerable)
        {
            collection.Add(t);
        }
    }
    
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var t in enumerable)
        {
            action(t);
        }
    }
}