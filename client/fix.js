const fs = require('fs');
const path = require('path');
const dir = path.join(process.cwd(), 'src/app');

// Fix TS imports
const tsPath = path.join(dir, 'landing.component.ts');
let tsContent = fs.readFileSync(tsPath, 'utf-8');
if (!tsContent.includes('import { LanguageService }')) {
  tsContent = "import { LanguageService } from './language.service';\n" + tsContent;
}
if (!tsContent.includes('readonly languageService')) {
  tsContent = tsContent.replace('readonly themeService: ThemeService,', 'readonly themeService: ThemeService,\n    readonly languageService: LanguageService,');
}
fs.writeFileSync(tsPath, tsContent);

// Fix HTML errors
const files = fs.readdirSync(dir).filter(f => f.endsWith('.html'));
for (const file of files) {
  const p = path.join(dir, file);
  let html = fs.readFileSync(p, 'utf-8');
  
  html = html.replace(/length > \{\{ '0" class="([^"]+)">' \| translate \}\}/g, 'length > 0">\n<ul class="$1">');
  html = html.replace(/length > \{\{ '0" class="([^"]+)" aria-label="([^"]+)">' \| translate \}\}/g, 'length > 0">\n<ul class="$1" aria-label="$2">');
  html = html.replace(/> \{\{ '0" class="row g-4">' \| translate \}\}/g, '> 0">\n<div class="row g-4">');
  
  fs.writeFileSync(p, html);
}
console.log('Fixed files');
