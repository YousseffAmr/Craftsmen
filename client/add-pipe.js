const fs = require('fs');
const path = require('path');
const srcDir = path.join(process.cwd(), 'src/app');

const files = fs.readdirSync(srcDir).filter(f => f.endsWith('.component.ts'));

for (const file of files) {
  const filePath = path.join(srcDir, file);
  let content = fs.readFileSync(filePath, 'utf-8');
  
  if (!content.includes('TranslatePipe')) {
    // Add import statement
    content = "import { TranslatePipe } from './translate.pipe';\n" + content;
    
    // Add to imports array
    content = content.replace(/imports:\s*\[([^\]]*)\]/, (match, p1) => {
      const parts = p1.trim();
      if (parts) {
        return `imports: [${parts}, TranslatePipe]`;
      }
      return `imports: [TranslatePipe]`;
    });
    
    fs.writeFileSync(filePath, content);
    console.log('Updated', file);
  }
}
