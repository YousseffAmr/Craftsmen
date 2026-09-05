import { Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<Theme>('light');
  private readonly storageKey = 'craftconnect_theme';

  initialize(): void {
    const savedTheme = localStorage.getItem(this.storageKey);
    this.setTheme(savedTheme === 'dark' ? 'dark' : 'light');
  }

  toggle(): void {
    this.setTheme(this.theme() === 'light' ? 'dark' : 'light');
  }

  private setTheme(theme: Theme): void {
    this.theme.set(theme);
    document.documentElement.dataset['theme'] = theme;
    localStorage.setItem(this.storageKey, theme);
  }
}