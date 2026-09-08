using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommentBot;

public static class Common
{
    public static JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true, AllowTrailingCommas = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
