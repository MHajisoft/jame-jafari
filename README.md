# جامعه جعفری (Jame-Jafari)

سامانه مدیریت مالی برای تشکل مذهبی **جامعه جعفری** — RTL، فارسی، تاریخ شمسی، PWA.

## ساختار پروژه

```
jame-jafari-source/
├── src/
│   ├── JameJafari.Api/              # ASP.NET Core 10 Web API، JWT، فیلتر دسترسی
│   ├── JameJafari.Core/             # Entities، DTOs، Enums، Constants، Validation
│   └── JameJafari.Infrastructure/   # EF Core، Services، Migrations، Caching
├── frontend/                        # Vue 3 + Vite PWA (+ Dockerfile / nginx)
├── Dockerfile                       # image API
├── docker-compose.yml               # db + api + web
├── .env.example                     # نمونه متغیرهای Docker
└── .cursor/rules/                   # قوانین Agent برای توسعه یکپارچه
```

## پیش‌نیازها

- .NET 10 SDK
- SQL Server (LocalDB یا Express) — یا Docker
- Node.js 18+
- (اختیاری) Docker Desktop / Docker Engine + Compose

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

### Docker (publish / run)

```bash
cp .env.example .env
# ویرایش MSSQL_SA_PASSWORD و JWT_KEY
docker compose up -d --build
```

| سرویس | آدرس |
|--------|------|
| وب (nginx + SPA) | `http://localhost:8080` |
| API مستقیم | `http://localhost:5093` |
| SQL Server | `localhost:1433` (user `sa`) |

- Migration/seed هنگام استارت API اجرا می‌شود.
- فایل‌های آپلود در volume `uploads_data` می‌مانند.
- توقف: `docker compose down` — داده DB با `docker compose down -v` پاک می‌شود.

ساخت جداگانه:

```bash
docker build -t jamejafari-api -f Dockerfile .
docker build -t jamejafari-web ./frontend
```

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
| **Api** | Controller نازک، `PermissionFilter`، `FileStorageService`، **`ImageProcessingService`** (آواتار/سند) |

**الگوهای کلیدی:**
- سرویس per-aggregate؛ بدون Repository/UoW جدا
- Soft delete روی `AuditableEntity` + فیلتر سراسری EF
- قرارداد API: `*Request` برای ورودی، `*Response` برای خروجی (نه `*Dto`)
- پاسخ‌های auditable: `ResponseBase` (Audit مشترک)؛ با پیوست: `AttachmentResponseBase`
- `ResponseVisibility` (static): `Apply<T>` / `ApplyAttachments<T>` — fluent deep-clone (`WithoutAudit` / `ApplyVisibility`)
- Projection: ردیف slim در SQL → نگاشت در حافظه (`PersonDisplayNameHelper`، `AuditHelper.FromProjection`)
- `[RequirePermission]` روی action/controller — منطق **OR** بین چند permission
- JWT: claim نوع `"permission"` برای هر کد دسترسی
- کش lookup (FusionCache) با invalidation پس از write

### Frontend

| بخش | مسئولیت |
|-----|---------|
| `views/` | صفحات CRUD + گزارش (الگوی FormHost) |
| `components/` | UI مشترک (ClearableInput، AppSelect، PersianDatePicker، …) |
| `composables/` | `useEntityForm`، `useFormValidation`، `useFormPage`، **`useOverlayBack`** |
| `stores/` | auth، lookups، theme، uiPrefs، toast، dialog، **loading** (overlay سراسری API) |
| `api/` | axios client + `ApiPaths` |

**الگوهای کلیدی:**
- Desktop: فرم inline در `FormHost`؛ Mobile: full-page + back در top bar
- Mobile overlay (پیش‌نمایش پیوست، انتخابگر تاریخ، …): **`useOverlayBack`** — دکمه/ژesture back ابتدا overlay را می‌بندد
- دسترسی UI: `auth.hasPermission('module.action')`
- تاریخ: نمایش شمسی، ذخیره ISO میلادی
- فرم‌ها: `form-layout-adaptive` (۲ ستون / ۳ ستون در عرض زیاد)، `form-span-full` برای textarea/پیوست
- **بارگذاری:** `AppGlobalLoader` برای همه درخواست‌های axios (به‌جز `skipGlobalLoader`); لیست‌ها قبل از داده → `AppSkeleton`

