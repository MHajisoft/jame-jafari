using JameJafari.Core.Enums;
using JameJafari.Core.Helpers;
using JameJafari.Core.Options;
using Microsoft.Extensions.Options;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace JameJafari.Infrastructure.Services;

/// <summary>Dark condolence card with candles: person name + anniversary year count.</summary>
public class DeathAnniversaryCondolenceImageService(IOptions<BaleOptions> options)
{
    private const int CanvasWidth = 1080;
    private const int CanvasHeight = 1350;
    private const float Margin = 36;
    private const float LogoSize = 96;

    private static readonly SKColor Night = SKColor.Parse("#0b0c10");
    private static readonly SKColor NightMid = SKColor.Parse("#14161c");
    private static readonly SKColor Panel = SKColor.Parse("#1a1d26");
    private static readonly SKColor PanelEdge = SKColor.Parse("#2a2f3a");
    private static readonly SKColor Mist = SKColor.Parse("#8b93a7");
    private static readonly SKColor SoftWhite = SKColor.Parse("#e8e6e1");
    private static readonly SKColor FlameCore = SKColor.Parse("#fff4c2");
    private static readonly SKColor FlameMid = SKColor.Parse("#ffb347");
    private static readonly SKColor FlameOuter = SKColor.Parse("#e85d04");
    private static readonly SKColor Wax = SKColor.Parse("#d6d0c4");
    private static readonly SKColor WaxShadow = SKColor.Parse("#9a9388");
    private static readonly SKColor CandleBase = SKColor.Parse("#3a3340");

    private static readonly Lazy<SKTypeface> RegularFace = new(() => LoadFace("IRANSans.ttf"));
    private static readonly Lazy<SKTypeface> BoldFace = new(() => LoadFace("IRANSans-Bold.ttf"));

    private readonly string _uploadsRoot = options.Value.UploadsRootPath;

