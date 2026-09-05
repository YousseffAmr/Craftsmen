import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { Craft } from './craft.service';
import { AvailableCraftsman, CustomerService } from './customer.service';

@Component({
  selector: 'app-customer-craftsmen',
  standalone: true,
  imports: [CommonModule, RouterLink],
  styleUrl: './customer-craftsmen.component.css',
  templateUrl: './customer-craftsmen.component.html',
})
export class CustomerCraftsmenComponent implements OnInit {
  crafts = signal<Craft[]>([]);
  craftsmen = signal<AvailableCraftsman[]>([]);
  selectedCraftId = signal<number | null>(null);
  loading = signal(false);
  craftsmenLoading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(private readonly customerService: CustomerService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.customerService.getCrafts()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (crafts) => {
          this.crafts.set(crafts);
          if (crafts.length > 0) {
            this.selectCraft(crafts[0].id);
          }
        },
        error: (error: Error) => {
          this.errorMessage.set(error.message);
          this.crafts.set([]);
        },
      });
  }

  selectCraft(craftId: number): void {
    this.selectedCraftId.set(craftId);
    this.craftsmenLoading.set(true);
    this.errorMessage.set(null);
    this.customerService.getCraftsmen(craftId)
      .pipe(finalize(() => this.craftsmenLoading.set(false)))
      .subscribe({
        next: (craftsmen) => this.craftsmen.set(craftsmen),
        error: (error: Error) => {
          this.craftsmen.set([]);
          this.errorMessage.set(error.message);
        },
      });
  }
}