const fs = require('fs');
const dict = JSON.parse(fs.readFileSync('src/app/translation-dict.json', 'utf-8'));

const arOverrides = {
  'Craft admin': 'إدارة الحرف',
  'Name': 'الاسم',
  'Description': 'الوصف',
  'Cancel': 'إلغاء',
  'Edit': 'تعديل',
  'Delete': 'حذف',
  'CraftConnect': 'كرافت كونكت',
  'Marketplace': 'السوق',
  'Craftsmen': 'الحرفيين',
  'Services': 'الخدمات',
  'Admin': 'المسؤول',
  'New craft': 'حرفة جديدة',
  'Add a service': 'إضافة خدمة',
  'Craft name': 'اسم الحرفة',
  'Saving...': 'جاري الحفظ...',
  'Catalog': 'الكتالوج',
  'Loading': 'جاري التحميل',
  'Administration': 'الإدارة',
  'Approve': 'موافقة',
  'Refuse': 'رفض',
  'Requests': 'الطلبات',
  'Board': 'اللوحة',
  'Day sheet': 'جدول اليوم',
  'Notifications': 'الإشعارات',
  'Craftsman': 'حرفي',
  'Customer': 'عميل',
  'Profile': 'الملف الشخصي',
  'Accept': 'قبول',
  'Decline': 'رفض',
  'Price': 'السعر',
  'Log in': 'تسجيل الدخول',
  'Sign in': 'تسجيل الدخول',
  'Email address': 'البريد الإلكتروني',
  'Password': 'كلمة المرور',
  'Create an account': 'إنشاء حساب',
  'Full name': 'الاسم الكامل',
  'I am a': 'أنا'
};

const arDict = {};
for (const key of Object.keys(dict)) {
  if (arOverrides[key]) {
    arDict[key] = arOverrides[key];
  } else {
    throw new Error(`Missing Arabic translation for: ${key}`);
  }
}

const content = `
export const EN_TRANSLATIONS: Record<string, string> = ${JSON.stringify(dict, null, 2)};
export const AR_TRANSLATIONS: Record<string, string> = ${JSON.stringify(arDict, null, 2)};
`;

fs.mkdirSync('src/app/i18n', { recursive: true });
fs.writeFileSync('src/app/i18n/translations.ts', content);
console.log('Created translations.ts');
