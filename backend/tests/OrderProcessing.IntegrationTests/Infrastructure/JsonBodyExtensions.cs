using System.Text.Json;

namespace OrderProcessing.IntegrationTests.Infrastructure;

public static class JsonBodyExtensions
{
    public static Guid GetGuid(this string json, string propertyName) =>
        JsonDocument.Parse(json).RootElement.GetProperty(propertyName).GetGuid();

    public static string GetString(this string json, string propertyName) =>
        JsonDocument.Parse(json).RootElement.GetProperty(propertyName).GetString()!;

    public static int GetInt(this string json, string propertyName) =>
        JsonDocument.Parse(json).RootElement.GetProperty(propertyName).GetInt32();
}
