using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TomLonghurst.Nupendencies.Http;

public class JsonHttpContent : StringContent
{
    public JsonHttpContent(object content) : this(content, Encoding.UTF8)
    {
    }

    public JsonHttpContent(object content, Encoding? encoding) : base(JsonSerializer.Serialize(content), encoding, "application/json")
    {
    }
}