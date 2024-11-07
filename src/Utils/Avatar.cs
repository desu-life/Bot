using System.IO;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KanonBot;

public static partial class Utils
{
    public static async Task<Image<Rgba32>> LoadOrDownloadAvatar(API.OSU.Models.User userInfo)
    {
        var filename = $"{userInfo.Id}.png";
        if (userInfo.AvatarUrl.Host == "a.ppy.sb") {
            filename = $"sb-{userInfo.Id}.png";
        }
        var avatarPath = $"./work/avatar/{filename}";
        return await TryAsync(Utils.ReadImageRgba(avatarPath))
            .IfFail(async () =>
            {
                try
                {
                    avatarPath = await userInfo.AvatarUrl.DownloadFileAsync(
                        "./work/avatar/",
                        filename
                    );
                }
                catch (Exception ex)
                {
                    var msg = $"从API下载用户头像时发生了一处异常\n异常类型: {ex.GetType()}\n异常信息: '{ex.Message}'";
                    Log.Error(msg);
                    throw; // 下载失败直接抛出error
                }
                return await Utils.ReadImageRgba(avatarPath); // 下载后再读取
            });
    }
}
