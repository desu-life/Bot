using System.IO;
using System.Security.Cryptography;

namespace KanonBot;

public static partial class Utils
{
    public static async Task<byte[]> LoadOrDownloadBeatmap(API.OSU.Models.Beatmap bm)
    {
        try
        {
            byte[]? f = null;
            // 检查有没有本地的谱面
            if (File.Exists($"./work/beatmap/{bm.BeatmapId}.osu"))
            {
                f = await File.ReadAllBytesAsync($"./work/beatmap/{bm.BeatmapId}.osu");
                if (bm.Checksum is not null)
                {
                    using (var md5 = MD5.Create())
                    {
                        var hash = md5.ComputeHash(f);
                        var hash_online = System.Convert.FromHexString(bm.Checksum);

                        if (!hash.SequenceEqual(hash_online))
                        {
                            // 删除本地的谱面
                            File.Delete($"./work/beatmap/{bm.BeatmapId}.osu");
                            f = null;
                        }
                    }
                }
            }

            if (f is null)
            {
                // 下载谱面
                await API.OSU.Client.DownloadBeatmapFile(bm.BeatmapId);
                f = await File.ReadAllBytesAsync($"./work/beatmap/{bm.BeatmapId}.osu");
            }

            // 读取铺面
            return f!;
        }
        catch
        {
            // 加载失败，删除重新抛异常
            File.Delete($"./work/beatmap/{bm.BeatmapId}.osu");
            throw;
        }
    }
}
