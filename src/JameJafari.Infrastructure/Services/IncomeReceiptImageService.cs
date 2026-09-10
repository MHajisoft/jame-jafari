using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Core.Helpers;
using JameJafari.Core.Options;
using Microsoft.Extensions.Options;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace JameJafari.Infrastructure.Services;

public class IncomeReceiptImageService(IOptions<BaleOptions> options)
{
    private const int CanvasWidth = 1080;
    private const float BorderOuter = 22;
    private const float BorderBand = 46;
    private const float InnerPad = 36;
    private const float LogoSize = 118;

    private static readonly SKColor OuterGreen = SKColor.Parse("#0d3d2e");
    private static readonly SKColor BandGreen = SKColor.Parse("#124a38");
    private static readonly SKColor Accent = SKColor.Parse("#15956f");
    private static readonly SKColor Gold = SKColor.Parse("#c4a35a");
    private static readonly SKColor GoldLight = SKColor.Parse("#e8d5a3");
    private static readonly SKColor Ivory = SKColor.Parse("#fffdf6");
    private static readonly SKColor TextColor = SKColor.Parse("#0f2920");
    private static readonly SKColor MutedColor = SKColor.Parse("#5a7a6d");

    private static readonly Lazy<SKTypeface> RegularFace = new(() => LoadFace("IRANSans.ttf"));
    private static readonly Lazy<SKTypeface> BoldFace = new(() => LoadFace("IRANSans-Bold.ttf"));

    private readonly string _uploadsRoot = options.Value.UploadsRootPath;

    public string Create(IncomeTransaction tx, MessengerKind messenger = MessengerKind.Bale)
    {
        IncomeReceiptCopy.SelfCheck();

        if (string.IsNullOrWhiteSpace(_uploadsRoot))
            throw new InvalidOperationException("مسیر آپلود تنظیم نشده است");

        var greeting = IncomeReceiptCopy.Greeting(
            tx.Person.Gender, tx.Person.FirstName, tx.Person.LastName, tx.Person.NamePrefix?.Name);
        var body = IncomeReceiptCopy.Body(tx.Amount, tx.PaymentType, tx.CostType.Name);
        var date = IncomeReceiptCopy.FormatJalali(tx.TransactionDate);
        var footer = IncomeReceiptCopy.OrganizationName;
        var bismillah = IncomeReceiptCopy.Bismillah;

        using var regular = new SKFont(RegularFace.Value, 34) { Subpixel = true };
        using var bold = new SKFont(BoldFace.Value, 38) { Subpixel = true };
        using var headerFont = new SKFont(BoldFace.Value, 42) { Subpixel = true };
        using var dateFont = new SKFont(BoldFace.Value, 36) { Subpixel = true };
        using var footerFont = new SKFont(BoldFace.Value, 28) { Subpixel = true };
        using var regularShaper = new SKShaper(RegularFace.Value);
        using var boldShaper = new SKShaper(BoldFace.Value);

        var contentLeft = BorderOuter + BorderBand + InnerPad;
        var contentRight = CanvasWidth - BorderOuter - BorderBand - InnerPad;
        var textWidth = contentRight - contentLeft;
        var greetingLines = Wrap(greeting, textWidth, bold, boldShaper);
        var bodyLines = Wrap(body, textWidth, regular, regularShaper);

        const float headerH = 132;
        const float nameTopGap = 72;
        const float greetingLh = 54;
        const float bodyLh = 50;
        var innerTop = BorderOuter + BorderBand + InnerPad;
        var bottomPad = BorderOuter + BorderBand + InnerPad;
        var cy = innerTop + headerH + nameTopGap;
        cy += greetingLines.Count * greetingLh + 16;
        cy += bodyLines.Count * bodyLh;
        const float footerGap = 40;
        var footerY = cy + footerGap;
        var height = (int)Math.Ceiling(footerY + 28 + bottomPad);

        using var surface = SKSurface.Create(new SKImageInfo(CanvasWidth, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        DrawReligiousBorder(canvas, CanvasWidth, height);

        var headerMidY = innerTop + headerH / 2;
        DrawLogo(canvas, contentRight, innerTop + (headerH - LogoSize) / 2);

        using var goldPaint = new SKPaint { Color = Gold, IsAntialias = true };
        using var muted = new SKPaint { Color = MutedColor, IsAntialias = true };
        using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };

        canvas.DrawShapedText(
            boldShaper, bismillah, CanvasWidth / 2f, headerMidY + 10, SKTextAlign.Center, headerFont, goldPaint);
        canvas.DrawShapedText(
            regularShaper, IncomeReceiptCopy.ForRtlShaper(date), contentLeft, headerMidY + 10, SKTextAlign.Left, dateFont, muted);

        cy = innerTop + headerH + nameTopGap;
        foreach (var line in greetingLines)
        {
            canvas.DrawShapedText(boldShaper, line, contentRight, cy, SKTextAlign.Right, bold, textPaint);
            cy += greetingLh;
        }

        cy += 16;
        DrawRtlParagraph(
            canvas, bodyLines, contentLeft, contentRight, cy, bodyLh,
            regularShaper, regular, textPaint, justify: true);

        canvas.DrawShapedText(
            boldShaper, footer, CanvasWidth / 2f, footerY, SKTextAlign.Center, footerFont, muted);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        if (data is null || data.Size < 1000)
            throw new InvalidOperationException("تولید تصویر رسید ناموفق بود");

        var folder = messenger == MessengerKind.Rubika ? "rubika" : "bale";
        var dir = Path.Combine(_uploadsRoot, folder);
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid():N}.png";
        using var output = File.OpenWrite(Path.Combine(dir, fileName));
        data.SaveTo(output);
        return $"{folder}/{fileName}";
    }

