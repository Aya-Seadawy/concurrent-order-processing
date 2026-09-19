using MediatR;
using OrderProcessing.Application.Common.Models;

namespace OrderProcessing.Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;
