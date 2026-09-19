using MediatR;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Application.Common.Models;

namespace OrderProcessing.Application.Orders.Queries.GetProducts;

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _products;

    public GetProductsQueryHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _products.GetAllAsync(cancellationToken);
        return products
            .OrderBy(p => p.Code)
            .Select(p => new ProductDto(p.Code, p.Name, p.UnitPrice, p.AvailableQuantity))
            .ToList();
    }
}
