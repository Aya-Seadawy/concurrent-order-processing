using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderProcessing.Application.Common.Constants;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Application.Common.Mapping;
using OrderProcessing.Application.Common.Models;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Application.Orders.Commands.CreateOrder;

/// <summary>
/// Idempotency + stock deduction + order creation all happen inside one DB transaction (see design-note.md):
/// the unique constraint on IdempotencyRecord.Key is what serializes concurrent duplicate submissions,
/// and a failure anywhere rolls the whole thing back, including the claim row itself.
/// </summary>
public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _products;
    private readonly IOrderRepository _orders;
    private readonly IOrderNotificationRepository _notifications;
    private readonly IIdempotencyRecordRepository _idempotencyRecords;
    private readonly IDateTimeProvider _clock;
    private readonly IIdempotencyPayloadHasher _hasher;
    private readonly IChaosHook _chaosHook;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IUnitOfWork unitOfWork,
        IProductRepository products,
        IOrderRepository orders,
        IOrderNotificationRepository notifications,
        IIdempotencyRecordRepository idempotencyRecords,
        IDateTimeProvider clock,
        IIdempotencyPayloadHasher hasher,
        IChaosHook chaosHook,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _products = products;
        _orders = orders;
        _notifications = notifications;
        _idempotencyRecords = idempotencyRecords;
        _clock = clock;
        _hasher = hasher;
        _chaosHook = chaosHook;
        _logger = logger;
    }

    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var requestHash = _hasher.ComputeHash(
            request.CustomerReference,
            request.Lines.Select(l => (l.ProductCode, l.Quantity)).ToList());

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var claim = new IdempotencyRecord(request.IdempotencyKey, requestHash, _clock.UtcNow);
        var claimed = await _idempotencyRecords.TryInsertClaimAsync(claim, cancellationToken);

        if (!claimed)
        {
            // Another request already owns (or owned) this idempotency key.
            await transaction.RollbackAsync(cancellationToken);
            return await ResolveExistingKeyAsync(request.IdempotencyKey, requestHash, cancellationToken);
        }

        try
        {
            var result = await ProcessOrderAsync(request, claim, cancellationToken);
            await _chaosHook.BeforeOrderCommitAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<CreateOrderResult> ProcessOrderAsync(
        CreateOrderCommand request,
        IdempotencyRecord claim,
        CancellationToken cancellationToken)
    {
        var requestedCodes = request.Lines.Select(l => l.ProductCode).ToList();
        var products = await _products.GetByCodesAsync(requestedCodes, cancellationToken);

        var missingCode = requestedCodes.FirstOrDefault(code => !products.ContainsKey(code));
        if (missingCode is not null)
        {
            var result = CreateOrderResult.UnknownProduct(
                new ApiError(ErrorCodes.UnknownProduct, $"Unknown product code '{missingCode}'."));
            claim.Complete(result.StatusCode, result.ResponseBody, null, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }

        // Deduct each line atomically; if any line fails, compensate the lines already deducted so the
        // committed end state shows zero net stock change (spec: "leave no partial changes").
        var deductedSoFar = new List<(string ProductCode, int Quantity)>();
        foreach (var line in request.Lines)
        {
            var rowsAffected = await _products.TryDeductStockAsync(line.ProductCode, line.Quantity, cancellationToken);

            if (rowsAffected == 0)
            {
                foreach (var (code, qty) in deductedSoFar)
                {
                    await _products.RestoreStockAsync(code, qty, cancellationToken);
                }

                _logger.LogWarning("StockConflict for product {ProductCode}: requested {Quantity}", line.ProductCode, line.Quantity);

                var conflictResult = CreateOrderResult.InsufficientStock(
                    new ApiError(ErrorCodes.InsufficientStock, $"Insufficient stock for product '{line.ProductCode}'."));
                claim.Complete(conflictResult.StatusCode, conflictResult.ResponseBody, null, _clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return conflictResult;
            }

            deductedSoFar.Add((line.ProductCode, line.Quantity));
        }

        var orderLines = request.Lines
            .Select(l => new OrderLine(l.ProductCode, l.Quantity, products[l.ProductCode].UnitPrice))
            .ToList();

        var order = new Order(Guid.NewGuid(), request.CustomerReference, orderLines, _clock.UtcNow);
        _orders.Add(order);

        var notification = new OrderNotification(order.Id, _clock.UtcNow);
        _notifications.Add(notification);

        var orderDto = OrderMapper.ToDto(order, notification);
        var successResult = CreateOrderResult.Created(orderDto);
        claim.Complete(successResult.StatusCode, successResult.ResponseBody, order.Id, _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return successResult;
    }

    private async Task<CreateOrderResult> ResolveExistingKeyAsync(string key, string requestHash, CancellationToken cancellationToken)
    {
        var existing = await _idempotencyRecords.GetByKeyAsync(key, cancellationToken);

        if (existing is null)
        {
            // The competing transaction rolled back between our insert failing and this read; safe to ask for a retry.
            return CreateOrderResult.RequestInProgress(new ApiError(
                ErrorCodes.RequestInProgress, "A request with this Idempotency-Key is currently being processed. Retry shortly."));
        }

        if (existing.RequestHash != requestHash)
        {
            _logger.LogWarning("IdempotencyKeyConflict for key {Key}", key);
            return CreateOrderResult.KeyConflict(new ApiError(
                ErrorCodes.IdempotencyKeyConflict, "This Idempotency-Key was already used with a different request payload."));
        }

        if (existing.Status == IdempotencyStatus.Completed)
        {
            _logger.LogInformation("IdempotencyReplay for key {Key}, order {OrderId}", key, existing.OrderId);
            if (existing.OrderId.HasValue)
            {
                var currentOrder = await OrderLoader.LoadAsync(_orders, _notifications, existing.OrderId.Value, cancellationToken);
                if (currentOrder != null)
                {
                    return CreateOrderResult.Cached(existing.ResponseStatusCode!.Value, JsonSerializer.Serialize(currentOrder, SerializerOptions));
                }
            }

            return CreateOrderResult.Cached(existing.ResponseStatusCode!.Value, existing.ResponseBody!);
        }

        return CreateOrderResult.RequestInProgress(new ApiError(
            ErrorCodes.RequestInProgress, "A request with this Idempotency-Key is currently being processed. Retry shortly."));
    }
}
