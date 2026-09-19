using System.Net;
using System.Net.Http.Json;

namespace OrderProcessing.IntegrationTests.Infrastructure;

public static class HttpClientExtensions
{
    public static async Task<(HttpStatusCode StatusCode, string Body)> CreateOrderAsync(
        this HttpClient client, string idempotencyKey, string customerReference, params (string ProductCode, int Quantity)[] lines)
    {
        var payload = new
        {
            customerReference,
            lines = lines.Select(l => new { productCode = l.ProductCode, quantity = l.Quantity })
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, body);
    }

    public static async Task<(HttpStatusCode StatusCode, string Body)> CancelOrderAsync(this HttpClient client, Guid orderId)
    {
        var response = await client.PostAsync($"/api/orders/{orderId}/cancel", content: null);
        var body = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, body);
    }

    public static async Task<(HttpStatusCode StatusCode, string Body)> GetOrderAsync(this HttpClient client, Guid orderId)
    {
        var response = await client.GetAsync($"/api/orders/{orderId}");
        var body = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, body);
    }
}
