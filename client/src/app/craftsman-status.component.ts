import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

// TEMPORARY DEMO MODE: this is a small frontend-only screen to show the status route in preview mode.
@Component({
  selector: 'app-craftsman-status',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './craftsman-status.component.html',
  styleUrl: './craftsman-status.component.css',
})
export class CraftsmanStatusComponent {
  readonly demoStatus = { state: 'Approved', message: 'Preview account is active and ready for jobs.' };
}
