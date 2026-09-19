using OrderProcessing.Application.Common.Models;

namespace OrderProcessing.Application.Orders.Commands.CancelOrder;

public enum CancelOrderOutcome
{
    Cancelled,
    AlreadyCancelled,
    NotFound
}

public sealed class CancelOrderResult
{
    public required CancelOrderOutcome Outcome { get; init; }
    public OrderDto? Order { get; init; }
}
