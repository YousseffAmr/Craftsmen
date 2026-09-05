import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './theme.service';
import { HelperComponent } from './helper.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HelperComponent],
  template: '<router-outlet></router-outlet><app-helper></app-helper>',
})
export class App {
  constructor(private readonly themeService: ThemeService) {
    this.themeService.initialize();
  }
}

