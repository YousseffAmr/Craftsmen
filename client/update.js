const fs = require('fs');
let app = fs.readFileSync('src/app/app.ts', 'utf-8');
app = app.replace(
  'import { ThemeService } from \'./theme.service\';',
  'import { ThemeService } from \'./theme.service\';\nimport { LanguageService } from \'./language.service\';'
);
app = app.replace(
  'constructor(private readonly themeService: ThemeService) {\n    this.themeService.initialize();\n  }',
  'constructor(private readonly themeService: ThemeService, private readonly languageService: LanguageService) {\n    this.themeService.initialize();\n    this.languageService.initialize();\n  }'
);
fs.writeFileSync('src/app/app.ts', app);

let landing = fs.readFileSync('src/app/landing.component.ts', 'utf-8');
landing = landing.replace(
  'import { ThemeService } from \'./theme.service\';',
  'import { ThemeService } from \'./theme.service\';\nimport { LanguageService } from \'./language.service\';'
);
landing = landing.replace(
  'readonly themeService: ThemeService,',
  'readonly themeService: ThemeService,\n    readonly languageService: LanguageService,'
);
fs.writeFileSync('src/app/landing.component.ts', landing);

let landingHtml = fs.readFileSync('src/app/landing.component.html', 'utf-8');
const toggle = '<button class="theme-toggle" type="button" (click)="themeService.toggle()" [attr.aria-label]="themeService.theme() === \'light\' ? \'Switch to dark mode\' : \'Switch to light mode\'"><i class="bi" [class.bi-moon-stars-fill]="themeService.theme() === \'light\'" [class.bi-sun-fill]="themeService.theme() === \'dark\'"></i></button>';
const langBtn = '\n      <button class="theme-toggle ms-2" style="font-weight:bold; font-size: 0.8rem;" type="button" (click)="languageService.toggle()" [attr.aria-label]="languageService.language() === \'en\' ? \'Switch to Arabic\' : \'Switch to English\'">{{ languageService.language() === \'en\' ? \'AR\' : \'EN\' }}</button>';
landingHtml = landingHtml.replace(toggle, toggle + langBtn);
fs.writeFileSync('src/app/landing.component.html', landingHtml);
console.log('Done manual updates');
