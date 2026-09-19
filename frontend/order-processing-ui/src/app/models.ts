export interface ProductDto {
  code: string;
  name: string;
  unitPrice: number;
  availableQuantity: number;
}

export interface OrderLineDto {
  productCode: string;
  quantity: number;
  unitPriceAtPurchase: number;
  lineTotal: number;
}

export interface OrderDto {
  id: string;
  customerReference: string;
  status: string;
  totalAmount: number;
  createdAtUtc: string;
  cancelledAtUtc: string | null;
  lines: OrderLineDto[];
  notificationStatus: string;
}

export interface ApiError {
  code: string;
  message: string;
}

export interface OrderLineRequest {
  productCode: string;
  quantity: number;
}

export interface CreateOrderRequest {
  customerReference: string;
  lines: OrderLineRequest[];
}
