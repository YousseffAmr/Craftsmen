import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DemoPreviewRole, DemoPreviewService } from './demo-preview.service';
import { ThemeService } from './theme.service';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.css',
})
export class LandingComponent {
  constructor(
    readonly themeService: ThemeService,
    private readonly router: Router,
    private readonly demoPreviewService: DemoPreviewService,
  ) {}

  // TEMPORARY DEMO MODE: store a local role only and navigate to the matching protected page.
  previewAs(role: DemoPreviewRole): void {
    this.demoPreviewService.setRole(role);
    const targetRoute = this.demoPreviewService.getDefaultRoute(role);
    void this.router.navigateByUrl(targetRoute);
  }
}