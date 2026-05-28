using System.Text.Json;
using System.Text.Json.Nodes;

namespace KanonBot.Serializer;

public static class JsonNodeExtensions
{
    /// <summary>
    /// Deserialize a JsonNode to a specific type (replaces Newtonsoft's ToObject)
    /// </summary>
    public static T? ToObject<T>(this JsonNode? node)
    {
        if (node is null)
            return default;
        return node.Deserialize<T>(Json.Options);
    }

    /// <summary>
    /// Navigate a dot-separated path (replaces Newtonsoft's SelectToken)
    /// Supports paths like "data.bid_data" or "user_performances.total"
    /// </summary>
    public static JsonNode? SelectToken(this JsonNode? node, string path)
    {
        if (node is null)
            return null;
        var parts = path.Split('.');
        JsonNode? current = node;
        foreach (var part in parts)
        {
            if (current is JsonObject obj)
                current = obj[part];
            else if (current is JsonArray arr && int.TryParse(part, out var index))
                current = arr[index];
            else
                return null;
        }
        return current;
    }

    /// <summary>
    /// Get value from a JsonNode (replaces JToken.Value&lt;T&gt;())
    /// </summary>
    public static T? Value<T>(this JsonNode? node)
    {
        if (node is null)
            return default;
        return node.GetValue<T>();
    }
}
