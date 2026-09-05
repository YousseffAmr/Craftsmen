import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { BoardGroup, CraftsmanBoardService } from './craftsman-board.service';

@Component({
  selector: 'app-craftsman-board',
  standalone: true,
  imports: [CommonModule, RouterLink],
  styleUrl: './craftsman-board.component.css',
  templateUrl: './craftsman-board.component.html',
})
export class CraftsmanBoardComponent implements OnInit {
  groups = signal<BoardGroup[]>([]);
  requestCount = signal(0);
  totalValue = signal(0);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(private readonly service: CraftsmanBoardService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.service.getBoard()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: board => {
          this.groups.set(board.groups);
          this.requestCount.set(board.totals.requestCount);
          this.totalValue.set(board.totals.totalValue);
        },
        error: (error: Error) => {
          this.groups.set([]);
          this.errorMessage.set(error.message);
        },
      });
  }
}