## قابلیت‌ها

### تراکنش‌ها
- **درآمد:** شخص، حساب، مبلغ، نوع پرداخت، نوع هزینه، تاریخ، کد رهگیری، **چند پیوست**، توضیحات
- **هزینه:** حساب، مبلغ، نوع هزینه، تاریخ، کد رهگیری، **چند پیوست**، توضیحات
- **پیوست‌ها:** چند فایل تصویر/PDF per تراکنش (`TransactionAttachment`)
  - دسترسی‌ها (بدون منو): `attachments.view` (لیست/پیش‌نمایش)، `attachments.add` (آپلود)، `attachments.delete` (حذف)
  - **ردیابی ثبت (بدون منو):** `audit.view` — آیکون «اطلاعات ثبت» در عملیات هر ردیف (نه در ستون‌های جدول)؛ مودال دسکتاپ / شیت موبایل با تاریخ ایجاد/ویرایش، نام کاربر و آواتار
  - **ایجاد/ویرایش:** `TransactionAttachmentsField` — افزودن چند فایل؛ حذف تکی (در ویرایش: فوری via API)
  - **پردازش تصویر (سرور):** آپلود خام از مرورگر؛ API هنگام ذخیره — آواتار: تشخیص چهره + برش مربعی ۵۱۲px؛ تصویر پیوست: برش/چهارضلعی فاکتور (OpenCV) + JPEG؛ PDF بدون تغییر
  - **لیست:** آیکون per پیوست → پیش‌نمایش درون‌برنامه (`DocumentPreview`)
  - **API:** `POST/PUT` multipart — فیلد JSON `data` + چند فایل `documents`؛ `DELETE .../attachments/{attachmentId}` (`attachments.delete` + `income|cost.update`)
  - پیش‌نمایش موبایل: back (top bar / gesture) overlay را می‌بندد، نه خروج از صفحه

### مدیریت
- **کاربران:** ایجاد/ویرایش، آواتار، **ماتریس دسترسی** (`PermissionMatrix`)، تغییر رمز جدا (`users.changepassword`)
- **اشخاص:** درخت خانوادگی (پدر/مادر)، پیشوند نام، آواتار، **لقب**؛ وضعیت حیات فقط با بج «درگذشته» وقتی `IsDead` است (کنار نام، دسکتاپ/موبایل)؛ **تاریخ وفات** (`DeathDate`) فقط در فرم ویرایش/ایجاد — در لیست و PersonSelect نمایش داده نمی‌شود
- **آواتار:** آپلود خام؛ سرور تشخیص چهره (Haar) + برش + JPEG مربعی ۵۱۲px؛ جایگزینی/حذف، فایل قبلی از دیسک پاک می‌شود
- **حساب‌ها، انواع هزینه، انواع عمومی** (واحد، پیشوند نام) — هر سه با آیکون «اطلاعات ثبت» در `audit.view`

### تهیه غذا
- چند غذا در یک روز؛ ویرایش (`food.update`)
- مواد اولیه با واحد، قیمت پیشنهادی (میانگین وزنی از سوابق غذا + تراکنش هزینه)
- محاسبه خودکار هزینه کل و هزینه هر واحد

### گزارشات
- انتهای سایدبار؛ در موبایل قبل از تب «بیشتر»: خلاصه KPI + تب‌های جزئیات (حساب‌ها، نوع هزینه، اشخاص، غذا)
- فیلتر بازه تاریخ شمسی؛ نیاز به `reports.view`
- **گزارش سالگرد وفات** (`/reports/death-anniversaries`): درگذشتگان با `DeathDate` — تطابق سالگرد شمسی با **امروز / هفته جاری (شنبه–جمعه) / ماه جاری / فصل جاری**؛ نیاز به **`deathanniversaries.view`** (جدا از `reports.view`)

