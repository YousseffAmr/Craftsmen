import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CraftsmanProfile, ProfileService } from './profile.service';

@Component({ selector: 'app-craftsman-profile', standalone: true, imports: [CommonModule, FormsModule, RouterLink], styleUrl: './profile.component.css', templateUrl: './craftsman-profile.component.html' })
export class CraftsmanProfileComponent implements OnInit {
  profile = signal<CraftsmanProfile | null>(null); loading = signal(false); saving = signal(false); errorMessage = signal<string | null>(null); successMessage = signal<string | null>(null);
  form = { name: '', contactInfo: '', dailyRate: 0 };
  constructor(private readonly service: ProfileService) {}
  ngOnInit(): void { this.loading.set(true); this.service.getCraftsman().pipe(finalize(() => this.loading.set(false))).subscribe({ next: p => { this.profile.set(p); this.form = { name: p.name, contactInfo: p.contactInfo ?? '', dailyRate: p.dailyRate }; }, error: (e: Error) => this.errorMessage.set(e.message) }); }
  save(): void { this.saving.set(true); this.errorMessage.set(null); this.successMessage.set(null); this.service.updateCraftsman(this.form).pipe(finalize(() => this.saving.set(false))).subscribe({ next: p => { this.profile.set(p); this.successMessage.set('Profile updated.'); }, error: (e: Error) => this.errorMessage.set(e.message) }); }
}
