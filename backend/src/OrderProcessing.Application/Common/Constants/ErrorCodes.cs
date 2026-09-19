namespace OrderProcessing.Application.Common.Constants;

public static class ErrorCodes
{
    public const string InsufficientStock = "insufficient_stock";
    public const string IdempotencyKeyConflict = "idempotency_key_conflict";
    public const string RequestInProgress = "request_in_progress";
    public const string OrderNotFound = "order_not_found";
    public const string ValidationError = "validation_error";
    public const string UnknownProduct = "unknown_product";
}
