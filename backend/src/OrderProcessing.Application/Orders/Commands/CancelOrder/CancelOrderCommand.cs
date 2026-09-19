using MediatR;

namespace OrderProcessing.Application.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId) : IRequest<CancelOrderResult>;
