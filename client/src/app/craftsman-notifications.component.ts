import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CraftsmanNotification, NotificationsService } from './notifications.service';

@Component({
  selector: 'app-craftsman-notifications',
  standalone: true,
  imports: [CommonModule, RouterLink],
  styleUrl: './craftsman-notifications.component.css',
  templateUrl: './craftsman-notifications.component.html',
})
export class CraftsmanNotificationsComponent implements OnInit {
  notifications = signal<CraftsmanNotification[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(private readonly service: NotificationsService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.service.getCraftsmanNotifications()
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
