using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using CvSize = OpenCvSharp.Size;
using CvPoint = OpenCvSharp.Point;
using ImgSize = SixLabors.ImageSharp.Size;
using ImgRectangle = SixLabors.ImageSharp.Rectangle;

namespace JameJafari.Api.Services;

public class ImageProcessingService(IWebHostEnvironment env, ILogger<ImageProcessingService> logger)
{
    private const int AvatarSize = 512;
    private const int DocumentMaxEdge = 1600;
    private const double MinDocumentScore = 0.42;
    private readonly Lazy<string?> _faceCascadePath = new(() => ResolveFaceCascadePathStatic(env));

    public bool CanProcess(IFormFile file) => FileStorageService.IsImageUpload(file);

    public async Task<MemoryStream> ProcessToJpegAsync(Stream input, ImageProcessProfile profile, CancellationToken ct = default)
    {
        var bytes = await ReadAllBytesAsync(input, ct);
        return profile switch
        {
            ImageProcessProfile.Avatar => await ProcessAvatarAsync(bytes, ct),
            ImageProcessProfile.Document => await ProcessDocumentAsync(bytes, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(profile))
        };
    }

    static async Task<byte[]> ReadAllBytesAsync(Stream input, CancellationToken ct)
    {
        if (input.CanSeek) input.Position = 0;
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    async Task<MemoryStream> ProcessAvatarAsync(byte[] bytes, CancellationToken ct)
    {
        using var oriented = await LoadOrientedAsync(bytes, ct);
        var crop = TryGetFaceSquare(oriented) ?? GetCenterSquare(oriented);

        using var cropped = oriented.Clone(ctx => ctx.Crop(crop).Resize(AvatarSize, AvatarSize));
        return await EncodeJpegAsync(cropped, 88, ct);
    }

    async Task<MemoryStream> ProcessDocumentAsync(byte[] bytes, CancellationToken ct)
    {
        using var oriented = await LoadOrientedAsync(bytes, ct);
        using var mat = ImageSharpToMat(oriented);

        if (TryFindBestDocumentQuad(mat, out var quad, out var score) && score >= MinDocumentScore)
        {
            using var warped = WarpDocument(mat, quad);
            using var resized = ResizeMatMaxEdge(warped, DocumentMaxEdge);
            return MatToJpegStream(resized, 78);
        }

        using var fallback = oriented.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new ImgSize(DocumentMaxEdge, DocumentMaxEdge)
        }));
        return await EncodeJpegAsync(fallback, 78, ct);
    }

    static async Task<Image<Rgba32>> LoadOrientedAsync(byte[] bytes, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(new MemoryStream(bytes), ct);
        image.Mutate(x => x.AutoOrient());
        return image.CloneAs<Rgba32>();
    }

    static ImgRectangle GetCenterSquare(Image image)
    {
        var side = Math.Min(image.Width, image.Height);
        var x = (image.Width - side) / 2;
        var y = (image.Height - side) / 2;
        return new ImgRectangle(x, y, side, side);
    }

    ImgRectangle? TryGetFaceSquare(Image<Rgba32> image)
    {
        var cascadePath = _faceCascadePath.Value;
        if (string.IsNullOrEmpty(cascadePath)) return null;

        try
        {
            using var mat = ImageSharpToMat(image);
            using var gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGRA2GRAY);

            using var cascade = new CascadeClassifier(cascadePath);
            var faces = cascade.DetectMultiScale(gray, 1.1, 5, HaarDetectionTypes.ScaleImage, new CvSize(60, 60));
            if (faces.Length == 0) return null;

            var face = faces
                .OrderByDescending(r => r.Width * r.Height)
                .ThenBy(r => r.Y)
                .ThenBy(r => r.X)
                .First();

            var padX = (int)(face.Width * 0.55);
            var padY = (int)(face.Height * 0.65);
            var cx = face.X + face.Width / 2;
            var cy = face.Y + face.Height / 2 - (int)(face.Height * 0.05);
            var side = Math.Max(face.Width + padX * 2, face.Height + padY * 2);

            var x = Math.Clamp(cx - side / 2, 0, Math.Max(0, image.Width - 1));
            var y = Math.Clamp(cy - side / 2, 0, Math.Max(0, image.Height - 1));
            side = Math.Min(side, Math.Min(image.Width - x, image.Height - y));
            if (side < 32) return null;
            return new ImgRectangle(x, y, side, side);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Face detection skipped");
            return null;
        }
    }

    static bool TryFindBestDocumentQuad(Mat src, out Point2f[] quad, out double score)
    {
        quad = [];
        score = 0;
        Point2f[]? bestQuad = null;
        var bestScore = 0.0;

        foreach (var useCanny in new[] { true, false })
        {
            foreach (var candidate in FindQuadCandidates(src, useCanny))
            {
                var s = ScoreDocumentQuad(candidate, src.Width, src.Height);
                if (s > bestScore)
                {
                    bestScore = s;
                    bestQuad = candidate;
                }
            }
        }

        if (bestQuad is null) return false;
        quad = OrderQuad(bestQuad);
        score = bestScore;
        return true;
    }

    static IEnumerable<Point2f[]> FindQuadCandidates(Mat src, bool useCanny)
    {
        using var gray = new Mat();
        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
        using var blur = new Mat();
        Cv2.GaussianBlur(gray, blur, new CvSize(5, 5), 0);
        using var edges = new Mat();

        if (useCanny)
            Cv2.Canny(blur, edges, 75, 200);
        else
            Cv2.AdaptiveThreshold(blur, edges, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);

        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new CvSize(3, 3));
        using var closed = new Mat();
        Cv2.MorphologyEx(edges, closed, MorphTypes.Close, kernel);

        Cv2.FindContours(closed, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        var imgArea = src.Width * src.Height;

        foreach (var contour in contours.OrderByDescending(c => Cv2.ContourArea(c)))
        {
            var area = Cv2.ContourArea(contour);
            if (area < imgArea * 0.08 || area > imgArea * 0.98) continue;

            var peri = Cv2.ArcLength(contour, true);
            var approx = Cv2.ApproxPolyDP(contour, 0.02 * peri, true);
            if (approx.Length != 4) continue;

            var pts = approx.Select(p => new Point2f(p.X, p.Y)).ToArray();
            if (!Cv2.IsContourConvex(approx)) continue;
            yield return pts;
        }
    }

    static double ScoreDocumentQuad(Point2f[] pts, int width, int height)
    {
        using var contour = new Mat(4, 1, MatType.CV_32SC2);
        for (var i = 0; i < 4; i++)
            contour.Set(i, 0, new CvPoint((int)pts[i].X, (int)pts[i].Y));

        var area = Cv2.ContourArea(contour);
        var imgArea = width * height;
        var areaRatio = area / imgArea;
        if (areaRatio < 0.10 || areaRatio > 0.92) return 0;

        var rect = Cv2.MinAreaRect(pts);
        var size = rect.Size;
        var minSide = Math.Min(size.Width, size.Height);
        var maxSide = Math.Max(size.Width, size.Height);
        if (minSide < 1) return 0;

        var aspect = maxSide / minSide;
        if (aspect < 0.35 || aspect > 3.2) return 0;

        var angleScore = 1.0 - Math.Min(1.0, Math.Abs(rect.Angle) / 45.0);
        var areaScore = 1.0 - Math.Abs(areaRatio - 0.55) / 0.55;
        var aspectScore = aspect is >= 0.6f and <= 2.2f ? 1.0 : 0.55;

        return Math.Clamp(areaScore * 0.45 + aspectScore * 0.25 + angleScore * 0.30, 0, 1);
    }

    static Point2f[] OrderQuad(Point2f[] pts)
    {
        var ordered = pts.OrderBy(p => p.Y).ToArray();
        var top = ordered.Take(2).OrderBy(p => p.X).ToArray();
        var bottom = ordered.Skip(2).OrderBy(p => p.X).ToArray();
        return [top[0], top[1], bottom[1], bottom[0]];
    }

    static Mat WarpDocument(Mat src, Point2f[] quad)
    {
        var widthA = Distance(quad[1], quad[0]);
        var widthB = Distance(quad[2], quad[3]);
        var maxW = (int)Math.Max(widthA, widthB);

        var heightA = Distance(quad[3], quad[0]);
        var heightB = Distance(quad[2], quad[1]);
        var maxH = (int)Math.Max(heightA, heightB);

        maxW = Math.Max(maxW, 1);
        maxH = Math.Max(maxH, 1);

        Point2f[] dst =
        [
            new(0, 0),
            new(maxW - 1, 0),
            new(maxW - 1, maxH - 1),
            new(0, maxH - 1)
        ];

        using var matrix = Cv2.GetPerspectiveTransform(quad, dst);
        var warped = new Mat();
        Cv2.WarpPerspective(src, warped, matrix, new CvSize(maxW, maxH));
        return warped;
    }

    static float Distance(Point2f a, Point2f b) => MathF.Sqrt(MathF.Pow(a.X - b.X, 2) + MathF.Pow(a.Y - b.Y, 2));

    static Mat ResizeMatMaxEdge(Mat src, int maxEdge)
    {
        var longest = Math.Max(src.Width, src.Height);
        if (longest <= maxEdge) return src.Clone();
        var scale = maxEdge / (double)longest;
        var resized = new Mat();
        Cv2.Resize(src, resized, new CvSize((int)(src.Width * scale), (int)(src.Height * scale)));
        return resized;
    }

    static MemoryStream MatToJpegStream(Mat mat, int quality)
    {
        var encoded = mat.ImEncode(".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, quality));
        var ms = new MemoryStream(encoded);
        ms.Position = 0;
        return ms;
    }

    static async Task<MemoryStream> EncodeJpegAsync(Image image, int quality, CancellationToken ct)
    {
        var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = quality }, ct);
        ms.Position = 0;
        return ms;
    }

    static Mat ImageSharpToMat(Image<Rgba32> image)
    {
        var mat = new Mat(image.Height, image.Width, MatType.CV_8UC4);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var p = row[x];
                    mat.Set(y, x, new Vec4b(p.B, p.G, p.R, p.A));
                }
            }
        });
        return mat;
    }

    static string? ResolveFaceCascadePathStatic(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Assets", "haarcascade_frontalface_default.xml");
        return File.Exists(path) ? path : null;
    }
}
