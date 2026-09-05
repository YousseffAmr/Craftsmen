import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CraftsmanRequest } from './craftsman-requests.service';
import { CraftsmanDaySheetService } from './craftsman-day-sheet.service';

@Component({
  selector: 'app-craftsman-day-sheet',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  styleUrl: './craftsman-day-sheet.component.css',
  templateUrl: './craftsman-day-sheet.component.html',
})
export class CraftsmanDaySheetComponent implements OnInit {
  selectedDate = this.today();
  requests = signal<CraftsmanRequest[]>([]);
  loading = signal(false);
  completingId = signal<number | null>(null);
  errorMessage = signal<string | null>(null);

  constructor(private readonly service: CraftsmanDaySheetService) {}

  ngOnInit(): void {
    this.loadDaySheet();
  }

  loadDaySheet(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.service.getDaySheet(this.selectedDate)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: response => this.requests.set(response.requests),
        error: (error: Error) => {
          this.requests.set([]);
          this.errorMessage.set(error.message);
        },
      });
  }

  complete(request: CraftsmanRequest): void {
    this.completingId.set(request.id);
    this.errorMessage.set(null);
    this.service.complete(request.id)
      .pipe(finalize(() => this.completingId.set(null)))
      .subscribe({
        next: updated => this.requests.update(requests => requests.filter(item => item.id !== updated.id)),
        error: (error: Error) => this.errorMessage.set(error.message),
      });
  }

  private today(): string {
    const date = new Date();
    const offsetDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    return offsetDate.toISOString().slice(0, 10);
  }
}
