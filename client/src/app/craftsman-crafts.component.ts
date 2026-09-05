import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CraftsmanCraftOption, CraftsmanCraftsService } from './craftsman-crafts.service';

@Component({
  selector: 'app-craftsman-crafts',
  standalone: true,
  imports: [CommonModule, RouterLink],
  styleUrl: './craftsman-crafts.component.css',
  templateUrl: './craftsman-crafts.component.html',
})
export class CraftsmanCraftsComponent implements OnInit {
  crafts = signal<CraftsmanCraftOption[]>([]);
  status = signal<'Pending' | 'Approved' | 'Refused' | null>(null);
  loading = signal(false);
  actionId = signal<number | null>(null);
  errorMessage = signal<string | null>(null);

  constructor(private readonly service: CraftsmanCraftsService) {}

  ngOnInit(): void {
    this.loadCrafts();
  }

  loadCrafts(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.service.getCrafts()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.status.set(response.status);
          this.crafts.set(response.crafts);
        },
        error: (error: Error) => {
          this.crafts.set([]);
          this.errorMessage.set(error.message);
        },
      });
  }

  changeSelection(craft: CraftsmanCraftOption): void {
    if (this.status() !== 'Approved') {
      return;
    }

    this.actionId.set(craft.id);
    this.errorMessage.set(null);
    const request = craft.selected
      ? this.service.removeCraft(craft.id)
      : this.service.selectCraft(craft.id);

    request.pipe(finalize(() => this.actionId.set(null))).subscribe({
      next: () => this.loadCrafts(),
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }
}