    static void DrawReligiousBorder(SKCanvas canvas, int w, int h)
    {
        canvas.Clear(OuterGreen);

        var outer = new SKRect(BorderOuter, BorderOuter, w - BorderOuter, h - BorderOuter);
        var inner = new SKRect(
            BorderOuter + BorderBand, BorderOuter + BorderBand,
            w - BorderOuter - BorderBand, h - BorderOuter - BorderBand);

        using (var goldEdge = new SKPaint { Color = Gold, Style = SKPaintStyle.Stroke, StrokeWidth = 5, IsAntialias = true })
            canvas.DrawRect(SKRect.Inflate(outer, 6, 6), goldEdge);

        using var bandClip = new SKPath { FillType = SKPathFillType.EvenOdd };
        bandClip.AddRect(outer);
        bandClip.AddRect(inner);
        canvas.Save();
        canvas.ClipPath(bandClip, antialias: true);
        using (var band = new SKPaint { Color = BandGreen, IsAntialias = true })
            canvas.DrawRect(outer, band);

        using var starFill = new SKPaint { Color = GoldLight, IsAntialias = true };
        using var starStroke = new SKPaint { Color = Gold, Style = SKPaintStyle.Stroke, StrokeWidth = 1.4f, IsAntialias = true };
        var bandMid = BorderBand / 2;
        DrawStarRow(canvas, inner.Left, outer.Top + bandMid, inner.Right, outer.Top + bandMid, 38, starFill, starStroke);
        DrawStarRow(canvas, inner.Left, outer.Bottom - bandMid, inner.Right, outer.Bottom - bandMid, 38, starFill, starStroke);
        DrawStarRow(canvas, outer.Left + bandMid, inner.Top, outer.Left + bandMid, inner.Bottom, 38, starFill, starStroke, vertical: true);
        DrawStarRow(canvas, outer.Right - bandMid, inner.Top, outer.Right - bandMid, inner.Bottom, 38, starFill, starStroke, vertical: true);

        DrawCornerStar(canvas, inner.Left, inner.Top, starFill, starStroke);
        DrawCornerStar(canvas, inner.Right, inner.Top, starFill, starStroke);
        DrawCornerStar(canvas, inner.Left, inner.Bottom, starFill, starStroke);
        DrawCornerStar(canvas, inner.Right, inner.Bottom, starFill, starStroke);
        canvas.Restore();

        using (var ivory = new SKPaint { Color = Ivory, IsAntialias = true })
            canvas.DrawRect(inner, ivory);

        using (var innerGold = new SKPaint { Color = Gold, Style = SKPaintStyle.Stroke, StrokeWidth = 3, IsAntialias = true })
            canvas.DrawRect(SKRect.Inflate(inner, -10, -10), innerGold);
        using (var innerGreen = new SKPaint { Color = Accent, Style = SKPaintStyle.Stroke, StrokeWidth = 1.2f, IsAntialias = true })
            canvas.DrawRect(SKRect.Inflate(inner, -16, -16), innerGreen);
    }

