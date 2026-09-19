using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Api.Contracts;
using OrderProcessing.Application.Common.Constants;
using OrderProcessing.Application.Common.Models;
using OrderProcessing.Application.Orders.Commands.CancelOrder;
using OrderProcessing.Application.Orders.Commands.CreateOrder;
using OrderProcessing.Application.Orders.Queries.GetOrderById;

namespace OrderProcessing.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequestBody request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ApiError(ErrorCodes.ValidationError, "Idempotency-Key header is required."));
        }

        var command = new CreateOrderCommand(
            idempotencyKey,
            request.CustomerReference,
            request.Lines.Select(l => new CreateOrderLineRequest(l.ProductCode, l.Quantity)).ToList());

        var result = await _mediator.Send(command, cancellationToken);

        return new ContentResult
        {
            Content = result.ResponseBody,
            ContentType = "application/json",
            StatusCode = result.StatusCode
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);

        return order is null
            ? NotFound(new ApiError(ErrorCodes.OrderNotFound, $"Order '{id}' was not found."))
            : Ok(order);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id), cancellationToken);

        return result.Outcome switch
        {
            CancelOrderOutcome.NotFound => NotFound(new ApiError(ErrorCodes.OrderNotFound, $"Order '{id}' was not found.")),
            _ => Ok(result.Order)
        };
    }
}
