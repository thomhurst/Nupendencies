using System.Text;

namespace TomLonghurst.Nupendencies;

public class PrintableList<T> : List<T>
{
    public override string ToString()
    {
        var sb = new StringBuilder(); 
        
        foreach (var item in this)
        {
            sb.AppendLine(item.ToString());
        }

        return sb.ToString();
    }

    public static PrintableList<T> FromIEnumerable<T>(IEnumerable<T> list)
    {
        var printableList = new PrintableList<T>();
        
        printableList.AddRange(list);

        return printableList;
    }
}