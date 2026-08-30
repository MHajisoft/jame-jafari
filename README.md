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
├── AGENTS.md                        # دستورالعمل agent (portable؛ شامل ponytail)
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
2. (اختیاری) برای پیام‌رسان بله در توسعه محلی: فایل `appsettings.Development.local.json.example` را کپی کنید به `appsettings.Development.local.json` (در gitignore است) و `Bale:BotToken` را پر کنید — یا متغیر محیطی `BALE_BOT_TOKEN` بگذارید (مثلاً در `launchSettings.json` یا Docker `.env`).
3. اجرا:

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
- **بارگذاری:** `AppGlobalLoader` برای همه درخواست‌های axios (به‌جز `skipGlobalLoader`)؛ متن بر اساس عمل: بارگذاری / ارسال / ذخیره / حذف / همگام‌سازی؛ لیست‌ها قبل از داده → `AppSkeleton`

## قابلیت‌ها

### تراکنش‌ها
- **درآمد:** شخص، حساب، مبلغ، نوع پرداخت، نوع هزینه، تاریخ، کد رهگیری، **چند پیوست**، توضیحات
- **هزینه:** حساب، مبلغ، نوع هزینه، تاریخ، کد رهگیری، **چند پیوست**، توضیحات
- **پیوست‌ها:** چند فایل تصویر/PDF per تراکنش (`TransactionAttachment`)
  - دسترسی‌ها (بدون منو): `attachments.view` (لیست/پیش‌نمایش)، `attachments.add` (آپلود)، `attachments.delete` (حذف)
  - **ردیابی ثبت (بدون منو):** `audit.view` — آیتم «اطلاعات ثبت» در منوی سه‌نقطهٔ هر ردیف (نه در ستون‌های جدول)؛ مودال دسکتاپ / شیت موبایل با تاریخ ایجاد/ویرایش، نام کاربر و آواتار
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
- تم‌ها: **زمردی**، **تیره**، **زعفرانی**، **یاسی**، **آبی براق**، **آبی تیره**، **نئون آبی**، **نئون قرمز**
- نشان وضعیت (فعال/غیرفعال) از رنگ اصلی تم پیروی می‌کند؛ غیرفعال خنثی است
- کارت‌ها: جلوه شیشه‌ای (blur + highlight) در تم‌های روشن و آبی براق
- **انتخابگر تاریخ (موبایل):** در تنظیمات — «نوار پایین» (چرخاننده) یا «مودال» (تقویم)؛ فقط زیر ۷۶۸px؛ دسکتاپ همیشه مودال
- **واحد پول:** در تنظیمات — «ریال» یا «تومان» برای نمایش و ورود؛ **ذخیره در DB و API همیشه ریال** (۱ تومان = ۱۰ ریال)
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

## پیام‌رسانی (Message Center)

