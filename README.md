# جامعه جعفری (Jame-Jafari)

سامانه مدیریت مالی برای تشکل مذهبی **جامعه جعفری**

## ساختار پروژه

```
jame-jafari-source/
├── src/
│   ├── JameJafari.Api/          # ASP.NET Core 10 Web API
│   ├── JameJafari.Core/         # Entities, DTOs, Enums
│   └── JameJafari.Infrastructure/ # EF Core, Services, Migrations
└── frontend/                    # Vue 3 PWA
```

## پیش‌نیازها

- .NET 10 SDK
- SQL Server (LocalDB یا Express)
- Node.js 18+

## راه‌اندازی Backend

1. Connection string را در `src/JameJafari.Api/appsettings.json` تنظیم کنید.
2. اجرا:

```bash
cd src/JameJafari.Api
dotnet run
```

API روی `http://localhost:5093` اجرا می‌شود.  
Migration به‌صورت خودکار در startup اعمال می‌شود.

**کاربر پیش‌فرض:** `admin` / `admin123`

## راه‌اندازی Frontend (PWA)

1. اجرا:

```bash
cd frontend
npm install
npm run dev
```

Frontend روی `http://localhost:5173` اجرا می‌شود.

## قابلیت‌ها

### تراکنش‌ها
- **درآمد:** شخص، حساب، مبلغ، نوع پرداخت (نقد/کارتخوان/چک/انتقال بانکی)، نوع هزینه، پیوست، توضیحات
- **هزینه:** حساب، مبلغ، نوع هزینه، پیوست، توضیحات
- پیوست از گالری، آپلود یا دوربین

### مدیریت
- کاربران (نام کاربری، ایمیل، موبایل، آواتار، فعال/غیرفعال)
- اشخاص (نام، نام خانوادگی، جنسیت، پدر/مادر، پیشوند سفر، آدرس، ...)
- حساب‌های مالی
- انواع هزینه (با پرچم مواد اولیه و واحد)

### تهیه غذا
- ثبت چند غذا در یک روز
- مواد اولیه با قیمت پیشنهادی (میانگین تاریخی)
- محاسبه خودکار هزینه هر واحد

### گزارشات (مدیر)
- موجودی حساب‌ها
- تحلیل بر اساس نوع هزینه
- درآمد اشخاص
- هزینه تهیه غذا

### PWA
- تم‌ها: روشن، تاریک، جنگلی، لیمویی، شیرازی، طلایی، اقیانوس
- فونت IRANSans FaNum
- تاریخ شمسی/میلادی
- دسترسی‌های موردبه‌مورد برای هر کاربر (بدون نقش ثابت)

## دسترسی‌ها (Permissions)

دسترسی‌ها به‌صورت **هر کاربر و هر مورد جداگانه** تعریف می‌شوند (مدل نقش Admin/User حذف شده است). هر کاربر صرفاً مجموعه‌ای از کدهای دسترسی را دارد که در صفحه «کاربران» به‌صورت موردبه‌مورد تنظیم می‌شود.

| Permission | توضیح |
|---|---|
| accounts.view / accounts.manage | مشاهده / مدیریت حساب‌ها |
| income.view / income.create / income.delete | درآمد |
| cost.view / cost.create / cost.delete | هزینه |
| users.view / users.manage | کاربران |
| persons.view / persons.manage | اشخاص |
| costtypes.view / costtypes.manage | انواع هزینه |
| food.view / food.manage | تهیه غذا |
| reports.view | گزارشات |
| generaltypes.manage | انواع عمومی |

کاربر پیش‌فرض `admin` با **همه** دسترسی‌ها ایجاد می‌شود.

## Build Production

```bash
# Backend
dotnet publish src/JameJafari.Api -c Release -o ./publish/api

# Frontend PWA
cd frontend && npm run build
```

فایل‌های build در `frontend/dist` قرار می‌گیرند.
