import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { ProductDto } from '../models';

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly http = inject(HttpClient);

  getProducts(): Observable<ProductDto[]> {
    return this.http.get<ProductDto[]>(`${API_BASE_URL}/products`);
  }
}
