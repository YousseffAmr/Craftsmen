const fs = require('fs');
const path = require('path');
const srcDir = path.join(process.cwd(), 'src/app');

// Dictionary to store English strings
const dictionary = {};

function processHtmlFile(filePath) {
  let html = fs.readFileSync(filePath, 'utf-8');
  let originalHtml = html;
  
  html = html.replace(/>([^<{}]+)</g, (match, p1) => {
    const text = p1.trim();
    if (text && !/^[0-9\W]+$/.test(text) && !text.includes('"') && !text.includes('=')) {
      dictionary[text] = text;
      const escapedText = text.replace(/'/g, "\\'");
      return match.replace(text, `{{ '${escapedText}' | translate }}`);
    }
    return match;
  });
  
  html = html.replace(/placeholder="([^"]+)"/g, (match, p1) => {
    const text = p1.trim();
    if (text && !text.includes('{{') && !/^[0-9\W]+$/.test(text)) {
      dictionary[text] = text;
      const escapedText = text.replace(/'/g, "\\'");
      return `[placeholder]="'${escapedText}' | translate"`;
    }
    return match;
  });

  html = html.replace(/aria-label="([^"]+)"/g, (match, p1) => {
    if (match.startsWith('[attr.aria-label]')) return match;
    const text = p1.trim();
    if (text && !text.includes('{{') && !/^[0-9\W]+$/.test(text)) {
      dictionary[text] = text;
      const escapedText = text.replace(/'/g, "\\'");
      return `[attr.aria-label]="'${escapedText}' | translate"`;
    }
    return match;
  });
  
  html = html.replace(/title="([^"]+)"/g, (match, p1) => {
    if (match.startsWith('[title]')) return match;
    const text = p1.trim();
    if (text && !text.includes('{{') && !/^[0-9\W]+$/.test(text)) {
      dictionary[text] = text;
      const escapedText = text.replace(/'/g, "\\'");
      return `[title]="'${escapedText}' | translate"`;
    }
    return match;
  });

  if (html !== originalHtml) {
    fs.writeFileSync(filePath, html);
    console.log('Processed', path.basename(filePath));
  }
}

const htmlFiles = fs.readdirSync(srcDir).filter(f => f.endsWith('.html')).map(f => path.join(srcDir, f));
htmlFiles.forEach(processHtmlFile);

fs.writeFileSync(path.join(srcDir, 'translation-dict.json'), JSON.stringify(dictionary, null, 2));
console.log('Saved translation-dict.json with', Object.keys(dictionary).length, 'entries');
