import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { OrdersService } from '../../services/orders.service';
import { NotificationStatusBadgeComponent } from '../../notification-status-badge/notification-status-badge.component';
import { ApiError, OrderDto } from '../../models';

@Component({
  selector: 'app-order-lookup',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, NotificationStatusBadgeComponent],
  templateUrl: './order-lookup.component.html'
})
export class OrderLookupComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ordersService = inject(OrdersService);

  order = signal<OrderDto | null>(null);
  loading = signal(false);
  cancelling = signal(false);
  errorMessage = signal<string | null>(null);
  searchId = signal('');

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.searchId.set(id);
      this.load(id);
    }
  }

  load(id: string): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.ordersService.getOrder(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.order.set(null);
        this.loading.set(false);
        const apiError = err.error as ApiError | undefined;
        this.errorMessage.set(apiError?.message ?? 'Order not found.');
      }
    });
  }

  search(): void {
    const id = this.searchId().trim();
    if (!id) {
      return;
    }
    this.router.navigate(['/orders', id]);
    this.load(id);
  }

  cancel(): void {
    const current = this.order();
    if (!current) {
      return;
    }

    this.cancelling.set(true);
    this.ordersService.cancelOrder(current.id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.cancelling.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.cancelling.set(false);
        const apiError = err.error as ApiError | undefined;
        this.errorMessage.set(apiError?.message ?? 'Could not cancel the order.');
      }
    });
  }
}
