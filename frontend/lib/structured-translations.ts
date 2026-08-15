// Professional Persian Translation System
// Structured hierarchy for NovaShop

// Translation categories:
// auth.* - Authentication
// header.* - Header navigation
// hero.* - Hero section
// features.* - Feature cards
// featured.* - Featured products
// categories.* - Product categories
// cta.* - Call to action
// product.* - Product pages
// cart.* - Shopping cart
// checkout.* - Checkout process
// order.* - Order management
// search.* - Search functionality
// pagination.* - Pagination
// errors.* - Error messages
// loading.* - Loading states
// status.* - Status labels
// button.* - Button labels
// form.* - Form labels
// validation.* - Validation messages

export const structTranslations = {
  // Auth system
  'auth': {
    'login': {
      'title': 'ورود به حساب کاربری',
      'description': 'به حساب کاربری خود وارد شوید',
      'username': 'نام کاربری',
      'password': 'رمز عبور',
      'submit': 'ورود',
      'loading': 'در حال ورود...',
      'failed': 'ورود ناموفق',
      'success': 'با موفقیت وارد شدید',
      'required': 'لطفاً نام کاربری و رمز عبور را وارد کنید',
      'invalid': 'نام کاربری یا رمز عبور نادرست است',
    },
    'register': {
      'title': 'ایجاد حساب کاربری',
      'description': 'عضو شوید و خرید کنید',
      'username': 'نام کاربری',
      'email': 'ایمیل',
      'password': 'رمز عبور',
      'confirm': 'تأیید رمز عبور',
      'submit': 'ایجاد حساب',
      'loading': 'در حال ایجاد حساب...',
      'success': 'حساب کاربری با موفقیت ایجاد شد',
      'exists': 'این نام کاربری قبلاً ثبت شده است',
      'mismatch': 'رمز عبور تأیید شده مطابق رمز عبور نیست',
    },
    'forgot': {
      'title': 'فراموشی رمز عبور',
      'description': 'رمز عبور خود را بازیابی کنید',
      'email': 'آدرس ایمیل',
      'submit': 'ارسال لینک بازیابی',
      'success': 'لینک بازیابی رمز عبور به ایمیل شما ارسال شد',
      'backToLogin': 'بازگشت به ورود',
    },
  },

  // Header & Navigation
  'header': {
    'home': 'خانه',
    'shop': 'فروشگاه',
    'categories': 'دسته‌بندی‌ها',
    'about': 'درباره ما',
    'contact': 'تماس با ما',
    'cart': 'سبد خرید',
    'login': 'ورود',
    'register': 'ثبت‌نام',
    'logout': 'خروج',
    'language': 'زبان',
    'dashboard': 'داشبورد',
    'admin': 'پنل مدیریت',
  },

  // Hero Section
  'hero': {
    'title': 'دنیایی از عروسک‌های بافتنی دست‌ساز',
    'subtitle': 'عروسک‌هایی که با عشق بافته شده‌اند',
    'description': 'هر عروسک با دست و از نخ‌های مرغوب بافته می‌شود — منحصربه‌فرد، نرم و آماده برای هدیه دادن به عزیزان شما',
    'cta': 'مشاهده عروسک‌ها',
    'explore': 'دسته‌بندی‌ها',
  },

  // Features Section
  'features': {
    'title': 'چرا محصولات ما؟',
    'handmade': {
      'title': 'دست‌ساز و باکیفیت',
      'description': 'هر عروسک توسط هنرمندان ماهر با دقت بافته می‌شود',
    },
    'premium': {
      'title': 'بهترین مواد اولیه',
      'description': 'نخ‌های ضدحساسیت و ایمن برای کودکان',
    },
    'shipping': {
      'title': 'ارسال رایگان',
      'description': 'ارسال رایگان برای سفارش‌های بالای ۵۰۰ هزار تومان',
    },
  },

  // Featured Products
  'featured': {
    'title': 'عروسک‌های محبوب',
    'viewAll': 'مشاهده همه',
  },

  // Categories
  'categories': {
    'title': 'دسته‌بندی محصولات',
    'viewAll': 'مشاهده همه دسته‌بندی‌ها',
  },

  // Call to Action
  'cta': {
    'title': 'آماده پیدا کردن دوست جدیدتان هستید؟',
    'description': 'مجموعه عروسک‌های دست‌ساز ما را ببینید — هرکدام تنها و منتظر یک خانه گرم است',
    'button': 'شروع خرید',
  },

  // Product Pages
  'product': {
    'title': 'محصولات',
    'details': 'جزئیات محصول',
    'addToCart': 'افزودن به سبد خرید',
    'buyNow': 'خرید سریع',
    'outOfStock': 'ناموجود',
    'inStock': 'موجود',
    'price': 'قیمت',
    'quantity': 'تعداد',
    'quantityLabel': 'تعداد محصول',
    'total': 'مجموع',
    'description': 'توضیحات',
    'reviews': 'نظرات',
    'writeReview': 'ثبت نظر',
    'loginToReview': 'برای ثبت نظر وارد شوید',
    'related': 'محصولات مشابه',
    'back': 'بازگشت',
    'addedToCart': 'با موفقیت به سبد خرید اضافه شد',
    'loginRequired': 'لطفاً ابتدا وارد شوید',
    'handmadeNote': 'این محصول با دست و با عشق بافته شده است',
    'shippingNote': 'هزینه ارسال به صورت رایگان برای سفارش‌های بالای ۵۰۰ هزار تومان انجام می‌شود',
    'paymentNote': 'پرداخت امن',
  },

  // Cart
  'cart': {
    'title': 'سبد خرید',
    'empty': 'سبد خرید شما خالی است',
    'emptyDescription': 'هنوز محصولی اضافه نکرده‌اید',
    'checkout': 'تکمیل خرید',
    'continue': 'ادامه خرید',
    'subtotal': 'زیرمجموع',
    'total': 'مبلغ کل',
    'remove': 'حذف',
    'item': 'کالا',
    'items': 'کالا',
    'shipping': 'هزینه ارسال',
    'quantityUpdate': 'به‌روزرسانی تعداد',
    'removeItem': 'حذف محصول',
  },

  // Checkout
  'checkout': {
    'title': 'تکمیل سفارش',
    'shipping': 'اطلاعات ارسال',
    'payment': 'اطلاعات پرداخت',
    'summary': 'خلاصه سفارش',
    'fullName': 'نام و نام خانوادگی',
    'email': 'آدرس ایمیل',
    'phone': 'شماره موبایل',
    'address': 'آدرس کامل',
    'city': 'شهر',
    'postalCode': 'کد پستی',
    'placeOrder': 'ثبت سفارش',
    'processing': 'در حال پردازش...',
    'paymentMethod': 'روش پرداخت: پرداخت در محل',
    'paymentSuccessful': 'پرداخت با موفقیت انجام شد',
    'orderPlaced': 'سفارش با موفقیت ثبت شد',
    'viewOrder': 'مشاهده سفارش',
    'shippingFree': 'رایگان',
  },

  // Orders
  'order': {
    'title': 'سفارش‌ها',
    'detailTitle': 'جزئیات سفارش',
    'thankYou': 'با تشکر از شما',
    'description': 'سفارش شما با موفقیت ثبت شده است',
    'id': 'شماره سفارش',
    'status': 'وضعیت',
    'date': 'تاریخ',
    'total': 'مبلغ کل',
    'shippingTo': 'آدرس ارسال',
    'continueShopping': 'ادامه خرید',
    'backToHome': 'بازگشت به خانه',
    'items': 'محصولات',
    'quantity': 'تعداد',
    'unitPrice': 'قیمت واحد',
    'subTotal': 'زیرمجموع',
  },

  // Search
  'search': {
    'placeholder': 'جستجوی محصولات...',
    'button': 'جستجو',
    'noResults': 'محصولی یافت نشد',
    'clearFilter': 'پاک کردن فیلتر',
    'resultsFound': 'نتیجه‌ای یافت شد',
    'resultsFoundPlural': 'نتیجه‌ای یافت شد',
  },

  // Pagination
  'pagination': {
    'previous': 'قبلی',
    'next': 'بعدی',
    'page': 'صفحه',
    'of': 'از',
    'goto': 'برو به صفحه',
    'itemsPerPage': 'آیتم‌های هر صفحه',
  },

  // Errors & Messages
  'error': {
    'generic': 'خطایی رخ داده است. لطفاً دوباره تلاش کنید',
    'network': 'خطا در ارتباط با سرور. لطفاً اتصال اینترنت خود را بررسی کنید',
    'notFound': 'موردی که به دنبال آن هستید پیدا نشد',
    'unauthorized': 'برای ادامه، لطفاً وارد حساب کاربری خود شوید',
    'forbidden': 'شما اجازه دسترسی به این بخش را ندارید',
    'addToCart': 'افزودن به سبد خرید ناموفق بود',
    'removeItem': 'حذف کالا ناموفق بود',
    'updateQuantity': 'بروزرسانی تعداد ناموفق بود',
    'checkout': 'تکمیل سفارش ناموفق بود',
    'loginRequired': 'لطفاً ابتدا وارد شوید',
    'paymentFailed': 'پرداخت ناموفق بود. لطفاً دوباره تلاش کنید',
    'serverError': 'خطای سرور. لطفاً بعداً دوباره تلاش کنید',
  },

  // Loading States
  'loading': {
    'products': 'در حال دریافت محصولات...',
    'categories': 'در حال دریافت دسته‌بندی‌ها...',
    'cart': 'در حال بارگذاری سبد خرید...',
    'order': 'در حال دریافت اطلاعات سفارش...',
    'checkout': 'در حال پردازش سفارش...',
    'payment': 'در حال تأیید پرداخت...',
    'submit': 'در حال ارسال...',
    'general': 'در حال بارگذاری...',
  },

  // Status Labels
  'status': {
    'pending': 'در انتظار',
    'processing': 'در حال پردازش',
    'confirmed': 'تأیید شده',
    'paid': 'پرداخت شده',
    'shipped': 'ارسال شده',
    'delivered': 'تحویل شده',
    'cancelled': 'لغو شده',
    'failed': 'ناموفق',
    'refunded': 'بازپرداخت شده',
    'partial': 'جزئی',
  },

  // Button Labels
  'button': {
    'submit': 'ارسال',
    'save': 'ذخیره',
    'cancel': 'انصراف',
    'delete': 'حذف',
    'edit': 'ویرایش',
    'create': 'ایجاد',
    'add': 'افزودن',
    'remove': 'حذف',
    'update': 'بروزرسانی',
    'send': 'ارسال',
    'back': 'بازگشت',
    'next': 'بعدی',
    'previous': 'قبلی',
    'confirm': 'تأیید',
    'close': 'بستن',
    'ok': 'تأیید',
    'yes': 'بله',
    'no': 'خیر',
  },

  // Form Labels
  'form': {
    'required': 'این فیلد الزامی است',
    'optional': 'اختیاری',
    'invalid': 'مقدار وارد شده معتبر نیست',
    'minLength': 'باید حداقل {length} کاراکتر باشد',
    'maxLength': 'نباید بیش از {length} کاراکتر باشد',
    'pattern': 'فرمت وارد شده معتبر نیست',
    'email': 'لطفاً یک آدرس ایمیل معتبر وارد کنید',
    'phone': 'لطفاً یک شماره موبایل معتبر وارد کنید',
    'url': 'لطفاً یک URL معتبر وارد کنید',
    'number': 'لطفاً یک مقدار عددی معتبر وارد کنید',
    'date': 'لطفاً یک تاریخ معتبر وارد کنید',
    'time': 'لطفاً یک زمان معتبر وارد کنید',
    'file': 'لطفاً یک فایل معتبر انتخاب کنید',
    'fileSize': 'اندازه فایل باید کمتر از {size} باشد',
    'fileType': 'نوع فایل مجاز نیست',
  },

  // Admin Panel
  'admin': {
    'dashboard': {
      'title': 'داشبورد',
      'subtitle': 'نمای کلی فروشگاه',
      'totalSales': 'درآمد کل',
      'totalOrders': 'تعداد سفارش‌ها',
      'totalUsers': 'تعداد کاربران',
      'pendingOrders': 'سفارش‌های در انتظار',
    },
    'products': {
      'title': 'محصولات',
      'addNew': 'محصول جدید',
      'search': 'جستجوی محصولات',
      'name': 'نام محصول',
      'price': 'قیمت',
      'stock': 'موجودی',
      'category': 'دسته‌بندی',
      'status': 'وضعیت',
      'actions': 'عملیات',
      'edit': 'ویرایش',
      'delete': 'حذف',
      'create': 'ایجاد',
    },
    'categories': {
      'title': 'دسته‌بندی‌ها',
      'addNew': 'دسته‌بندی جدید',
      'name': 'نام دسته‌بندی',
      'description': 'توضیحات',
      'image': 'تصویر',
      'productsCount': 'تعداد محصولات',
    },
    'orders': {
      'title': 'سفارش‌ها',
      'orderId': 'شماره سفارش',
      'customer': 'مشتری',
      'date': 'تاریخ',
      'total': 'مبلغ کل',
      'status': 'وضعیت',
      'actions': 'عملیات',
      'view': 'مشاهده',
      'updateStatus': 'بروزرسانی وضعیت',
    },
    'users': {
      'title': 'کاربران',
      'username': 'نام کاربری',
      'email': 'ایمیل',
      'role': 'نقش',
      'status': 'وضعیت',
      'createdAt': 'تاریخ عضویت',
      'actions': 'عملیات',
      'addNew': 'کاربر جدید',
    },
    'payments': {
      'title': 'پرداخت‌ها',
      'gateway': 'درگاه پرداخت',
      'amount': 'مبلغ',
      'status': 'وضعیت',
      'transactionId': 'شماره تراکنش',
      'referenceNumber': 'شماره ارجاع',
      'date': 'تاریخ',
      'actions': 'عملیات',
    },
    'discounts': {
      'title': 'تخفیف‌ها',
      'code': 'کد تخفیف',
      'type': 'نوع',
      'value': 'مقدار',
      'minOrder': 'حداقل مبلغ سفارش',
      'startDate': 'تاریخ شروع',
      'endDate': 'تاریخ پایان',
      'isActive': 'فعال',
      'actions': 'عملیات',
    },
  },

  // Common
  'common': {
    'yes': 'بله',
    'no': 'خیر',
    'ok': 'تأیید',
    'cancel': 'انصراف',
    'close': 'بستن',
    'save': 'ذخیره',
    'edit': 'ویرایش',
    'delete': 'حذف',
    'create': 'ایجاد',
    'update': 'بروزرسانی',
    'submit': 'ارسال',
    'back': 'بازگشت',
    'next': 'بعدی',
    'previous': 'قبلی',
    'loading': 'در حال بارگذاری...',
    'error': 'خطا',
    'success': 'موفقیت',
    'warning': 'هشدار',
    'info': 'اطلاعات',
    'confirm': 'تأیید',
    'continue': 'ادامه',
    'view': 'مشاهده',
    'hide': 'مخفی کردن',
    'show': 'نمایش',
    'expand': 'گسترش',
    'collapse': 'جمع کردن',
  },
};

export type StructTranslations = typeof structTranslations;
export type TranslationKey = NestedKeys<StructTranslations>;

// Helper type for nested keys
type NestedKeys<T> = T extends object
  ? {
    [K in keyof T]: K extends string
      ? T[K] extends object
        ? `${K}.${NestedKeys<T[K]>}`
        : K
      : never;
    }[keyof T]
  : never;

// Helper function to get translation with fallback
export function getTranslation(
  translations: Record<string, unknown>,
  key: string,
  fallback?: string
): string {
  const keys = key.split('.');
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let value: any = translations;

  for (const k of keys) {
    if (value && typeof value === 'object' && k in value) {
      value = value[k];
    } else {
      return fallback ?? key;
    }
  }

  return typeof value === 'string' ? value : fallback ?? key;
}

// Get Persian translation helper
export const getFa = (key: string, fallback?: string): string =>
  getTranslation(structTranslations, key, fallback);

// Get English translation helper
export const getEn = (key: string, fallback?: string): string =>
  getTranslation(structTranslations, key, fallback);

// Get translation based on locale
export const getTranslationByLocale = (
  locale: 'fa' | 'en',
  key: string,
  fallback?: string
): string => {
  const translations = structTranslations;
  return getTranslation(translations, key, fallback);
};
