# جامعه جعفری (Jame-Jafari)

سامانه مدیریت مالی برای تشکل مذهبی **جامعه جعفری** — RTL، فارسی، تاریخ شمسی، PWA.

## ساختار پروژه

```
jame-jafari-source/
├── src/
│   ├── JameJafari.Api/              # ASP.NET Core 10 Web API، JWT، فیلتر دسترسی
│   ├── JameJafari.Core/             # Entities، DTOs، Enums، Constants، Validation
│   └── JameJafari.Infrastructure/   # EF Core، Services، Migrations، Caching
├── frontend/                        # Vue 3 + Vite PWA
└── .cursor/rules/                   # قوانین Agent برای توسعه یکپارچه
```

## پیش‌نیازها

- .NET 10 SDK
- SQL Server (LocalDB یا Express)
- Node.js 18+

## راه‌اندازی

### Backend

1. Connection string را در `src/JameJafari.Api/appsettings.json` تنظیم کنید.
2. اجرا:

```bash
cd src/JameJafari.Api
dotnet run
```

- API: `http://localhost:5093`
- Migration و seed در startup خودکار (`DbSeeder`) اعمال می‌شود.
- OpenAPI در Development: `/openapi/v1.json`

### Frontend (PWA)

```bash
cd frontend
npm install
npm run dev
```

- Dev server: `http://localhost:5173`
- Proxy: `/api` و `/uploads` → backend

### کاربر پیش‌فرض

| فیلد | مقدار |
|------|--------|
| نام کاربری | `admin` |
| رمز عبور | `admin@123` |

کاربر `admin` مدیر اصلی سیستم است؛ تمام دسترسی‌ها را دارد و از صفحه کاربران قابل حذف/ویرایش مستقیم نیست (فقط از پروفایل).

## معماری

### Backend (لایه‌ای)

| لایه | مسئولیت |
|------|---------|
| **Core** | مدل دامنه، DTO، `PermissionCodes`، `PasswordPolicy` — بدون وابستگی خارجی |
| **Infrastructure** | `AppDbContext`، سرویس‌های concrete، FusionCache، password hasher |
| **Api** | Controller نازک، `PermissionFilter`، `FileStorageService` |

**الگوهای کلیدی:**
- سرویس per-aggregate؛ بدون Repository/UoW جدا
- Soft delete روی `AuditableEntity` + فیلتر سراسری EF
- DTO به‌صورت `record` با DataAnnotations (پیام خطای فارسی)
- `[RequirePermission]` روی action/controller — منطق **OR** بین چند permission
- JWT: claim نوع `"permission"` برای هر کد دسترسی
- کش lookup (FusionCache) با invalidation پس از write

### Frontend

| بخش | مسئولیت |
|-----|---------|
| `views/` | صفحات CRUD + گزارش (الگوی FormHost) |
| `components/` | UI مشترک (ClearableInput، AppSelect، PersianDatePicker، …) |
| `composables/` | `useEntityForm`، `useFormValidation`، `useFormPage` |
| `stores/` | auth، lookups، theme، toast، dialog |
| `api/` | axios client + `ApiPaths` |

**الگوهای کلیدی:**
- Desktop: فرم inline در `FormHost`؛ Mobile: full-page + back در top bar
- دسترسی UI: `auth.hasPermission('module.action')`
- تاریخ: نمایش شمسی، ذخیره ISO میلادی
- فرم‌ها: `form-layout-adaptive` (۲ ستون / ۳ ستون در عرض زیاد)، `form-span-full` برای textarea/پیوست

## قابلیت‌ها

### تراکنش‌ها
- **درآمد:** شخص، حساب، مبلغ، نوع پرداخت، نوع هزینه، تاریخ، کد رهگیری، پیوست، توضیحات
- **هزینه:** حساب، مبلغ، نوع هزینه، تاریخ، کد رهگیری، پیوست، توضیحات
- پیوست: تصویر/PDF؛ multipart upload

### مدیریت
- **کاربران:** ایجاد/ویرایش، آواتار، دسترسی‌های granular، تغییر رمز جدا (`users.changepassword`)
- **اشخاص:** درخت خانوادگی (پدر/مادر)، پیشوند نام، آواتار
- **حساب‌ها، انواع هزینه، انواع عمومی** (واحد، پیشوند نام)

### تهیه غذا
- چند غذا در یک روز؛ ویرایش (`food.update`)
- مواد اولیه با واحد، قیمت پیشنهادی (میانگین وزنی از سوابق غذا + تراکنش هزینه)
- محاسبه خودکار هزینه کل و هزینه هر واحد

### گزارشات
- خلاصه درآمد/هزینه/مانده در بازه تاریخ
- موجودی حساب‌ها، تحلیل نوع هزینه، درآمد اشخاص، هزینه غذا

### PWA و UI
- تم‌ها: **زمردی** (پیش‌فرض)، **شب**، **زعفرانی**، **خاکستری**
- فونت IRANSans FaNum، RTL، FAB موبایل برای ایجاد رکورد

## سیاست رمز عبور

حداقل در backend (`PasswordPolicy`) و frontend (`passwordPolicy.js`):

- حداقل ۶ کاراکتر، حداکثر ۱۰۰
- حداقل یک حرف، یک عدد، یک نماد (غیر alphanumeric)
- اعمال در ایجاد/ویرایش کاربر و تغییر رمز پروفایل

## دسترسی‌ها (Permissions)

مدل **بدون نقش ثابت** — هر کاربر مجموعه‌ای از کدهای `{module}.{action}` دارد.

| ماژول | کدها |
|--------|------|
| accounts | `view`, `create`, `update`, `delete` |
| income | `view`, `create`, `update`, `delete` |
| cost | `view`, `create`, `update`, `delete` |
| users | `view`, `create`, `update`, `delete`, **`changepassword`** |
| persons | `view`, `create`, `update`, `delete` |
| costtypes | `view`, `create`, `update`, `delete` |
| food | `view`, `create`, `update`, `delete` |
| reports | `view` |
| generaltypes | `view`, `create`, `update`, `delete` |

کاتالوگ در `PermissionCodes.All` نگه‌داری می‌شود؛ `DbSeeder` کدهای جدید را sync و به admin اختصاص می‌دهد.

**مدیر اصلی:** username=`admin` — `SystemUsers.IsSystemAdmin()`؛ فقط از پروفایل قابل تغییر رمز/آواتار.

## Build Production

```bash
dotnet publish src/JameJafari.Api -c Release -o ./publish/api
cd frontend && npm run build
```

خروجی frontend: `frontend/dist`

## توسعه برای Agent / AI

قوانین پروژه در `.cursor/rules/*.mdc` تعریف شده‌اند:

| Rule | محدوده |
|------|--------|
| `project-overview.mdc` | همیشه — اهداف و مرزهای پروژه |
| `backend-dotnet.mdc` | `src/**/*.cs` |
| `frontend-vue.mdc` | `frontend/**/*` |
| `permissions-auth.mdc` | auth + permissions |
| `forms-ui-patterns.mdc` | فرم‌ها و UI |

هنگام افزودن feature جدید: permission در `PermissionCodes`، seed خودکار، route + `meta.permission`، `[RequirePermission]` در API، و الگوی FormHost در frontend.
