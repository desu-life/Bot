#pragma warning disable CS8618 // 非null 字段未初始化
using System.IO;
using System.Numerics;
using KanonBot.Functions.OSU;
using KanonBot.Image;
using KanonBot.Image.Components;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Diagnostics;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static KanonBot.API.OSU.OSUExtensions;
using static KanonBot.Image.Fonts;
using Img = SixLabors.ImageSharp.Image;
using OSU = KanonBot.API.OSU;

namespace KanonBot.Image;

public static class InfoV1
{
    public class UserPanelData
    {
        public OSU.Models.UserExtended userInfo;
        public OSU.Models.User? prevUserInfo;
        public OSU.Models.PPlusData.UserPerformancesNext? pplusInfo;
        public required long osuId; // 官方服务器的id
        public int daysBefore = 0;
        public List<int> badgeId = [];
        public CustomMode customMode = CustomMode.Dark; //0=custom 1=light 2=dark
        public string ColorConfigRaw;
        public string? modeString;

        public enum CustomMode
        {
            Custom = 0,
            Light = 1,
            Dark = 2
        }
    }

    public static async Task<Img> DrawInfo(
        UserPanelData data,
        bool isBonded = false,
        bool isDataOfDayAvaiavle = true,
        bool eventmode = false
    )
    {
        var info = new Image<Rgba32>(1200, 857);
        // custom panel
        var panelPath = "./work/legacy/default-info-v1.png";
        if (File.Exists($"./work/legacy/v1_infopanel/{data.osuId}.png"))
            panelPath = $"./work/legacy/v1_infopanel/{data.osuId}.png";
        using var panel = await Img.LoadAsync(panelPath);
        // cover
        var coverPath = $"./work/legacy/v1_cover/custom/{data.osuId}.png";
        if (!File.Exists(coverPath))
        {
            coverPath = $"./work/legacy/v1_cover/osu!web/{data.osuId}.png";
            if (!File.Exists(coverPath))
            {
                coverPath = null;
                if (data.userInfo.Cover is not null)
                {
                    if (data.userInfo.Cover.CustomUrl is not null)
                    {
                        coverPath = await data.userInfo.Cover.CustomUrl.DownloadFileAsync(
                            "./work/legacy/v1_cover/osu!web/",
                            $"{data.osuId}.png"
                        );
                    }
                    else
                    {
                        var cover_id = data.userInfo.Cover.Id ?? "0";
                        coverPath = $"./work/legacy/v1_cover/osu!web/default_{cover_id}.png";
                        if (!File.Exists(coverPath))
                        {
                            coverPath = await data.userInfo.Cover.Url.DownloadFileAsync(
                                "./work/legacy/v1_cover/osu!web/",
                                $"default_{cover_id}.png"
                            );
                        }
                    }
                }
            }
        }

        if (coverPath is null)
        {
            int n = new Random().Next(1, 6);
            coverPath = $"./work/legacy/v1_cover/default/default_{n}.png";
        }

        using var cover = await Img.LoadAsync(coverPath);
        var resizeOptions = new ResizeOptions
        {
            Size = new Size(1200, 350),
            Sampler = KnownResamplers.Lanczos3,
            Compand = true,
            Mode = ResizeMode.Crop
        };
        cover.Mutate(x => x.Resize(resizeOptions));
        // cover.Mutate(x => x.RoundCorner(new Size(1200, 350), 20));
        info.Mutate(x => x.DrawImage(cover, 1));
        info.Mutate(x => x.DrawImage(panel, 1));

        //avatar
        using var avatar = await Utils.LoadOrDownloadAvatar(data.userInfo);
        avatar.Mutate(x => x.Resize(190, 190).RoundCorner(new Size(190, 190), 40));
        info.Mutate(x => x.DrawImage(avatar, new Point(39, 55), 1));

        // badge 取第一个绘制
        if (data.badgeId[0] != -1)
        {
            try
            {
                int dbcountl = 0;
                for (int i = 0; i < data.badgeId.Count; ++i)
                {
                    if (data.badgeId[i] > -1)
                    {
                        using var badge = await Img.LoadAsync<Rgba32>(
                            $"./work/badges/{data.badgeId[i]}.png"
                        );
                        // var roundedCorner = true;
                        // badge.ProcessPixelRows(row =>
                        // {
                        //     roundedCorner = row.GetRowSpan(0)[0] == Rgba32.ParseHex("#000000");
                        // });
                        // if (!roundedCorner)
                        //     badge.Mutate(x => x.RoundCorner(badge.Size, 20));
                        badge.Mutate(x => x.Resize(86, 40)); //.RoundCorner(new Size(86, 40), 5));
                        info.Mutate(x =>
                            x.DrawImage(badge, new Point(272 + (dbcountl * 100), 152), 1)
                        );
                        ++dbcountl;
                        if (dbcountl > 4)
                            break;
                    }
                }
            }
            catch { }
        }

        // obj

        using var flags = await Img.LoadAsync($"./work/flags/{data.userInfo.Country!.Code}.png");
        info.Mutate(x => x.DrawImage(flags, new Point(272, 212), 1));
        using var modeicon = await Img.LoadAsync(
            $"./work/legacy/mode_icon/{data.userInfo.Mode.ToStr()}.png"
        );
        modeicon.Mutate(x => x.Resize(64, 64));
        info.Mutate(x => x.DrawImage(modeicon, new Point(1125, 10), 1));

        // pp+
        if (data.userInfo.Mode is OSU.Mode.OSU && data.pplusInfo is not null)
        {
            using var ppdataPanel = await Img.LoadAsync("./work/legacy/pp+-v1.png");
            info.Mutate(x => x.DrawImage(ppdataPanel, new Point(0, 0), 1));
            Hexagram.HexagramInfo hi =
                new()
                {
                    abilityFillColor = Color.FromRgba(253, 148, 62, 128),
                    abilityLineColor = Color.ParseHex("#fd943e"),
                    nodeMaxValue = 10000,
                    nodeCount = 6,
                    size = 200,
                    sideLength = 200,
                    mode = 1,
                    strokeWidth = 2f,
                    nodesize = new SizeF(5f, 5f)
                };
            // acc ,flow, jump, pre, speed, sta
            var ppd = new double[6]; // 这里就强制转换了
            try
            {
                ppd[0] = data.pplusInfo.AccuracyTotal;
                ppd[1] = data.pplusInfo.FlowAimTotal;
                ppd[2] = data.pplusInfo.JumpAimTotal;
                ppd[3] = data.pplusInfo.PrecisionTotal;
                ppd[4] = data.pplusInfo.SpeedTotal;
                ppd[5] = data.pplusInfo.StaminaTotal;
            }
            catch
            {
                for (int i = 0; i < 6; i++)
                    ppd[i] = 0;
            }
            // x_offset  pp+数据的坐标偏移量
            var x_offset = new int[6] { 372, 330, 122, 52, 128, 317 }; // pp+数据的x轴坐标
            var multi = new double[6] { 14.1, 69.7, 1.92, 19.8, 0.588, 3.06 };
            var exp = new double[6] { 0.769, 0.596, 0.953, 0.8, 1.175, 0.993 };
            using var pppImg = Hexagram.Draw(ppd, multi, exp, hi);
            info.Mutate(x => x.DrawImage(pppImg, new Point(132, 626), 1));
            var f = Exo2Regular.Get(18);
            var pppto = new RichTextOptions(f)
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            var color = Color.ParseHex("#FFCC33");
            for (var i = 0; i < hi.nodeCount; i++)
            {
                pppto.Origin = new Vector2(x_offset[i], (i % 3 != 0) ? (i < 3 ? 640 : 829) : 734);
                info.Mutate(x =>
                    x.DrawText(pppto, $"({Math.Round(ppd[i])})", color)
                );
            }
        }
        else
        {
            using var ppdataPanel = await Img.LoadAsync("./work/legacy/nopp+info-v1.png");
            info.Mutate(x => x.DrawImage(ppdataPanel, new Point(0, 0), 1));
        }

        // time
        var textOptions = new RichTextOptions(Exo2Regular.Get(20))
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Origin = new PointF(15, 25),
            FallbackFontFamilies = [HarmonySans, HarmonySansArabic]
        };
        info.Mutate(x =>
            x.DrawText(textOptions, $"update: {DateTime.Now:yyyy/MM/dd HH:mm:ss}", Color.White)
        );
        if (data.daysBefore > 1)
        {
            textOptions = new RichTextOptions(HarmonySans.Get(20))
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            if (isDataOfDayAvaiavle)
            {
                textOptions.Origin = new PointF(300, 25);
                info.Mutate(x =>
                    x.DrawText(textOptions, $"对比自{data.daysBefore}天前", Color.White)
                );
            }
            else
            {
                textOptions.Origin = new PointF(300, 25);
                info.Mutate(x =>
                    x.DrawText(textOptions, $" 请求的日期没有数据.." + $"当前数据对比自{data.daysBefore}天前", Color.White)
                );
            }
        }
        // username
        textOptions.Font = Exo2SemiBold.Get(60);
        textOptions.Origin = new PointF(268, 140);
        info.Mutate(x =>
            x.DrawText(textOptions, data.userInfo.Username, Color.White)
        );

