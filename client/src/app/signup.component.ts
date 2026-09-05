import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService, SignupRequest, UserRole } from './auth.service';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  styleUrl: './signup.component.css',
  templateUrl: './signup.component.html',
})
export class SignupComponent {
  form: SignupRequest = {
    name: '',
    email: '',
    password: '',
    role: 'Customer',
  };

  submitting = signal(false);
  formError = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    this.formError.set(null);
    this.successMessage.set(null);

    const name = this.form.name.trim();
    const email = this.form.email.trim();
    const password = this.form.password;
    const role: UserRole = this.form.role ?? 'Customer';

    if (!name) {
      this.formError.set('Name is required.');
      return;
    }

    if (!email) {
      this.formError.set('Email is required.');
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      this.formError.set('Please enter a valid email address.');
      return;
    }

    if (!password || password.length < 8) {
      this.formError.set('Password must be at least 8 characters long.');
      return;
    }

    this.submitting.set(true);

    this.authService.signup({ name, email, password, role })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.form = { name: '', email: '', password: '', role: 'Customer' };
          this.authService.clearToken();
          this.router.navigate(['/login']);
        },
        error: (error: Error) => {
          this.formError.set(error.message);
        },
      });
  }
}
