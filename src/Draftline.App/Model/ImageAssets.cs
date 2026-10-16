namespace Draftline.App.Model;
/// <summary>Portable raster resources. No remote URLs or executable SVG enter a document.</summary>
public static class ImageAssets
{
    public const int MaximumBytes = 5 * 1024 * 1024;
    public static long EncodedBytes(string data)
    {
        var comma = data.IndexOf(',');
        return comma < 0 ? 0 : (long)(data.Length - comma - 1) * 3 / 4;
    }

    public static string Mime(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 12 || bytes.Length > MaximumBytes)
            return "";
        if (bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return "image/png";
        if (bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255)
            return "image/jpeg";
        if (bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8))
            return "image/gif";
        if (bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
            return "image/webp";
        return "";
    }

    public static bool HasImage(WireframeElement e) => e.ImageWidth > 0 && e.ImageHeight > 0 && e.ImageData.Length <= MaximumBytes * 4 / 3 + 64 && new[]
    {
        "data:image/png;base64,",
        "data:image/jpeg;base64,",
        "data:image/gif;base64,",
        "data:image/webp;base64,"
    }.Any(e.ImageData.StartsWith);
    public static double Aspect(WireframeElement e) => HasImage(e) ? e.ImageWidth * e.CropWidth / (e.ImageHeight * e.CropHeight) : 1;
    public static Rect KeepAspect(Rect rect, WireframeElement e, double canvasWidth, double canvasHeight, bool fromHeight = false)
    {
        if (!e.KeepImageAspect || !HasImage(e))
            return rect;
        var ratio = Aspect(e);
        var width = fromHeight ? rect.Height * ratio : rect.Width;
        var height = width / ratio;
        var scale = Math.Min(1, Math.Min((canvasWidth - rect.X) / width, (canvasHeight - rect.Y) / height));
        return new(rect.X, rect.Y, Math.Max(CanvasGeometry.MinimumSize, width * scale), Math.Max(CanvasGeometry.MinimumSize, height * scale));
    }

    public static void Normalize(WireframeElement e)
    {
        e.ImageData ??= "";
        e.ImageName ??= "";
        if (e.ImageData.Length > 0)
        {
            var valid = false;
            if (HasImage(e) && double.IsFinite(e.ImageWidth) && double.IsFinite(e.ImageHeight) && e.ImageWidth * e.ImageHeight <= 32000000)
                try
                {
                    var prefix = e.ImageData.IndexOf(',');
                    var bytes = Convert.FromBase64String(e.ImageData[(prefix + 1)..]);
                    valid = e.ImageData.StartsWith("data:" + Mime(bytes) + ";base64,");
                }
                catch (FormatException)
                {
                }

            if (!valid)
            {
                e.ImageData = "";
                e.ImageWidth = e.ImageHeight = 0;
            }
        }

        static double Finite(double value, double fallback) => double.IsFinite(value) ? value : fallback;
        e.CropX = Math.Clamp(Finite(e.CropX, 0), 0, .99);
        e.CropY = Math.Clamp(Finite(e.CropY, 0), 0, .99);
        e.CropWidth = Math.Clamp(Finite(e.CropWidth, 1), .01, 1 - e.CropX);
        e.CropHeight = Math.Clamp(Finite(e.CropHeight, 1), .01, 1 - e.CropY);
        if (e.ImageFit is not ("contain" or "cover" or "stretch"))
            e.ImageFit = "contain";
    }
}
