import { Component, OnInit, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { OrdersService } from '../../services/orders.service';
import { ProductsService } from '../../services/products.service';
import { ApiError, ProductDto } from '../../models';

@Component({
  selector: 'app-order-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './order-form.component.html'
})
export class OrderFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ordersService = inject(OrdersService);
  private readonly productsService = inject(ProductsService);
  private readonly router = inject(Router);

  products = signal<ProductDto[]>([]);
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  errorCode = signal<string | null>(null);

  /** Reused across retries of the same in-flight submission; replaced only when the user starts a genuinely new order. */
  private idempotencyKey = crypto.randomUUID();

  form = this.fb.nonNullable.group({
    customerReference: ['', Validators.required],
    lines: this.fb.array([this.createLineGroup()])
  });

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  ngOnInit(): void {
    this.productsService.getProducts().subscribe((products) => this.products.set(products));
  }

  private createLineGroup() {
    return this.fb.nonNullable.group({
      productCode: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]]
    });
  }

  addLine(): void {
    this.lines.push(this.createLineGroup());
  }

  removeLine(index: number): void {
    if (this.lines.length > 1) {
      this.lines.removeAt(index);
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.errorCode.set(null);

    const value = this.form.getRawValue();

    this.ordersService.createOrder(this.idempotencyKey, value).subscribe({
      next: (order) => {
        this.submitting.set(false);
        // The submission settled successfully; a future order must use a fresh key.
        this.idempotencyKey = crypto.randomUUID();
        this.router.navigate(['/orders', order.id]);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        const apiError = err.error as ApiError | undefined;
        this.errorCode.set(apiError?.code ?? null);
        this.errorMessage.set(apiError?.message ?? 'Something went wrong while creating the order.');

        // Keep retrying with the same key only while the outcome is genuinely uncertain: a duplicate is
        // still processing, or we couldn't confirm whether the server received/committed the request at all.
        const outcomeIsUncertain = apiError?.code === 'request_in_progress' || err.status === 0 || err.status >= 500;
        if (!outcomeIsUncertain) {
          this.idempotencyKey = crypto.randomUUID();
        }
      }
    });
  }
}