    static void DrawStarRow(
        SKCanvas canvas, float x0, float y0, float x1, float y1, float spacing,
        SKPaint fill, SKPaint stroke, bool vertical = false)
    {
        var len = vertical ? Math.Abs(y1 - y0) : Math.Abs(x1 - x0);
        var count = Math.Max(2, (int)(len / spacing));
        for (var i = 1; i < count; i++)
        {
            var t = i / (float)count;
            var x = x0 + (x1 - x0) * t;
            var y = y0 + (y1 - y0) * t;
            DrawEightPointStar(canvas, x, y, 9, fill, stroke);
        }
    }

    static void DrawCornerStar(SKCanvas canvas, float x, float y, SKPaint fill, SKPaint stroke)
        => DrawEightPointStar(canvas, x, y, 16, fill, stroke);

    static void DrawEightPointStar(SKCanvas canvas, float cx, float cy, float outerR, SKPaint fill, SKPaint stroke)
    {
        var innerR = outerR * 0.38f;
        using var path = new SKPath();
        for (var i = 0; i < 16; i++)
        {
            var r = i % 2 == 0 ? outerR : innerR;
            var a = (i * 22.5f - 90f) * (MathF.PI / 180f);
            var pt = new SKPoint(cx + r * MathF.Cos(a), cy + r * MathF.Sin(a));
            if (i == 0) path.MoveTo(pt);
            else path.LineTo(pt);
        }
        path.Close();
        canvas.DrawPath(path, fill);
        canvas.DrawPath(path, stroke);
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
        canvas.DrawImage(image, dest, new SKSamplingOptions(SKFilterMode.Linear));
    }

    static void DrawRtlParagraph(
        SKCanvas canvas,
        IReadOnlyList<string> lines,
        float left,
        float right,
        float y,
        float lineHeight,
        SKShaper shaper,
        SKFont font,
        SKPaint paint,
        bool justify)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var last = i == lines.Count - 1;
            DrawRtlLine(canvas, lines[i], left, right, y, shaper, font, paint, justify && !last);
            y += lineHeight;
        }
    }

    static void DrawRtlLine(
        SKCanvas canvas,
        string line,
        float left,
        float right,
        float y,
        SKShaper shaper,
        SKFont font,
        SKPaint paint,
        bool justify)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return;

        if (!justify || words.Length == 1)
        {
            canvas.DrawShapedText(shaper, line, right, y, SKTextAlign.Right, font, paint);
            return;
        }

        var widths = new float[words.Length];
        var wordsWidth = 0f;
        for (var i = 0; i < words.Length; i++)
        {
            widths[i] = shaper.Shape(words[i], font).Width;
            wordsWidth += widths[i];
        }

        var space = MeasureSpace(shaper, font);
        var extra = Math.Max(0, (right - left) - wordsWidth - space * (words.Length - 1));
        var gap = space + extra / (words.Length - 1);

        var x = right;
        for (var i = 0; i < words.Length; i++)
        {
            canvas.DrawShapedText(shaper, words[i], x, y, SKTextAlign.Right, font, paint);
            x -= widths[i] + gap;
        }
    }

    static float MeasureSpace(SKShaper shaper, SKFont font)
    {
        var withSpace = shaper.Shape("ا ا", font).Width;
        var glued = shaper.Shape("اا", font).Width;
        var space = withSpace - glued;
        return space > 1f ? space : font.Size * 0.28f;
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
        Path.Combine(Path.GetDirectoryName(typeof(IncomeReceiptImageService).Assembly.Location)!, "Assets");

    static SKTypeface LoadFace(string fileName)
    {
        var path = Path.Combine(AssetsDir, fileName);
        if (!File.Exists(path))
            throw new InvalidOperationException("قلم رسید یافت نشد");
        return SKTypeface.FromFile(path)
            ?? throw new InvalidOperationException("بارگذاری قلم رسید ناموفق بود");
    }
}
