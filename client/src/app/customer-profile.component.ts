import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CustomerProfile, ProfileService } from './profile.service';

@Component({ selector: 'app-customer-profile', standalone: true, imports: [CommonModule, FormsModule, RouterLink], styleUrl: './profile.component.css', templateUrl: './customer-profile.component.html' })
export class CustomerProfileComponent implements OnInit {
  profile = signal<CustomerProfile | null>(null); loading = signal(false); saving = signal(false); errorMessage = signal<string | null>(null); successMessage = signal<string | null>(null); form = { name: '', contactInfo: '' };
  constructor(private readonly service: ProfileService) {}
  ngOnInit(): void { this.loading.set(true); this.service.getCustomer().pipe(finalize(() => this.loading.set(false))).subscribe({ next: p => { this.profile.set(p); this.form = { name: p.name, contactInfo: p.contactInfo ?? '' }; }, error: (e: Error) => this.errorMessage.set(e.message) }); }
  save(): void { this.saving.set(true); this.errorMessage.set(null); this.successMessage.set(null); this.service.updateCustomer(this.form).pipe(finalize(() => this.saving.set(false))).subscribe({ next: p => { this.profile.set(p); this.successMessage.set('Profile updated.'); }, error: (e: Error) => this.errorMessage.set(e.message) }); }
}
