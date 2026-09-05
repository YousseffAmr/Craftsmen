import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CraftsmanAdminService, PendingCraftsman } from './craftsman-admin.service';

@Component({
  selector: 'app-craftsman-admin',
  standalone: true,
  imports: [CommonModule, RouterLink],
  styleUrl: './craftsman-admin.component.css',
  templateUrl: './craftsman-admin.component.html',
})
export class CraftsmanAdminComponent implements OnInit {
  craftsmen = signal<PendingCraftsman[]>([]);
  loading = signal(false);
  actionId = signal<number | null>(null);
  errorMessage = signal<string | null>(null);

  constructor(private readonly service: CraftsmanAdminService) {}

  ngOnInit(): void {
    this.loadPending();
  }

  loadPending(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.service.getPending()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (craftsmen) => this.craftsmen.set(craftsmen),
        error: (error: Error) => {
          this.craftsmen.set([]);
          this.errorMessage.set(error.message);
        },
      });
  }

  approve(craftsman: PendingCraftsman): void {
    this.updateStatus(craftsman, 'approve');
  }

  refuse(craftsman: PendingCraftsman): void {
    this.updateStatus(craftsman, 'refuse');
  }

  private updateStatus(craftsman: PendingCraftsman, action: 'approve' | 'refuse'): void {
    this.actionId.set(craftsman.id);
    this.errorMessage.set(null);
    const request = action === 'approve'
      ? this.service.approve(craftsman.id)
      : this.service.refuse(craftsman.id);

    request.pipe(finalize(() => this.actionId.set(null))).subscribe({
      next: () => this.loadPending(),
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }
}