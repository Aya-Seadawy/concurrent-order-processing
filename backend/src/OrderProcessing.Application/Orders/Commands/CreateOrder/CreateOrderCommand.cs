using MediatR;

namespace OrderProcessing.Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderLineRequest(string ProductCode, int Quantity);

public sealed record CreateOrderCommand(
    string IdempotencyKey,
    string CustomerReference,
    IReadOnlyList<CreateOrderLineRequest> Lines) : IRequest<CreateOrderResult>;
