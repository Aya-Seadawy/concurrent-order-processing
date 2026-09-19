import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { CreateOrderRequest, OrderDto } from '../models';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private readonly http = inject(HttpClient);

  createOrder(idempotencyKey: string, request: CreateOrderRequest): Observable<OrderDto> {
    const headers = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<OrderDto>(`${API_BASE_URL}/orders`, request, { headers });
  }

  getOrder(id: string): Observable<OrderDto> {
    return this.http.get<OrderDto>(`${API_BASE_URL}/orders/${id}`);
  }

  cancelOrder(id: string): Observable<OrderDto> {
    return this.http.post<OrderDto>(`${API_BASE_URL}/orders/${id}/cancel`, {});
  }
}
