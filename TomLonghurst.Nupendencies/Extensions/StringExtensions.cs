namespace TomLonghurst.Nupendencies.Extensions;

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }
        
        return value.Length <= maxLength ? value : value.Substring(0, maxLength); 
    }
    
    public static bool Contains(this string source, string toCheck, StringComparison comp)
    {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
}