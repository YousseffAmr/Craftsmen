import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService, LoginRequest } from './auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  styleUrl: './signup.component.css',
  templateUrl: './login.component.html',
})
export class LoginComponent {
  form: LoginRequest = {
    email: '',
    password: '',
  };

  submitting = signal(false);
  formError = signal<string | null>(null);

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    this.formError.set(null);

    const email = this.form.email.trim();
    const password = this.form.password;

    if (!email) {
      this.formError.set('Email is required.');
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      this.formError.set('Please enter a valid email address.');
      return;
    }

    if (!password) {
      this.formError.set('Password is required.');
      return;
    }

    this.submitting.set(true);
    this.authService.login({ email, password })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (response) => {
          this.authService.storeToken(response.token);
          switch (this.authService.getRole()) {
            case 'Admin':
              this.router.navigate(['/admin/craftsmen']);
              break;
            case 'Craftsman':
              this.router.navigate(['/craftsman/crafts']);
              break;
            case 'Customer':
              this.router.navigate(['/customer/craftsmen']);
              break;
            default:
              this.authService.clearToken();
              this.formError.set('Your account role could not be verified. Please sign in again.');
              break;
          }
        },
        error: (error: Error) => {
          this.formError.set(error.message);
        },
      });
  }
}