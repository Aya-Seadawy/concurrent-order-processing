import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { ProductsService } from '../../services/products.service';
import { ProductDto } from '../../models';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit {
  private readonly productsService = inject(ProductsService);

  products = signal<ProductDto[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.productsService.getProducts().subscribe({
      next: (products) => {
        this.products.set(products);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load products. Is the API running?');
        this.loading.set(false);
      }
    });
  }
}
