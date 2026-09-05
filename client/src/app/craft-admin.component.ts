import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { Craft, CraftPayload, CraftService } from './craft.service';

@Component({
  selector: 'app-craft-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  styleUrl: './craft-admin.component.css',
  templateUrl: './craft-admin.component.html',
})
export class CraftAdminComponent implements OnInit {
  crafts = signal<Craft[]>([]);
  loading = signal(false);
  saving = signal(false);
  formError = signal<string | null>(null);
  errorMessage = signal<string | null>(null);
  editingId: number | null = null;
  form: CraftPayload = {
    name: '',
    description: '',
  };

  constructor(private readonly craftService: CraftService) {}

  ngOnInit(): void {
    this.loadCrafts();
  }

  loadCrafts(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.craftService.getCrafts().subscribe({
      next: (crafts) => {
        this.crafts.set(crafts);
        this.loading.set(false);
      },
      error: (error: Error) => {
        this.errorMessage.set(error.message);
        this.loading.set(false);
        this.crafts.set([]);
      },
    });
  }

  submitCraft(): void {
    this.formError.set(null);

    const payload: CraftPayload = {
      name: this.form.name.trim(),
      description: this.form.description?.trim() ?? '',
    };

    if (!payload.name) {
      this.formError.set('Craft name is required.');
      return;
    }

    this.saving.set(true);
    const request = this.editingId === null
      ? this.craftService.createCraft(payload)
      : this.craftService.updateCraft(this.editingId, payload);

    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.resetForm();
        this.loadCrafts();
      },
      error: (error: Error) => {
        this.formError.set(error.message);
      },
    });
  }

  startEdit(craft: Craft): void {
    this.editingId = craft.id;
    this.form = {
      name: craft.name,
      description: craft.description ?? '',
    };
    this.formError.set(null);
  }

  cancelEdit(): void {
    this.resetForm();
  }

  deleteCraft(id: number): void {
    const confirmed = window.confirm('Delete this craft?');
    if (!confirmed) {
      return;
    }

    this.craftService.deleteCraft(id).subscribe({
      next: () => {
        if (this.editingId === id) {
          this.resetForm();
        }
        this.loadCrafts();
      },
      error: (error: Error) => {
        this.errorMessage.set(error.message);
      },
    });
  }

  private resetForm(): void {
    this.editingId = null;
    this.form = { name: '', description: '' };
    this.formError.set(null);
    this.saving.set(false);
  }
}
