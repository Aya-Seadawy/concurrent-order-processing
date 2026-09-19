namespace OrderProcessing.Api.Contracts;

public sealed record OrderLineRequestBody(string ProductCode, int Quantity);

public sealed record CreateOrderRequestBody(string CustomerReference, List<OrderLineRequestBody> Lines);