### PWA و UI
- **نصب اندروید (حالت تمام‌صفحه):** Chrome فقط روی **HTTPS** (یا localhost) PWA واقعی می‌سازد؛ `http://IP:8080` فقط میانبر Chrome است (نوار آدرس می‌ماند). برای production: دامنه + SSL (مثلاً Caddy / nginx + Let's Encrypt).
- تم‌ها: **زمردی**، **تیره**، **زعفرانی**، **یاسی**، **آبی براق**، **آبی تیره**
- کارت‌ها: جلوه شیشه‌ای (blur + highlight) در تم‌های روشن و آبی براق
- **انتخابگر تاریخ (موبایل):** در تنظیمات — «نوار پایین» (چرخاننده) یا «مودال» (تقویم)؛ فقط زیر ۷۶۸px؛ دسکتاپ همیشه مودال
- فونت IRANSans FaNum، RTL، FAB موبایل برای ایجاد رکورد
- **به‌روزرسانی نسخه (autoUpdate):** با انتشار بیلد جدید، سرویس‌ورکر به‌صورت خودکار فعال می‌شود و صفحه رفرش می‌شود؛ بررسی دوره‌ای هر ۳۰ دقیقه + هنگام بازگشت به تب.
- **خروج با بازگشت (موبایل):** در تب‌های اصلی، دکمه/ژست بازگشت سیستم پیام «برای خروج دوباره…» را نشان می‌دهد و با بار دوم (ظرف ۲ ثانیه) خارج می‌شود؛ لایه اول: شیت/اورلی → فرم → صفحات تو در تو → سپس خروج. تب‌های پایین با `replace` جابه‌جا می‌شوند تا تاریخچه روی هم انباشته نشود.

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
| deathanniversaries | `view` |
| generaltypes | `view`, `create`, `update`, `delete` |

**Lookups (فرم‌ها):** کاربر با `income.create` / `cost.create` بدون `accounts.view` می‌تواند از `GET /api/lookups/accounts` و `GET /api/lookups/cost-types` برای dropdown استفاده کند؛ APIهای مدیریت (`/api/accounts`, …) همچنان نیاز به `*.view` دارند.

**جستجوی اشخاص (PersonSelect):** `GET /api/lookups/persons?search=&gender=&page=&pageSize=` — فیلتر چندکلمه‌ای (همه توکن‌ها) روی نام/نام‌خانوادگی/لقب خود شخص و پدر/مادر؛ لیست slim در FusionCache و فیلتر در حافظه؛ شامل `isDead` برای نمایش وضعیت حیات. `GET /api/persons` برای CRUD باقی می‌ماند.

**AppSelect:** جستجو پیش‌فرض در دسکتاپ و موبایل؛ فیلتر چندکلمه‌ای (همه توکن‌ها) با نرمال‌سازی فارسی، هم‌راستا با PersonSelect.

کاتالوگ در `PermissionCodes.All` نگه‌داری می‌شود؛ `DbSeeder` کدهای جدید را sync و به admin اختصاص می‌دهد.

**مدیر اصلی:** username=`admin` — `SystemUsers.IsSystemAdmin()`؛ فقط از پروفایل قابل تغییر رمز/آواتار.

## Build Production

```bash
dotnet publish src/JameJafari.Api -c Release -o ./publish/api
cd frontend && npm run build
```

خروجی frontend: `frontend/dist`

یا با Docker: `docker compose up -d --build` (بخش Docker در بالا).

## توسعه برای Agent / AI

قوانین پروژه در `.cursor/rules/*.mdc` تعریف شده‌اند:

| Rule | محدوده |
|------|--------|
| `project-overview.mdc` | همیشه — اهداف، مرزهای پروژه، **نگهداری مستندات** |
| `backend-dotnet.mdc` | `src/**/*.cs` |
| `frontend-vue.mdc` | `frontend/**/*` |
| `permissions-auth.mdc` | auth + permissions |
| `forms-ui-patterns.mdc` | فرم‌ها، UI، overlay back، پیوست‌ها |
| `jame-jafari-theming` skill | `.cursor/skills/jame-jafari-theming/` — توکن رنگ و تم |
| SonarQube (اختیاری) | `.cursor/SONARQUBE.md` — skills `sonar-*`، agent `sonarqube-reviewer`، MCP در `.cursor/mcp.json` |

**الزام:** هر تغییر معنادار در رفتار یا معماری → به‌روزرسانی **`README.md`** و rule مرتبط در **همان تغییر**.

هنگام افزودن feature جدید: permission در `PermissionCodes`، seed خودکار، route + `meta.permission`، `[RequirePermission]` در API، الگوی FormHost در frontend، و **به‌روزرسانی README + rules**.
