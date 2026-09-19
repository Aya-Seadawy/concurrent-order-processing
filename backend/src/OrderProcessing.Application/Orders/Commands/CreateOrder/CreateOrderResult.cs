using System.Text.Json;
using OrderProcessing.Application.Common.Models;

namespace OrderProcessing.Application.Orders.Commands.CreateOrder;

public enum CreateOrderOutcome
{
    Created,
    CachedReplay,
    InsufficientStock,
    IdempotencyKeyConflict,
    RequestInProgress,
    UnknownProduct
}

public sealed class CreateOrderResult
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public CreateOrderOutcome Outcome { get; }
    public int StatusCode { get; }
    public string ResponseBody { get; }

    private CreateOrderResult(CreateOrderOutcome outcome, int statusCode, string responseBody)
    {
        Outcome = outcome;
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public static CreateOrderResult Created(OrderDto order) =>
        new(CreateOrderOutcome.Created, 201, JsonSerializer.Serialize(order, SerializerOptions));

    public static CreateOrderResult Cached(int statusCode, string responseBody) =>
        new(CreateOrderOutcome.CachedReplay, statusCode, responseBody);

    public static CreateOrderResult InsufficientStock(ApiError error) =>
        new(CreateOrderOutcome.InsufficientStock, 409, JsonSerializer.Serialize(error, SerializerOptions));

    public static CreateOrderResult KeyConflict(ApiError error) =>
        new(CreateOrderOutcome.IdempotencyKeyConflict, 409, JsonSerializer.Serialize(error, SerializerOptions));

    public static CreateOrderResult RequestInProgress(ApiError error) =>
        new(CreateOrderOutcome.RequestInProgress, 409, JsonSerializer.Serialize(error, SerializerOptions));

    public static CreateOrderResult UnknownProduct(ApiError error) =>
        new(CreateOrderOutcome.UnknownProduct, 400, JsonSerializer.Serialize(error, SerializerOptions));
}
