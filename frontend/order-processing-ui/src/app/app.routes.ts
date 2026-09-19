import { Routes } from '@angular/router';
import { ProductListComponent } from './pages/product-list/product-list.component';
import { OrderFormComponent } from './pages/order-form/order-form.component';
import { OrderLookupComponent } from './pages/order-lookup/order-lookup.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  { path: 'products', component: ProductListComponent },
  { path: 'orders/new', component: OrderFormComponent },
  { path: 'orders/:id', component: OrderLookupComponent },
];