    public string Create(string displayName, int yearsSinceDeath, MessengerKind messenger = MessengerKind.Bale)
    {
        DeathAnniversaryCondolenceCopy.SelfCheck();

        if (string.IsNullOrWhiteSpace(_uploadsRoot))
            throw new InvalidOperationException("مسیر آپلود تنظیم نشده است");

        var name = string.IsNullOrWhiteSpace(displayName) ? "—" : displayName.Trim();
        var yearsLine = DeathAnniversaryCondolenceCopy.YearsLine(yearsSinceDeath);

        using var bold = new SKFont(BoldFace.Value, 48) { Subpixel = true };
        using var titleFont = new SKFont(BoldFace.Value, 38) { Subpixel = true };
        using var verseFont = new SKFont(BoldFace.Value, 32) { Subpixel = true };
        using var yearsFont = new SKFont(BoldFace.Value, 50) { Subpixel = true };
        using var footerFont = new SKFont(BoldFace.Value, 24) { Subpixel = true };
        using var boldShaper = new SKShaper(BoldFace.Value);

        using var surface = SKSurface.Create(new SKImageInfo(CanvasWidth, CanvasHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        DrawDarkBackground(canvas);

        var panel = new SKRect(Margin + 20, Margin + 40, CanvasWidth - Margin - 20, CanvasHeight - Margin - 40);
        DrawPanel(canvas, panel);

        DrawCandle(canvas, panel.Left + 90, panel.Bottom - 70, 34, 220);
        DrawCandle(canvas, panel.Right - 90, panel.Bottom - 70, 34, 220);
        DrawCandle(canvas, CanvasWidth / 2f, panel.Bottom - 48, 28, 160);

        var contentLeft = panel.Left + 56;
        var contentRight = panel.Right - 56;
        var midX = CanvasWidth / 2f;

        using var mistPaint = new SKPaint { Color = Mist, IsAntialias = true };
        using var whitePaint = new SKPaint { Color = SoftWhite, IsAntialias = true };
        using var flamePaint = new SKPaint { Color = FlameMid, IsAntialias = true };

        DrawLogo(canvas, contentRight, panel.Top + 28);

        var y = panel.Top + 110;
        canvas.DrawShapedText(boldShaper, DeathAnniversaryCondolenceCopy.Verse, midX, y, SKTextAlign.Center, verseFont, mistPaint);

        y += 64;
        canvas.DrawShapedText(boldShaper, DeathAnniversaryCondolenceCopy.Title, midX, y, SKTextAlign.Center, titleFont, flamePaint);

        y += 28;
        using (var linePaint = new SKPaint { Color = SKColor.Parse("#4a4250"), IsAntialias = true, StrokeWidth = 1.5f })
            canvas.DrawLine(contentLeft + 100, y, contentRight - 100, y, linePaint);

        y += 110;
        var nameLines = Wrap(name, contentRight - contentLeft - 40, bold, boldShaper);
        foreach (var line in nameLines)
        {
            canvas.DrawShapedText(boldShaper, line, midX, y, SKTextAlign.Center, bold, whitePaint);
            y += 62;
        }

        y += 40;
        canvas.DrawShapedText(boldShaper, yearsLine, midX, y, SKTextAlign.Center, yearsFont, mistPaint);

        var footerY = panel.Bottom - 36;
        canvas.DrawShapedText(
            boldShaper, DeathAnniversaryCondolenceCopy.OrganizationName,
            midX, footerY, SKTextAlign.Center, footerFont, mistPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        if (data is null || data.Size < 1000)
            throw new InvalidOperationException("تولید تصویر تسلیت ناموفق بود");

        var folder = messenger switch
        {
            MessengerKind.Rubika => "rubika",
            MessengerKind.Telegram => "telegram",
            MessengerKind.WhatsApp => "whatsapp",
            _ => "bale"
        };
        var dir = Path.Combine(_uploadsRoot, folder);
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid():N}.png";
        using var output = File.OpenWrite(Path.Combine(dir, fileName));
        data.SaveTo(output);
        return $"{folder}/{fileName}";
    }

    static void DrawDarkBackground(SKCanvas canvas)
    {
        canvas.Clear(Night);
        using var mid = new SKPaint { Color = NightMid, IsAntialias = true };
        canvas.DrawOval(new SKRect(-120, CanvasHeight * 0.55f, CanvasWidth + 120, CanvasHeight + 180), mid);

        // Soft radial glow behind candles (bottom center)
        using var glow = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(
                new SKPoint(CanvasWidth / 2f, CanvasHeight - 160),
                420,
                [SKColor.Parse("#3a2818").WithAlpha(90), SKColors.Transparent],
                [0f, 1f],
                SKShaderTileMode.Clamp)
        };
        canvas.DrawRect(0, CanvasHeight * 0.45f, CanvasWidth, CanvasHeight * 0.55f, glow);
    }

    static void DrawPanel(SKCanvas canvas, SKRect panel)
    {
        using var fill = new SKPaint { Color = Panel, IsAntialias = true };
        canvas.DrawRoundRect(panel, 22, 22, fill);

        using var stroke = new SKPaint
        {
            Color = PanelEdge,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        canvas.DrawRoundRect(SKRect.Inflate(panel, -8, -8), 16, 16, stroke);
    }

    /// <summary>Candle standing on baseline Y (bottom of wax). Flame above.</summary>
    static void DrawCandle(SKCanvas canvas, float cx, float baselineY, float waxHalfW, float waxH)
    {
        var waxTop = baselineY - waxH;
        var waxRect = new SKRect(cx - waxHalfW, waxTop, cx + waxHalfW, baselineY);

        using (var basePaint = new SKPaint { Color = CandleBase, IsAntialias = true })
            canvas.DrawRoundRect(new SKRect(cx - waxHalfW - 10, baselineY - 10, cx + waxHalfW + 10, baselineY + 8), 6, 6, basePaint);

        using (var waxPaint = new SKPaint { Color = Wax, IsAntialias = true })
            canvas.DrawRoundRect(waxRect, 8, 8, waxPaint);

        using (var shade = new SKPaint { Color = WaxShadow.WithAlpha(90), IsAntialias = true })
            canvas.DrawRect(cx - waxHalfW * 0.15f, waxTop + 8, waxHalfW * 0.35f, waxH - 16, shade);

        // Wick
        using (var wick = new SKPaint { Color = SKColor.Parse("#2a2218"), IsAntialias = true, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round })
            canvas.DrawLine(cx, waxTop, cx, waxTop - 18, wick);

        // Flame (ellipse stack)
        var flameY = waxTop - 42;
        using (var outer = new SKPaint
        {
            Color = FlameOuter.WithAlpha(160),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 8)
        })
            canvas.DrawOval(new SKRect(cx - 22, flameY - 38, cx + 22, flameY + 18), outer);

        using (var mid = new SKPaint { Color = FlameMid, IsAntialias = true })
            canvas.DrawOval(new SKRect(cx - 14, flameY - 32, cx + 14, flameY + 10), mid);

        using (var core = new SKPaint { Color = FlameCore, IsAntialias = true })
            canvas.DrawOval(new SKRect(cx - 7, flameY - 22, cx + 7, flameY + 2), core);
    }

    static void DrawLogo(SKCanvas canvas, float right, float top)
    {
        var path = Path.Combine(AssetsDir, "logo.png");
        if (!File.Exists(path)) return;

        using var bitmap = SKBitmap.Decode(path);
        if (bitmap is null) return;

        using var image = SKImage.FromBitmap(bitmap);
        var scale = Math.Min(LogoSize / (float)bitmap.Width, LogoSize / (float)bitmap.Height);
        var w = bitmap.Width * scale;
        var h = bitmap.Height * scale;
        var dest = new SKRect(right - w, top, right, top + h);
        using var paint = new SKPaint { Color = SKColors.White.WithAlpha(200), IsAntialias = true };
        canvas.DrawImage(image, dest, new SKSamplingOptions(SKFilterMode.Linear), paint);
    }

    static List<string> Wrap(string text, float maxWidth, SKFont font, SKShaper shaper)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = "";
        foreach (var word in words)
        {
            var trial = current.Length == 0 ? word : $"{current} {word}";
            if (shaper.Shape(trial, font).Width <= maxWidth)
            {
                current = trial;
                continue;
            }

            if (current.Length > 0)
                lines.Add(current);
            current = word;
        }

        if (current.Length > 0)
            lines.Add(current);
        return lines.Count == 0 ? [text] : lines;
    }

    static string AssetsDir =>
        Path.Combine(Path.GetDirectoryName(typeof(DeathAnniversaryCondolenceImageService).Assembly.Location)!, "Assets");

    static SKTypeface LoadFace(string fileName)
    {
        var path = Path.Combine(AssetsDir, fileName);
        if (!File.Exists(path))
            throw new InvalidOperationException("قلم تصویر تسلیت یافت نشد");
        return SKTypeface.FromFile(path)
            ?? throw new InvalidOperationException("بارگذاری قلم ناموفق بود");
    }
}
