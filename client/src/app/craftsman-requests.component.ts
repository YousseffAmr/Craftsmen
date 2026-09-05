import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CraftsmanRequest, CraftsmanRequestsService } from './craftsman-requests.service';

@Component({
  selector: 'app-craftsman-requests',
  standalone: true,
  imports: [CommonModule, RouterLink],
  styleUrl: './craftsman-requests.component.css',
  templateUrl: './craftsman-requests.component.html',
})
export class CraftsmanRequestsComponent implements OnInit {
  requests = signal<CraftsmanRequest[]>([]);
  loading = signal(false);
  actionId = signal<number | null>(null);
  errorMessage = signal<string | null>(null);

  constructor(private readonly service: CraftsmanRequestsService) {}

  ngOnInit(): void {
    this.loadRequests();
  }

  loadRequests(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.service.getRequests()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (requests) => this.requests.set(requests),
        error: (error: Error) => {
          this.requests.set([]);
          this.errorMessage.set(error.message);
        },
      });
  }

  respond(request: CraftsmanRequest, action: 'accept' | 'decline'): void {
    if (request.status !== 'Pending') {
      return;
    }

    this.actionId.set(request.id);
    this.errorMessage.set(null);
    const response = action === 'accept'
      ? this.service.accept(request.id)
      : this.service.decline(request.id);

    response.pipe(finalize(() => this.actionId.set(null))).subscribe({
      next: (updatedRequest) => {
        this.requests.update(requests => requests.map(item => item.id === updatedRequest.id ? updatedRequest : item));
      },
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }
}