        var Statistics = data.userInfo.Statistics;
        var prevStatistics = data.prevUserInfo?.Statistics ?? data.userInfo.Statistics; // 没有就为当前数据

        // country_rank
        string countryRank;
        if (isBonded)
        {
            var diff = Statistics.CountryRank - prevStatistics!.CountryRank;
            if (diff > 0)
                countryRank = string.Format("#{0:N0}(-{1:N0})", Statistics.CountryRank, diff);
            else if (diff < 0)
                countryRank = string.Format(
                    "#{0:N0}(+{1:N0})",
                    Statistics.CountryRank,
                    Math.Abs(diff)
                );
            else
                countryRank = string.Format("#{0:N0}", Statistics.CountryRank);
        }
        else
        {
            countryRank = string.Format("#{0:N0}", Statistics.CountryRank);
        }
        textOptions.Font = Exo2SemiBold.Get(20);
        textOptions.Origin = new PointF(350, 260);
        info.Mutate(x =>
            x.DrawText(textOptions,  countryRank, Color.White)
        );
        // global_rank
        string diffStr;
        if (isBonded)
        {
            var diff = Statistics.GlobalRank - prevStatistics!.GlobalRank;
            if (diff > 0)
                diffStr = string.Format("↓ {0:N0}", diff);
            else if (diff < 0)
                diffStr = string.Format("↑ {0:N0}", Math.Abs(diff));
            else
                diffStr = "↑ -";
        }
        else
        {
            diffStr = "↑ -";
        }
        textOptions.Font = Exo2Regular.Get(40);
        textOptions.Origin = new PointF(40, 410);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:N0}", Statistics.GlobalRank), Color.White)
        );
        textOptions.Font = HarmonySans.Get(14);
        textOptions.Origin = new PointF(40, 430);
        info.Mutate(x =>
            x.DrawText(textOptions,  diffStr, Color.White)
        );
        // pp
        if (isBonded)
        {
            var diff = Statistics.PP - prevStatistics!.PP;
            if (diff >= 0.01)
                diffStr = string.Format("↑ {0:0.##}", diff);
            else if (diff <= -0.01)
                diffStr = string.Format("↓ {0:0.##}", Math.Abs(diff));
            else
                diffStr = "↑ -";
        }
        else
        {
            diffStr = "↑ -";
        }
        textOptions.Font = Exo2Regular.Get(40);
        textOptions.Origin = new PointF(246, 410);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:0.##}", Statistics.PP), Color.White)
        );
        textOptions.Font = HarmonySans.Get(14);
        textOptions.Origin = new PointF(246, 430);
        info.Mutate(x =>
            x.DrawText(textOptions,  diffStr, Color.White)
        );
        // ssh ss
        textOptions.Font = Exo2Regular.Get(30);
        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
        textOptions.Origin = new PointF(80, 540);
        info.Mutate(x =>
            x.DrawText(textOptions, Statistics.GradeCounts.SSH.ToString(), Color.White)
        );
        textOptions.Origin = new PointF(191, 540);
        info.Mutate(x =>
            x.DrawText(textOptions, Statistics.GradeCounts.SS.ToString(), Color.White)
        );
        textOptions.Origin = new PointF(301, 540);
        info.Mutate(x =>
            x.DrawText(textOptions, Statistics.GradeCounts.SH.ToString(), Color.White)
        );
        textOptions.Origin = new PointF(412, 540);
        info.Mutate(x =>
            x.DrawText(textOptions, Statistics.GradeCounts.S.ToString(), Color.White)
        );
        textOptions.Origin = new PointF(522, 540);
        info.Mutate(x =>
            x.DrawText(textOptions, Statistics.GradeCounts.A.ToString(), Color.White)
        );
        // level
        textOptions.Font = Exo2SemiBold.Get(34);
        textOptions.Origin = new PointF(1115, 385);
        info.Mutate(x =>
            x.DrawText(textOptions, Statistics.Level.Current.ToString(), Color.White)
        );
        // Level%
        var levelper = Statistics.Level.Progress;
        textOptions.HorizontalAlignment = HorizontalAlignment.Right;
        textOptions.Font = Exo2SemiBold.Get(20);
        textOptions.Origin = new PointF(1060, 400);
        info.Mutate(x =>
            x.DrawText(textOptions,  $"{levelper}%", Color.White)
        );
        try
        {
            using var levelRoundrect = new Image<Rgba32>(4 * levelper, 7);
            levelRoundrect.Mutate(x =>
                x.Fill(Color.ParseHex("#FF66AB")).RoundCorner(new Size(4 * levelper, 7), 5)
            );
            info.Mutate(x => x.DrawImage(levelRoundrect, new Point(662, 370), 1));
        }
        catch (ArgumentOutOfRangeException) { }
        // SCORES
        textOptions.Font = Exo2Regular.Get(36);
        string rankedScore;
        // if (isBonded){
        //     var diff = data.userInfo.rankedScore - data.prevUserInfo.rankedScore;
        //     if (diff > 0) rankedScore = string.Format("{0:N0}(+{1:N0})", data.userInfo.rankedScore, diff);
        //     else if (diff < 0) rankedScore = string.Format("{0:N0}({1:N0})", data.userInfo.rankedScore, diff);
        //     else rankedScore = string.Format("{0:N0}", data.userInfo.rankedScore);
        // } else {
        //     rankedScore = string.Format("{0:N0}", data.userInfo.rankedScore);
        // }
        rankedScore = string.Format("{0:N0}", Statistics.RankedScore);
        textOptions.Origin = new PointF(1180, 625);
        info.Mutate(x =>
            x.DrawText(textOptions,  rankedScore, Color.White)
        );
        string acc;
        if (isBonded)
        {
            var diff = Statistics.HitAccuracy - prevStatistics!.HitAccuracy;
            if (diff >= 0.01)
                acc = string.Format("{0:0.##}%(+{1:0.##}%)", Statistics.HitAccuracy, diff);
            else if (diff <= -0.01)
                acc = string.Format("{0:0.##}%({1:0.##}%)", Statistics.HitAccuracy, diff);
            else
                acc = string.Format("{0:0.##}%", Statistics.HitAccuracy);
        }
        else
        {
            acc = string.Format("{0:0.##}%", Statistics.HitAccuracy);
        }
        textOptions.Origin = new PointF(1180, 665);
        info.Mutate(x =>
            x.DrawText(textOptions,  acc, Color.White)
        );
        string playCount;
        if (isBonded)
        {
            var diff = Statistics.PlayCount - prevStatistics!.PlayCount;
            if (diff > 0)
                playCount = string.Format("{0:N0}(+{1:N0})", Statistics.PlayCount, diff);
            else if (diff < 0)
                playCount = string.Format("{0:N0}({1:N0})", Statistics.PlayCount, diff);
            else
                playCount = string.Format("{0:N0}", Statistics.PlayCount);
        }
        else
        {
            playCount = string.Format("{0:N0}", Statistics.PlayCount);
        }
        textOptions.Origin = new PointF(1180, 705);
        info.Mutate(x =>
            x.DrawText(textOptions,  playCount, Color.White)
        );
        string totalScore;
        // if (isBonded){
        //     var diff = data.userInfo.totalScore - data.prevUserInfo.totalScore;
        //     if (diff > 0) totalScore = string.Format("{0:N0}(+{1:N0})", data.userInfo.totalScore, diff);
        //     else if (diff < 0) totalScore = string.Format("{0:N0}({1:N0})", data.userInfo.totalScore, diff);
        //     else totalScore = string.Format("{0:N0}", data.userInfo.totalScore);
        // } else {
        //     totalScore = string.Format("{0:N0}", data.userInfo.totalScore);
        // }
        totalScore = string.Format("{0:N0}", Statistics.TotalScore);
        textOptions.Origin = new PointF(1180, 745);
        info.Mutate(x =>
            x.DrawText(textOptions,  totalScore, Color.White)
        );
        string totalHits;
        if (isBonded)
        {
            var diff = Statistics.TotalHits - prevStatistics!.TotalHits;
            if (diff > 0)
                totalHits = string.Format("{0:N0}(+{1:N0})", Statistics.TotalHits, diff);
            else if (diff < 0)
                totalHits = string.Format("{0:N0}({1:N0})", Statistics.TotalHits, diff);
            else
                totalHits = string.Format("{0:N0}", Statistics.TotalHits);
        }
        else
        {
            totalHits = string.Format("{0:N0}", Statistics.TotalHits);
        }
        textOptions.Origin = new PointF(1180, 785);
        info.Mutate(x =>
            x.DrawText(textOptions,  totalHits, Color.White)
        );
        textOptions.Origin = new PointF(1180, 825);
        info.Mutate(x =>
            x.DrawText(textOptions, Utils.Duration2StringWithoutSec(Statistics.PlayTime), Color.White)
        );
        info.Mutate(x => x.RoundCorner(new Size(1200, 857), 24));

        // 不知道为啥更新了imagesharp后对比度(亮度)变了
        info.Mutate(x => x.Brightness(0.998f));

        return info;
    }
}
