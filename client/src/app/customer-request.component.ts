import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { Craft } from './craft.service';
import { AvailableCraftsman, CustomerService } from './customer.service';

@Component({
  selector: 'app-customer-request',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  styleUrl: './customer-request.component.css',
  templateUrl: './customer-request.component.html',
})
export class CustomerRequestComponent implements OnInit {
  craft = signal<Craft | null>(null);
  craftsman = signal<AvailableCraftsman | null>(null);
  loading = signal(true);
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  form = { neededOn: '', price: '', description: '' };
  private craftsmanId = 0;
  private craftId = 0;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly customerService: CustomerService,
  ) {}

  ngOnInit(): void {
    this.craftsmanId = Number(this.route.snapshot.paramMap.get('id'));
    this.craftId = Number(this.route.snapshot.queryParamMap.get('craftId'));
    if (!this.craftsmanId || !this.craftId) {
      this.loading.set(false);
      this.errorMessage.set('A craftsman and craft must be selected.');
      return;
    }

    this.customerService.getCrafts()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (crafts) => {
          const selectedCraft = crafts.find(craft => craft.id === this.craftId);
          if (!selectedCraft) {
            this.errorMessage.set('Craft not found.');
            return;
          }
          this.craft.set(selectedCraft);
          this.customerService.getCraftsmen(this.craftId).subscribe({
            next: (craftsmen) => {
              const selectedCraftsman = craftsmen.find(craftsman => craftsman.id === this.craftsmanId);
              if (!selectedCraftsman) {
                this.errorMessage.set('Craftsman not found for this craft.');
                return;
              }
              this.craftsman.set(selectedCraftsman);
            },
            error: (error: Error) => this.errorMessage.set(error.message),
          });
        },
        error: (error: Error) => this.errorMessage.set(error.message),
      });
  }

  submit(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    if (!this.form.neededOn || !this.form.price || !this.form.description.trim()) {
      this.errorMessage.set('Date, price, and request details are required.');
      return;
    }
    const price = Number(this.form.price);
    if (!Number.isFinite(price) || price <= 0) {
      this.errorMessage.set('Price must be greater than zero.');
      return;
    }

    this.submitting.set(true);
    this.customerService.createRequest(this.craftsmanId, {
      craftId: this.craftId,
      neededOn: this.form.neededOn,
      price,
      description: this.form.description.trim(),
    }).pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: () => {
        this.successMessage.set('Your service request has been submitted.');
        this.form = { neededOn: '', price: '', description: '' };
      },
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }
}