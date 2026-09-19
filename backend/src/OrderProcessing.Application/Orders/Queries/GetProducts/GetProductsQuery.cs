using MediatR;
using OrderProcessing.Application.Common.Models;

namespace OrderProcessing.Application.Orders.Queries.GetProducts;

public sealed record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;
