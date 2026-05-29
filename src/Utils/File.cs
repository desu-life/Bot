using System.Text;
using Img = SixLabors.ImageSharp.Image;

namespace KanonBot;

public static partial class Utils
{
    public static string Byte2File(string fileName, byte[] buffer)
    {
        using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
        {
            fs.Write(buffer, 0, buffer.Length);
        }
        return Path.GetFullPath(fileName);
        ;
    }

    public static Stream LoadFile2ReadStream(string filePath)
    {
        var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return fs;
    }

    public static async Task<byte[]> LoadFile2Byte(string filePath)
    {
        using var fs = LoadFile2ReadStream(filePath);
        byte[] bt = new byte[fs.Length];
        var mem = new Memory<Byte>(bt);
        await fs.ReadExactlyAsync(mem);
        fs.Close();
        return mem.ToArray();
    }

    public static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            builder.Append(invalid.Contains(c) ? '_' : c);
        }

        return builder.ToString();
    }

    public static async Task<string?> CacheRemoteImage(string cacheRoot, string url, string key)
    {
        try
        {
            var sanitized = SanitizeFileName(key);
            var etagFile = Path.Combine(cacheRoot, $"{sanitized}.etag");

            var (cachedEtag, cachedExt) = ReadEtagFile(etagFile);
            var existing =
                cachedExt != null ? Path.Combine(cacheRoot, $"{sanitized}.{cachedExt}") : null;

            if (existing != null && File.Exists(existing))
            {
                if (cachedEtag != null)
                {
                    var response = await url.WithHeader("If-None-Match", cachedEtag).GetAsync();

                    if (response.StatusCode == 304)
                        return existing;

                    return await SaveResponseAsync(
                        cacheRoot,
                        response,
                        sanitized,
                        etagFile,
                        existing
                    );
                }
                return existing;
            }

            var initResponse = await url.GetAsync();
            initResponse.ResponseMessage.EnsureSuccessStatusCode();
            return await SaveResponseAsync(cacheRoot, initResponse, sanitized, etagFile, null);
        }
        catch
        {
            return null;
        }
    }

    private static (string? etag, string? ext) ReadEtagFile(string etagFile)
    {
        if (!File.Exists(etagFile))
            return (null, null);
        var parts = File.ReadAllText(etagFile).Split('|');
        return (parts[0], parts.Length > 1 ? parts[1] : null);
    }

    private static async Task<string> SaveResponseAsync(
        string cacheRoot,
        IFlurlResponse response,
        string sanitized,
        string etagFile,
        string? oldOutput
    )
    {
        var bytes = await response.GetBytesAsync();

        var format =
            Img.DetectFormat(bytes) ?? throw new InvalidDataException("Unknown image format");
        var ext = format.FileExtensions.First();
        var output = Path.Combine(cacheRoot, $"{sanitized}.{ext}");

        if (oldOutput != null && oldOutput != output)
            File.Delete(oldOutput);

        // 保护避免并发时文件损坏
        var tmp = output + ".tmp";
        await File.WriteAllBytesAsync(tmp, bytes);
        File.Move(tmp, output, true);

        var newEtag = response.ResponseMessage.Headers.ETag?.Tag;
        await File.WriteAllTextAsync(etagFile, $"{newEtag}|{ext}");

        return output;
    }
}
