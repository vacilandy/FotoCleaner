using System.IO;
using OpenCvSharp;

namespace FotoCleaner.Services;

public sealed class PerceptualHashService : IHashService
{
    private static readonly HashSet<string> ImageExtensions = [
        ".jpg", ".jpeg", ".jpe", ".jfif", ".png", ".bmp", ".dib", ".gif", ".webp",
        ".tif", ".tiff", ".ico", ".avif", ".heic", ".heif", ".jp2", ".j2k", ".jpf",
        ".raw", ".cr2", ".cr3", ".nef", ".arw", ".dng", ".orf", ".rw2", ".pef"
    ];

    public Task<HashResult> ComputeAsync(string path, CancellationToken cancellationToken)
    {
        return Task.Run(() => HashImage(path), cancellationToken);
    }

    public static bool IsSupported(string path) => ImageExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

    private static HashResult HashImage(string path)
    {
        try
        {
            // Evita pasar rutas Unicode directamente al wrapper nativo de OpenCV.
            var fileBytes = File.ReadAllBytes(path);
            using var source = Cv2.ImDecode(fileBytes, ImreadModes.Grayscale);
            if (source.Empty()) throw new InvalidDataException("No se pudo leer la imagen.");
            return new HashResult(DHash(source), null, source.Width, source.Height);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"No se pudo procesar la imagen: {Path.GetFileName(path)}", ex);
        }
    }

    private static ulong DHash(Mat input)
    {
        using var gray = new Mat();
        using var resized = new Mat();
        if (input.Channels() == 1) input.CopyTo(gray); else Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.Resize(gray, resized, new OpenCvSharp.Size(9, 8));
        ulong hash = 0;
        for (var row = 0; row < 8; row++) for (var column = 0; column < 8; column++)
        {
            hash <<= 1;
            if (resized.At<byte>(row, column) > resized.At<byte>(row, column + 1)) hash |= 1;
        }
        return hash;
    }
}
