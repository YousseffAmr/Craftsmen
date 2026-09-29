import { Injectable, signal } from '@angular/core';
import { TRANSLATIONS } from './i18n/translations';

export type Language = 'en' | 'ar';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  readonly language = signal<Language>('en');
  private readonly storageKey = 'craftconnect_language';

  initialize(): void {
    const savedLang = localStorage.getItem(this.storageKey);
    this.setLanguage(savedLang === 'ar' ? 'ar' : 'en');
  }

  toggle(): void {
    this.setLanguage(this.language() === 'en' ? 'ar' : 'en');
  }

  private setLanguage(lang: Language): void {
    this.language.set(lang);
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    document.documentElement.lang = lang;
    localStorage.setItem(this.storageKey, lang);
  }

  translate(key: string): string {
    return TRANSLATIONS[key]?.[this.language()] ?? key;
  }
}
