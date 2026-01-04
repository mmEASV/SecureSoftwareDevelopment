using System.Text.Json;

namespace Admin.Shared.Utils;

public static class JsonExtensions
{
    /// <summary>
    /// Converts an object to a JsonDocument by serializing it to JSON and then parsing it.
    /// </summary>
    /// <typeparam name="T">The type of object to convert</typeparam>
    /// <param name="obj">The object to convert</param>
    /// <returns>A JsonDocument representation of the object</returns>
    public static JsonDocument ToJsonDocument<T>(this T obj)
    {
        // First serialize the object to a JSON string
        string jsonString = JsonSerializer.Serialize(obj);

        // Then parse the JSON string to a JsonDocument
        return JsonDocument.Parse(jsonString);
    }
}
