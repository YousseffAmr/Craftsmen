import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CustomerNotification, NotificationsService } from './notifications.service';

@Component({
  selector: 'app-customer-notifications',
  standalone: true,
  imports: [CommonModule, RouterLink],
  styleUrl: './customer-notifications.component.css',
  templateUrl: './customer-notifications.component.html',
})
export class CustomerNotificationsComponent implements OnInit {
  notifications = signal<CustomerNotification[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(private readonly service: NotificationsService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.service.getCustomerNotifications()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: notifications => this.notifications.set(notifications),
        error: (error: Error) => {
          this.notifications.set([]);
          this.errorMessage.set(error.message);
        },
      });
  }
}