ادغام با [API بازوی بله](https://docs.bale.ai) (قابل گسترش به پیام‌رسان‌های دیگر) برای ارسال پیام متنی، تصویر/ویدیو، فایل و رسید درآمد.

### پیکربندی

| متغیر | توضیح |
|--------|--------|
| `BALE_BOT_TOKEN` | توکن بازو (از @botfather در بله) — **هرگز commit نکنید** |
| `BALE_DEFAULT_GROUP_CHAT_ID` | فقط برای **seed اولیه** کانال «گروه پیش‌فرض» — شناسه عددی (مثلاً `-1001234567890`) یا نام کاربری `@channel` (اختیاری) |
| `BALE_BOT_USERNAME` | نام کاربری بازو (بدون @) — برای لینک شروع گفتگو |
| `BALE_WEBHOOK_SECRET` | رمز هدر `X-Bale-Webhook-Secret` برای `POST /api/bale/webhook` |

در Docker: مقادیر را در `.env` قرار دهید (نمونه در `.env.example`).

**توسعه محلی (`dotnet run`):** توکن را در `appsettings.Development.local.json` با کلید `Bale:BotToken` بگذارید (نمونه: `appsettings.Development.local.json.example`) — نه داخل `Logging` و نه با نام `BALE_BOT_TOKEN` در JSON. جایگزین: `BALE_BOT_TOKEN` به‌عنوان متغیر محیطی.

**کانال‌ها:** پس از راه‌اندازی، کانال‌های پیام (نام + شناسه گفتگو/کانال + پیام‌رسان) از منوی **کانال‌های پیام** مدیریت می‌شوند. شناسه می‌تواند عددی (مثلاً `-1001234567890`) یا نام کاربری بله (`@channelname`) باشد.

**وب‌هوک:** آدرس `https://{دامنه}/api/bale/webhook` را در بله ثبت کنید. بدنهٔ هر درخواست یک شی `Update` است (`from`، `contact.phone_number` و غیره). `update_id` تکراری نادیده گرفته می‌شود. در صورت نبود وب‌هوک، دکمه «همگام‌سازی مخاطبین» از `getUpdates` با **offset** استفاده می‌کند — اولین فراخوانی صف قدیمی را با `offset=-1` خالی می‌کند و فقط آپدیت‌های بعدی پردازش می‌شوند.

### اتصال مخاطب (موبایل‌محور)

API بله ارسال مستقیم با شماره موبایل را پشتیبانی نمی‌کند؛ سامانه از **موبایل شخص** به‌عنوان شناسه کاربری استفاده می‌کند و `chat_id` را خودکار از طریق بازو به‌دست می‌آورد:

1. موبایل شخص در پرونده اشخاص ثبت شود — با ذخیرهٔ ایجاد/ویرایش، در صورت پیکربندی بازو، همگام‌سازی مخاطبین (همان منطق `sync-contacts`) خودکار اجرا می‌شود.
2. شخص لینک `/start person_{id}` را باز کند (یا با بازو گفتگو کند) — بازو دکمه **اشتراک شماره تماس** نمایش می‌دهد. فیلد `contact` فقط پس از فشردن این دکمه در وب‌هوک می‌آید؛ پیام `/start` فقط `from` دارد.
3. پس از اشتراک شماره، `Person.BaleChatId` و جدول `BaleContactLink` به‌روز می‌شوند — **قابل ویرایش دستی نیست**.

### قابلیت‌ها

- **مرکز پیام** (`/message-center`): انتخاب **پیام‌رسان** و مقصد **کانال/گروه**، **گروه اشخاص** یا **شخص**؛ متن + پیوست اختیاری (بدون فایل متن الزامی است؛ نوع پیوست از دیالوگ فایل: تصویر / ویدیو / صوت / سند؛ نوع پیام در سرور از روی فایل‌ها تعیین می‌شود؛ ۲–۱۰ فایل هم‌نوع با `sendMediaGroup`)؛ ویرایش متن/زیرنویس روی کانال؛ حذف از گفتگو / حذف از لیست از منوی سه‌نقطهٔ ردیف — **لاگ پیام‌ها صفحه‌بندی‌شده** (حذف از لیست/دیتابیس، پیوست‌های آپلودشده در `uploads/bale/` را هم حذف می‌کند — حذف از گفتگو فقط پیام بله را برمی‌دارد؛ پیوست‌های borrow‌شده از تراکنش درآمد حذف نمی‌شوند)
- **کانال‌های پیام** (`/message-channels`): CRUD کانال با نام نمایشی و `ExternalChatId` (عدد یا `@username`) — لیست صفحه‌بندی‌شده
- **گروه‌های اشخاص** (`/person-groups`): CRUD گروه با لیست اعضا — ارسال گروهی = یک پیام خصوصی به هر عضو — لیست صفحه‌بندی‌شده (lookupهای انتخاب در مرکز پیام بدون صفحه‌بندی)
- **رسید درآمد**: پس از **ثبت درآمد**، اگر موبایل شخص موجود باشد رسید **تصویری** (بدون متن/زیرنویس) به‌صورت خودکار از طریق پیام‌رسان ارسال می‌شود (بدون نیاز به `messages.send`)؛ در صورت خطا، درآمد ثبت می‌شود و هشدار نمایش داده می‌شود. تصویر با حاشیهٔ هندسی اسلامی است: لوگو راست، «به نام خدا» وسط، تاریخ عددی جلالی چپ؛ نام راست‌چین؛ متن رسمی «بدین‌وسیله اعلام می‌گردد… به‌صورت «نوع پرداخت» … بابت «نوع هزینه»…». «ارسال رسید» در منوی عملیات لیست درآمد برای ارسال مجدد دستی است و به `messages.send` نیاز دارد — اگر `chat_id` نباشد، یک‌بار `getUpdates` (همگام‌سازی مخاطبین) قبل از هشدار اجرا می‌شود
- **همگام‌سازی مخاطبین**: `POST /api/bale-messages/sync-contacts` — فقط آپدیت‌های جدید (نه صف قدیمی پس از اولین sync)

ارسال به گروه اشخاص: ویرایش دسته‌ای پشتیبانی نمی‌شود؛ هر عضو یک ردیف جدا در لاگ دارد.

### دسترسی‌ها

| ماژول | منو |
|--------|-----|
| `messages.*` | مرکز پیام |
| `messagechannels.*` | کانال‌های پیام |
| `persongroups.*` | گروه‌های اشخاص |

seed خودکار؛ فقط admin پیش‌فرض.

## توسعه برای Agent / AI

قوانین پروژه در `.cursor/rules/*.mdc` تعریف شده‌اند:

| Rule | محدوده |
|------|--------|
| `ponytail.mdc` | همیشه — [ponytail](https://github.com/DietrichGebert/ponytail): کمترین کد لازم، بدون حذف امنیت/دسترسی/اعتبارسنجی |
| `project-overview.mdc` | همیشه — اهداف، مرزهای پروژه، **نگهداری مستندات** |
| `AGENTS.md` | خلاصه قابل‌حمل برای agentهایی که فقط root `AGENTS.md` می‌خوانند |
| `backend-dotnet.mdc` | `src/**/*.cs` |
| `frontend-vue.mdc` | `frontend/**/*` |
| `permissions-auth.mdc` | auth + permissions |
| `forms-ui-patterns.mdc` | فرم‌ها، UI، overlay back، پیوست‌ها |
| `jame-jafari-theming` skill | `.cursor/skills/jame-jafari-theming/` — توکن رنگ و تم |
| SonarQube (اختیاری) | `.cursor/SONARQUBE.md` — skills `sonar-*`، agent `sonarqube-reviewer`، MCP در `.cursor/mcp.json` |

**الزام:** هر تغییر معنادار در رفتار یا معماری → به‌روزرسانی **`README.md`** و rule مرتبط در **همان تغییر**.

هنگام افزودن feature جدید: permission در `PermissionCodes`، seed خودکار، route + `meta.permission`، `[RequirePermission]` در API، الگوی FormHost در frontend، و **به‌روزرسانی README + rules**.
