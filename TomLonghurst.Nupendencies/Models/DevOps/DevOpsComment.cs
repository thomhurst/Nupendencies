using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsComment(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("parentCommentId")] int ParentCommentId,
    [property: JsonPropertyName("author")] DevOpsAuthor DevOpsAuthor,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("publishedDate")] DateTime PublishedDate,
    [property: JsonPropertyName("lastUpdatedDate")] DateTime LastUpdatedDate,
    [property: JsonPropertyName("lastContentUpdatedDate")] DateTime LastContentUpdatedDate,
    [property: JsonPropertyName("commentType")] string CommentType
);