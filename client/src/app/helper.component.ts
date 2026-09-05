import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HelperService } from './helper.service';
import { AuthService } from './auth.service';

@Component({ selector: 'app-helper', standalone: true, imports: [CommonModule, FormsModule, RouterLink], templateUrl: './helper.component.html', styleUrl: './helper.component.css' })
export class HelperComponent {
  open = signal(false); loading = signal(false); errorMessage = signal<string | null>(null); response = signal<{answer:string;link?:string}|null>(null); question = '';
  constructor(private readonly service: HelperService, readonly authService: AuthService) {}
  ask(): void { if (!this.question.trim()) { this.errorMessage.set('Question is required.'); return; } this.loading.set(true); this.errorMessage.set(null); this.response.set(null); this.service.ask(this.question).subscribe({ next: value => { this.response.set(value); this.loading.set(false); }, error: (error: Error) => { this.errorMessage.set(error.message); this.loading.set(false); } }); }
}
