import { Component, input } from '@angular/core';

@Component({
  selector: 'app-notification-status-badge',
  standalone: true,
  template: `<span class="badge" [class]="status()">{{ status() }}</span>`,
  styles: [`
    .badge {
      display: inline-block;
      padding: 0.15rem 0.6rem;
      border-radius: 999px;
      font-size: 0.8rem;
      font-weight: 600;
      text-transform: uppercase;
      background: #e0e0e0;
      color: #333;
    }
    .badge.Sent { background: #d4edda; color: #155724; }
    .badge.Pending { background: #fff3cd; color: #856404; }
    .badge.InProgress { background: #cce5ff; color: #004085; }
    .badge.Failed { background: #f8d7da; color: #721c24; }
  `]
})
export class NotificationStatusBadgeComponent {
  status = input.required<string>();
}
