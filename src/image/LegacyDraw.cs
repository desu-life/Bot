#pragma warning disable CS8618 // 非null 字段未初始化
using System.IO;
using System.Numerics;
using KanonBot.Functions.OSU;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Diagnostics;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static KanonBot.API.OSU.OSUExtensions;
using Img = SixLabors.ImageSharp.Image;
using OSU = KanonBot.API.OSU;

namespace KanonBot.LegacyImage
{
    public static class Draw
    {
        public class UserPanelData
        {
            public OSU.Models.UserExtended userInfo;
            public OSU.Models.User? prevUserInfo;
            public OSU.Models.PPlusData.UserData? pplusInfo;
            public string? customPanel;
            public int daysBefore = 0;
            public List<int> badgeId = new();
            public CustomMode customMode = CustomMode.Dark; //0=custom 1=light 2=dark
            public string ColorConfigRaw;

            public enum CustomMode
            {
                Custom = 0,
                Light = 1,
                Dark = 2
            }
        }

        public class ScorePanelData
        {
            public OsuPerformance.PPInfo ppInfo;
            public OSU.Models.ScoreLazer scoreInfo;
            public RosuPP.Mode mode;
        }

        public class PPVSPanelData
        {
            public string u1Name;
            public string u2Name;
            public OSU.Models.PPlusData.UserData u1;
            public OSU.Models.PPlusData.UserData u2;
        }

        public static FontCollection fonts = new();
        public static FontFamily Exo2SemiBold = fonts.Add("./work/fonts/Exo2/Exo2-SemiBold.ttf");
        public static FontFamily Exo2Regular = fonts.Add("./work/fonts/Exo2/Exo2-Regular.ttf");
        public static FontFamily HarmonySans = fonts.Add(
            "./work/fonts/HarmonyOS_Sans_SC/HarmonyOS_Sans_SC_Regular.ttf"
        );
        public static FontFamily TorusRegular = fonts.Add("./work/fonts/Torus-Regular.ttf");
        public static FontFamily TorusSemiBold = fonts.Add("./work/fonts/Torus-SemiBold.ttf");
        public static FontFamily avenirLTStdMedium = fonts.Add(
            "./work/fonts/AvenirLTStd-Medium.ttf"
        );

        public static FontFamily Mizolet = fonts.Add("./work/fonts/mizolet.ttf");
        public static FontFamily MizoletBokutoh = fonts.Add("./work/fonts/mizolet-bokutoh.ttf");
        public static FontFamily FredokaRegular = fonts.Add("./work/fonts/fredoka/Fredoka-Regular.ttf");
        public static FontFamily FredokaBold = fonts.Add("./work/fonts/fredoka/Fredoka-Bold.ttf");

        public static async Task<Img> DrawMod(OSU.Models.Mod mod)
        {
            var modName = mod.Acronym.ToUpper();
            var modPath = $"./work/mods/{modName}.png";
            if (File.Exists(modPath))
            {
                var modPic = await Img.LoadAsync(modPath);
                modPic.Mutate(x => x.Resize(200, 0));
                return modPic;
            }
            else
            {
                var drawOptions = new DrawingOptions
                {
                    GraphicsOptions = new GraphicsOptions { Antialias = true }
                };
                var textOptions = new RichTextOptions(new Font(Mizolet, 40))
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                var modPic = await Img.LoadAsync($"./work/mods/Unknown.png");
                modPic.Mutate(x => x.Resize(200, 0));
                textOptions.Origin = new PointF(96, 34);
                modPic.Mutate(operation: x =>
                    x.DrawText(drawOptions, textOptions, modName, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(96, 33);
                modPic.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, modName, new SolidBrush(Color.White), null)
                );
                return modPic;
            }
        }

        public static async Task<Img> DrawDifficultyRing(RosuPP.Mode mode, double star)
        {
            var ringFile = mode switch
            {
                RosuPP.Mode.Osu => "std-expertplus.png",
                RosuPP.Mode.Taiko => "taiko-expertplus.png",
                RosuPP.Mode.Catch => "ctb-expertplus.png",
                RosuPP.Mode.Mania => "mania-expertplus.png",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

            using var color = new Image<Rgba32>(128, 128);
            color.Mutate(x => x.Fill(Utils.ForStarDifficultyScore(star)));

            using var cover = await Image<Rgba32>.LoadAsync($"./work/icons/ringcontent.png");
            cover.Mutate(x => x.Resize(128, 128));
            color.Mutate(x => x.DrawImage(cover, new Point(0, 0), 0.3f));
            cover.Mutate(x => x.Brightness(0.9f)); // adjust

            var ring = await Image<Rgba32>.LoadAsync($"./work/icons/{ringFile}");
            ring.Mutate(x => x.Resize(128, 128));
            ring.Mutate(x =>
                x.DrawImage(
                    color,
                    new Point(0, 0),
                    PixelColorBlendingMode.Lighten,
                    PixelAlphaCompositionMode.SrcAtop,
                    1f
                )
            );
            return ring;
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
            if (File.Exists($"./work/legacy/v1_infopanel/{data.userInfo.Id}.png"))
                panelPath = $"./work/legacy/v1_infopanel/{data.userInfo.Id}.png";
            using var panel = await Img.LoadAsync(panelPath);
            // cover
            var coverPath = $"./work/legacy/v1_cover/custom/{data.userInfo.Id}.png";
            if (!File.Exists(coverPath))
            {
                coverPath = $"./work/legacy/v1_cover/osu!web/{data.userInfo.Id}.png";
                if (!File.Exists(coverPath))
                {
                    coverPath = null;
                    if (data.userInfo.Cover is not null)
                    {
                        if (data.userInfo.Cover.CustomUrl is not null)
                        {
                            coverPath = await data.userInfo.Cover.CustomUrl.DownloadFileAsync(
                                "./work/legacy/v1_cover/osu!web/",
                                $"{data.userInfo.Id}.png"
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
            var avatarPath = $"./work/avatar/{data.userInfo.Id}.png";
            using var avatar = await TryAsync(Utils.ReadImageRgba(avatarPath))
                .IfFail(async () =>
                {
                    try
                    {
                        avatarPath = await data.userInfo.AvatarUrl.DownloadFileAsync(
                            "./work/avatar/",
                            $"{data.userInfo.Id}.png"
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
            var drawOptions = new DrawingOptions
            {
                GraphicsOptions = new GraphicsOptions { Antialias = true }
            };

            using var flags = await Img.LoadAsync(
                $"./work/flags/{data.userInfo.Country!.Code}.png"
            );
            info.Mutate(x => x.DrawImage(flags, new Point(272, 212), 1));
            using var modeicon = await Img.LoadAsync(
                $"./work/legacy/mode_icon/{data.userInfo.Mode.ToStr()}.png"
            );
            modeicon.Mutate(x => x.Resize(64, 64));
            info.Mutate(x => x.DrawImage(modeicon, new Point(1125, 10), 1));

            // pp+
            if (data.userInfo.Mode is OSU.Mode.OSU)
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
                var ppd = new int[6]; // 这里就强制转换了
                try
                {
                    ppd[0] = (int)data.pplusInfo!.AccuracyTotal;
                    ppd[1] = (int)data.pplusInfo.FlowAimTotal;
                    ppd[2] = (int)data.pplusInfo.JumpAimTotal;
                    ppd[3] = (int)data.pplusInfo.PrecisionTotal;
                    ppd[4] = (int)data.pplusInfo.SpeedTotal;
                    ppd[5] = (int)data.pplusInfo.StaminaTotal;
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
                var f = new Font(Exo2Regular, 18);
                var pppto = new RichTextOptions(f)
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                var color = Color.ParseHex("#FFCC33");
                for (var i = 0; i < hi.nodeCount; i++)
                {
                    pppto.Origin = new Vector2(
                        x_offset[i],
                        (i % 3 != 0) ? (i < 3 ? 642 : 831) : 736
                    );
                    info.Mutate(x =>
                        x.DrawText(drawOptions, pppto, $"({ppd[i]})", new SolidBrush(color), null)
                    );
                }
            }
            else
            {
                using var ppdataPanel = await Img.LoadAsync("./work/legacy/nopp+info-v1.png");
                info.Mutate(x => x.DrawImage(ppdataPanel, new Point(0, 0), 1));
            }

            // time
            var textOptions = new RichTextOptions(new Font(Exo2Regular, 20))
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
                Origin = new PointF(15, 25)
            };
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    $"update: {DateTime.Now:yyyy/MM/dd HH:mm:ss}",
                    new SolidBrush(Color.White),
                    null
                )
            );
            if (data.daysBefore > 1)
            {
                textOptions = new RichTextOptions(new Font(HarmonySans, 20))
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                if (isDataOfDayAvaiavle)
                {
                    textOptions.Origin = new PointF(300, 25);
                    info.Mutate(x =>
                        x.DrawText(
                            drawOptions,
                            textOptions,
                            $"对比自{data.daysBefore}天前",
                            new SolidBrush(Color.White),
                            null
                        )
                    );
                }
                else
                {
                    textOptions.Origin = new PointF(300, 25);
                    info.Mutate(x =>
                        x.DrawText(
                            drawOptions,
                            textOptions,
                            $" 请求的日期没有数据.." + $"当前数据对比自{data.daysBefore}天前",
                            new SolidBrush(Color.White),
                            null
                        )
                    );
                }
            }
            // username
            textOptions.Font = new Font(Exo2SemiBold, 60);
            textOptions.Origin = new PointF(268, 140);
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    data.userInfo.Username,
                    new SolidBrush(Color.White),
                    null
                )
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
            textOptions.Font = new Font(Exo2SemiBold, 20);
            textOptions.Origin = new PointF(350, 260);
            info.Mutate(x =>
                x.DrawText(drawOptions, textOptions, countryRank, new SolidBrush(Color.White), null)
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
            textOptions.Font = new Font(Exo2Regular, 40);
            textOptions.Origin = new PointF(40, 410);
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    string.Format("{0:N0}", Statistics.GlobalRank),
                    new SolidBrush(Color.White),
                    null
                )
            );
            textOptions.Font = new Font(HarmonySans, 14);
            textOptions.Origin = new PointF(40, 430);
            info.Mutate(x =>
                x.DrawText(drawOptions, textOptions, diffStr, new SolidBrush(Color.White), null)
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
            textOptions.Font = new Font(Exo2Regular, 40);
            textOptions.Origin = new PointF(246, 410);
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    string.Format("{0:0.##}", Statistics.PP),
                    new SolidBrush(Color.White),
                    null
                )
            );
            textOptions.Font = new Font(HarmonySans, 14);
            textOptions.Origin = new PointF(246, 430);
            info.Mutate(x =>
                x.DrawText(drawOptions, textOptions, diffStr, new SolidBrush(Color.White), null)
            );
            // ssh ss
            textOptions.Font = new Font(Exo2Regular, 30);
            textOptions.HorizontalAlignment = HorizontalAlignment.Center;
            textOptions.Origin = new PointF(80, 540);
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    Statistics.GradeCounts.SSH.ToString(),
                    new SolidBrush(Color.White),
                    null
                )
            );
            textOptions.Origin = new PointF(191, 540);
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    Statistics.GradeCounts.SS.ToString(),
                    new SolidBrush(Color.White),
                    null
                )
            );
            textOptions.Origin = new PointF(301, 540);
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    Statistics.GradeCounts.SH.ToString(),
                    new SolidBrush(Color.White),
                    null
                )
            );
            textOptions.Origin = new PointF(412, 540);
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    Statistics.GradeCounts.S.ToString(),
                    new SolidBrush(Color.White),
                    null
                )
            );
            textOptions.Origin = new PointF(522, 540);
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    Statistics.GradeCounts.A.ToString(),
                    new SolidBrush(Color.White),
                    null
                )
            );
            // level
            textOptions.Font = new Font(Exo2SemiBold, 34);
            textOptions.Origin = new PointF(1115, 385);
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    Statistics.Level.Current.ToString(),
                    new SolidBrush(Color.White),
                    null
                )
            );
            // Level%
            var levelper = Statistics.Level.Progress;
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.Font = new Font(Exo2SemiBold, 20);
            textOptions.Origin = new PointF(1060, 400);
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    $"{levelper}%",
                    new SolidBrush(Color.White),
                    null
                )
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
            textOptions.Font = new Font(Exo2Regular, 36);
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
                x.DrawText(drawOptions, textOptions, rankedScore, new SolidBrush(Color.White), null)
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
                x.DrawText(drawOptions, textOptions, acc, new SolidBrush(Color.White), null)
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
                x.DrawText(drawOptions, textOptions, playCount, new SolidBrush(Color.White), null)
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
                x.DrawText(drawOptions, textOptions, totalScore, new SolidBrush(Color.White), null)
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
                x.DrawText(drawOptions, textOptions, totalHits, new SolidBrush(Color.White), null)
            );
            textOptions.Origin = new PointF(1180, 825);
            info.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    Utils.Duration2String(Statistics.PlayTime),
                    new SolidBrush(Color.White),
                    null
                )
            );
            info.Mutate(x => x.RoundCorner(new Size(1200, 857), 24));

            // 不知道为啥更新了imagesharp后对比度(亮度)变了
            info.Mutate(x => x.Brightness(0.998f));

            return info;
        }

        public static async Task<Img> DrawScore(ScorePanelData data)
        {
            var ppInfo = data.ppInfo;
            var score = new Image<Rgba32>(1950, 1088);
            // 先下载必要文件
            var bgPath = $"./work/background/{data.scoreInfo.Beatmap!.BeatmapId}.png";
            if (!File.Exists(bgPath))
            {
                bgPath = null;
                try
                {
                    bgPath = await OSU.Client.SayoDownloadBeatmapBackgroundImg(
                        data.scoreInfo.Beatmap.BeatmapsetId,
                        data.scoreInfo.Beatmap.BeatmapId,
                        "./work/background/"
                    );
                }
                catch (Exception ex)
                {
                    var msg =
                        $"从Sayo API下载背景图片时发生了一处异常\n异常类型: {ex.GetType()}\n异常信息: '{ex.Message}'";
                    Log.Warning(msg);
                }

                if (bgPath is null)
                {
                    try
                    {
                        bgPath = await OSU.Client.DownloadBeatmapBackgroundImg(
                            data.scoreInfo.Beatmap.BeatmapsetId,
                            "./work/background/",
                            $"{data.scoreInfo.Beatmap!.BeatmapId}.png"
                        );
                    }
                    catch (Exception ex)
                    {
                        var msg =
                            $"从OSU API下载背景图片时发生了一处异常\n异常类型: {ex.GetType()}\n异常信息: '{ex.Message}'";
                        Log.Warning(msg);
                    }
                }
            }

            var avatarPath = $"./work/avatar/{data.scoreInfo.UserId}.png";
            using var avatar = await TryAsync(Utils.ReadImageRgba(avatarPath))
                .IfFail(async () =>
                {
                    try
                    {
                        avatarPath = await data.scoreInfo.User!.AvatarUrl.DownloadFileAsync(
                            "./work/avatar/",
                            $"{data.scoreInfo.UserId}.png"
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

            using var panel = data.mode switch
            {
                RosuPP.Mode.Catch
                    => await Img.LoadAsync("work/legacy/v2_scorepanel/default-score-v2-fruits.png"),
                RosuPP.Mode.Mania
                    => await Img.LoadAsync("work/legacy/v2_scorepanel/default-score-v2-mania.png"),
                _ => await Img.LoadAsync("work/legacy/v2_scorepanel/default-score-v2.png")
            };

            // bg
            Image<Rgba32> bg;
            if (bgPath is null)
            {
                bg = await Img.LoadAsync<Rgba32>("./work/legacy/load-failed-img.png");
            }
            else
            {
                try
                {
                    bg = await Img.LoadAsync<Rgba32>(bgPath);
                }
                catch
                {
                    bg = await Img.LoadAsync<Rgba32>("./work/legacy/load-failed-img.png");
                    try
                    {
                        File.Delete(bgPath);
                    }
                    catch { }
                }
            }
            using var smallBg = bg.Clone(x => x.RoundCorner(new Size(433, 296), 20));
            using var backBlack = new Image<Rgba32>(1950 - 2, 1088);
            backBlack.Mutate(x =>
                x.BackgroundColor(Color.Black).RoundCorner(new Size(1950 - 2, 1088), 20)
            );
            bg.Mutate(x => x.GaussianBlur(5).RoundCorner(new Size(1950 - 2, 1088), 20));
            score.Mutate(x => x.DrawImage(bg, 1));
            score.Mutate(x => x.DrawImage(backBlack, 0.33f));

            if (data.scoreInfo.IsLazer)
            {
                var blurpanel = panel.Clone(x => x.GaussianBlur(3));
                score.Mutate(x => x.DrawImage(blurpanel, 0.3f));
            }

            score.Mutate(x => x.DrawImage(panel, 1));
            score.Mutate(x => x.DrawImage(smallBg, new Point(27, 34), 1));

            score.Mutate(x =>
                x.DrawText(
                    new DrawingOptions
                    {
                        GraphicsOptions = new GraphicsOptions { Antialias = true }
                    },
                    new RichTextOptions(new Font(FredokaBold, 60, FontStyle.Bold))
                    {
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Origin = new PointF(1915, 1065)
                    },
                    data.scoreInfo.IsLazer ? "Lazer" : "Classic",
                    new SolidBrush(Color.Transparent),
                    new SolidPen(Color.FromRgba(0x5f, 0x5f, 0x5f, 0xff), 3)
                )
            );

            bg.Dispose();

            // var lazer_triangle = await Img.LoadAsync<Rgba32>("./work/triangles.png");
            // lazer_triangle.Mutate(x => x.Resize(200, 0));
            // score.Mutate(x => x.DrawImage(lazer_triangle, new Point(870, 810), 0.5f));

            // StarRing
            // diff circle
            // green, blue, yellow, red, purple, black
            // [0,2), [2,3), [3,4), [4,5), [5,7), [7,?)
            // var ringFile = new string[6];
            // switch (data.mode)
            // {
            //     case RosuPP.Mode.Osu:
            //         ringFile[0] = "std-easy.png";
            //         ringFile[1] = "std-normal.png";
            //         ringFile[2] = "std-hard.png";
            //         ringFile[3] = "std-insane.png";
            //         ringFile[4] = "std-expert.png";
            //         ringFile[5] = "std-expertplus.png";
            //         break;
            //     case RosuPP.Mode.Catch:
            //         ringFile[0] = "ctb-easy.png";
            //         ringFile[1] = "ctb-normal.png";
            //         ringFile[2] = "ctb-hard.png";
            //         ringFile[3] = "ctb-insane.png";
            //         ringFile[4] = "ctb-expert.png";
            //         ringFile[5] = "ctb-expertplus.png";
            //         break;
            //     case RosuPP.Mode.Taiko:
            //         ringFile[0] = "taiko-easy.png";
            //         ringFile[1] = "taiko-normal.png";
            //         ringFile[2] = "taiko-hard.png";
            //         ringFile[3] = "taiko-insane.png";
            //         ringFile[4] = "taiko-expert.png";
            //         ringFile[5] = "taiko-expertplus.png";
            //         break;
            //     case RosuPP.Mode.Mania:
            //         ringFile[0] = "mania-easy.png";
            //         ringFile[1] = "mania-normal.png";
            //         ringFile[2] = "mania-hard.png";
            //         ringFile[3] = "mania-insane.png";
            //         ringFile[4] = "mania-expert.png";
            //         ringFile[5] = "mania-expertplus.png";
            //         break;
            // }
            // string temp;
            // var star = ppInfo.star;
            // if (star < 2)
            // {
            //     temp = ringFile[0];
            // }
            // else if (star < 2.7)
            // {
            //     temp = ringFile[1];
            // }
            // else if (star < 4)
            // {
            //     temp = ringFile[2];
            // }
            // else if (star < 5.3)
            // {
            //     temp = ringFile[3];
            // }
            // else if (star < 6.5)
            // {
            //     temp = ringFile[4];
            // }
            // else
            // {
            //     temp = ringFile[5];
            // }
            // using var diffCircle = await Img.LoadAsync("./work/icons/" + temp);
            // diffCircle.Mutate(x => x.Resize(65, 65));
            using var diffCircle = await DrawDifficultyRing(data.mode, ppInfo.star);
            diffCircle.Mutate(x => x.Resize(65, 65));
            score.Mutate(x => x.DrawImage(diffCircle, new Point(512, 257), 1));
            // beatmap_status
            if (data.scoreInfo.Beatmap.Status is OSU.Models.Status.ranked)
            {
                using var c = await Img.LoadAsync("./work/icons/ranked.png");
                score.Mutate(x => x.DrawImage(c, new Point(415, 16), 1));
            }
            if (data.scoreInfo.Beatmap.Status is OSU.Models.Status.approved)
            {
                using var c = await Img.LoadAsync("./work/icons/approved.png");
                score.Mutate(x => x.DrawImage(c, new Point(415, 16), 1));
            }
            if (data.scoreInfo.Beatmap.Status is OSU.Models.Status.loved)
            {
                using var c = await Img.LoadAsync("./work/icons/loved.png");
                score.Mutate(x => x.DrawImage(c, new Point(415, 16), 1));
            }
            // mods
            var me = data.scoreInfo.Mods.AsEnumerable();
            // mods.Sort((a, b) => a.IsSpeedChangeMod ? 1 : -1);

            // 筛选classic成绩
            if (data.scoreInfo.IsClassic)
            {
                me = me.Filter(x => !x.IsClassic);
            }

            var mods = me.ToList();
            var modp = 0;
            if (mods.Count > 7)
            {
                foreach (var mod in mods)
                {
                    var modPic = await DrawMod(mod);
                    if (modPic is null)
                        continue;
                    modPic.Mutate(x => x.Resize(200, 0));
                    score.Mutate(x => x.DrawImage(modPic, new Point(modp + 440, 440), 1));
                    modp += 120;
                }
            }
            else
            {
                foreach (var mod in mods)
                {
                    var modPic = await DrawMod(mod);
                    if (modPic is null)
                        continue;
                    modPic.Mutate(x => x.Resize(200, 0));
                    score.Mutate(x => x.DrawImage(modPic, new Point(modp + 440, 440), 1));
                    modp += 160;
                }
            }
            // rankings
            var ranking = data.scoreInfo.Passed ? data.scoreInfo.RankAuto : "F";
            using var rankPic = await Img.LoadAsync($"./work/ranking/ranking-{ranking}.png");

            if (data.scoreInfo.IsLazer)
            {
                var blurrank = rankPic.Clone(x =>
                    x.Resize(
                            new ResizeOptions
                            {
                                Size = new Size(300, 300),
                                Mode = ResizeMode.BoxPad
                            }
                        )
                        .GaussianBlur(40)
                );
                score.Mutate(x =>
                    x.DrawImage(blurrank, new Point(913 - 150 + 62, 874 - 150 + 31), 0.8f)
                );
            }

            score.Mutate(x => x.DrawImage(rankPic, new Point(913, 874), 1));
            // text part (文字部分)
            var font = new Font(TorusRegular, 60);
            var drawOptions = new DrawingOptions
            {
                GraphicsOptions = new GraphicsOptions { Antialias = true }
            };
            var textOptions = new RichTextOptions(new Font(font, 60))
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            // beatmap_info
            var title = "";
            foreach (char c in data.scoreInfo.Beatmapset!.Title)
            {
                title += c;
                var m = TextMeasurer.MeasureSize(title, textOptions);
                if (m.Width > 725)
                {
                    title += "...";
                    break;
                }
            }
            textOptions.Origin = new PointF(499, 110);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, title, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(499, 105);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, title, new SolidBrush(Color.White), null)
            );
            // artist
            textOptions.Font = new Font(TorusRegular, 40);
            var artist = "";
            foreach (char c in data.scoreInfo.Beatmapset.Artist)
            {
                artist += c;
                var m = TextMeasurer.MeasureSize(artist, textOptions);
                if (m.Width > 205)
                {
                    artist += "...";
                    break;
                }
            }
            textOptions.Origin = new PointF(519, 178);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, artist, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(519, 175);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, artist, new SolidBrush(Color.White), null)
            );
            // creator
            var creator = "";
            foreach (char c in data.scoreInfo.Beatmapset.Creator)
            {
                creator += c;
                var m = TextMeasurer.MeasureSize(creator, textOptions);
                if (m.Width > 145)
                {
                    creator += "...";
                    break;
                }
            }
            textOptions.Origin = new PointF(795, 178);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, creator, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(795, 175);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, creator, new SolidBrush(Color.White), null)
            );
            // beatmap_id
            var beatmap_id = data.scoreInfo.Beatmap.BeatmapId.ToString();
            textOptions.Origin = new PointF(1008, 178);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, beatmap_id, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1008, 175);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, beatmap_id, new SolidBrush(Color.White), null)
            );
            // ar,od info
            var color = Color.ParseHex("#f1ce59");
            textOptions.Font = new Font(TorusRegular, 24.25f);
            // time
            var song_time = Utils.Duration2TimeString(
                (long)Math.Round((data.scoreInfo.Beatmap.TotalLength - 1.0) / data.ppInfo.clockrate)
            );
            textOptions.Origin = new PointF(1741, 127);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, song_time, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1741, 124);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, song_time, new SolidBrush(color), null)
            );
            // bpm
            var bpm = data.ppInfo.bpm.ToString("0.##");
            textOptions.Origin = new PointF(1457, 127);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, bpm, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1457, 124);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, bpm, new SolidBrush(color), null)
            );
            // ar
            var ar = ppInfo.AR.ToString("0.0#");
            textOptions.Origin = new PointF(1457, 218);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, ar, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1457, 215);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, ar, new SolidBrush(color), null)
            );
            // od
            var od = ppInfo.OD.ToString("0.0#");
            textOptions.Origin = new PointF(1741, 218);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, od, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1741, 215);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, od, new SolidBrush(color), null)
            );
            // cs
            var cs = ppInfo.CS.ToString("0.0#");
            textOptions.Origin = new PointF(1457, 312);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, cs, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1457, 309);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, cs, new SolidBrush(color), null)
            );
            // hp
            var hp = ppInfo.HP.ToString("0.0#");
            textOptions.Origin = new PointF(1741, 312);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, hp, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1741, 309);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, hp, new SolidBrush(color), null)
            );
            // stars, version
            var starText = $"Stars: {ppInfo.star:0.##}";
            textOptions.Origin = new PointF(584, 292);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, starText, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(584, 289);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, starText, new SolidBrush(color), null)
            );
            var version = "";
            foreach (char c in data.scoreInfo.Beatmap.Version)
            {
                version += c;
                var m = TextMeasurer.MeasureSize(version, textOptions);
                if (m.Width > 140)
                {
                    version += "...";
                    break;
                }
            }
            textOptions.Origin = new PointF(584, 320);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, version, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(584, 317);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, version, new SolidBrush(Color.White), null)
            );
            // avatar
            avatar.Mutate(x => x.Resize(80, 80).RoundCorner(new Size(80, 80), 40));
            score.Mutate(x => x.Fill(Color.White, new EllipsePolygon(80, 465, 85, 85)));
            score.Mutate(x => x.DrawImage(avatar, new Point(40, 425), 1));
            // username
            textOptions.Font = new Font(TorusSemiBold, 36);
            var username = data.scoreInfo.User!.Username;
            textOptions.Origin = new PointF(145, 470);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, username, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(145, 467);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, username, new SolidBrush(Color.White), null)
            );
            // time
            textOptions.Font = new Font(TorusRegular, 27.61f);
            data.scoreInfo.EndedAt = data.scoreInfo.EndedAt.ToLocalTime(); //to UTC+8
            var time = data.scoreInfo.EndedAt.ToString("yyyy/MM/dd HH:mm:ss");
            textOptions.Origin = new PointF(145, 505);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, time, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(145, 502);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, time, new SolidBrush(Color.White), null)
            );

            // pp
            var ppTColor = Color.ParseHex("#cf93ae");
            var ppColor = Color.ParseHex("#fc65a9");
            textOptions.Font = new Font(TorusRegular, 33.5f);
            // aim, speed
            string pptext;
            if (ppInfo.ppStat.aim == null)
                pptext = "-";
            else
                pptext = ppInfo.ppStat.aim.Value.ToString("0");
            var metric = TextMeasurer.MeasureSize(pptext, textOptions);
            textOptions.Origin = new PointF(1532, 638);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
            );
            textOptions.Origin = new PointF(1532 + metric.Width, 638);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
            );
            if (ppInfo.ppStat.speed == null)
                pptext = "-";
            else
                pptext = ppInfo.ppStat.speed.Value.ToString("0");
            metric = TextMeasurer.MeasureSize(pptext, textOptions);
            textOptions.Origin = new PointF(1672, 638);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
            );
            textOptions.Origin = new PointF(1672 + metric.Width, 638);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
            );
            if (ppInfo.ppStat.acc == null)
                pptext = "-";
            else
                pptext = ppInfo.ppStat.acc.Value.ToString("0");
            metric = TextMeasurer.MeasureSize(pptext, textOptions);
            textOptions.Origin = new PointF(1812, 638);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
            );
            textOptions.Origin = new PointF(1812 + metric.Width, 638);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
            );

            // if (data.scoreInfo.Mode is OSU.Mode.Mania)
            // {
            //     pptext = "-";
            //     metric = TextMeasurer.MeasureSize(pptext, textOptions);
            //     for (var i = 0; i < 5; i++)
            //     {
            //         textOptions.Origin = new PointF(50 + 139 * i, 638);
            //         score.Mutate(x => x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null));
            //         textOptions.Origin = new PointF(50 + 139 * i + metric.Width, 638);
            //         score.Mutate(x => x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null));
            //     }
            // }
            // else
            // {
            // }
            // 这边不再需要匹配mania模式
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    pptext = ppInfo.ppStats![5 - (i + 1)].total.ToString("0");
                }
                catch
                {
                    pptext = "-";
                }
                metric = TextMeasurer.MeasureSize(pptext, textOptions);
                textOptions.Origin = new PointF(50 + 139 * i, 638);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
                );
                textOptions.Origin = new PointF(50 + 139 * i + metric.Width, 638);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
                );
            }

            // if fc
            textOptions.Font = new Font(TorusRegular, 24.5f);
            try
            {
                pptext = ppInfo.ppStats![5].total.ToString("0");
            }
            catch
            {
                pptext = "-";
            }
            metric = TextMeasurer.MeasureSize(pptext, textOptions);
            textOptions.Origin = new PointF(99, 562);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
            );
            textOptions.Origin = new PointF(99 + metric.Width, 562);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
            );

            // total pp
            textOptions.Font = new Font(TorusRegular, 61f);
            pptext = Math.Round(ppInfo.ppStat.total).ToString("0");
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.Origin = new PointF(1825, 500);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
            );
            textOptions.Origin = new PointF(1899, 500);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
            );

            // score
            textOptions.HorizontalAlignment = HorizontalAlignment.Center;
            textOptions.Font = new Font(TorusRegular, 40);
            textOptions.Origin = new PointF(980, 745);
            score.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    data.scoreInfo.ScoreAuto.ToString("N0"),
                    new SolidBrush(Color.White),
                    null
                )
            );

            if (data.mode is RosuPP.Mode.Catch)
            {
                textOptions.Font = new Font(TorusRegular, 40.00f);
                var statistics = data.scoreInfo.ConvertStatistics;
                var great = statistics.CountGreat.ToString();
                var ok = statistics.CountOk.ToString();
                var meh = statistics.CountMeh.ToString();
                var miss = statistics.CountMiss.ToString();

                // great
                textOptions.Origin = new PointF(790, 852);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(790, 849);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.White), null)
                );
                // ok
                textOptions.Origin = new PointF(790, 975);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(790, 972);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.White), null)
                );
                // meh
                textOptions.Origin = new PointF(1152, 852);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(1152, 849);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.White), null)
                );
                // miss
                textOptions.Origin = new PointF(1152, 975);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(1152, 972);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.White), null)
                );
            }
            else if (data.mode is RosuPP.Mode.Mania)
            {
                textOptions.Font = new Font(TorusRegular, 35.00f);
                var great = data.scoreInfo.Statistics.CountGreat.ToString();
                var ok = data.scoreInfo.Statistics.CountOk.ToString();
                var meh = data.scoreInfo.Statistics.CountMeh.ToString();
                var miss = data.scoreInfo.Statistics.CountMiss.ToString();
                var geki = data.scoreInfo.Statistics.CountGeki.ToString();
                var katu = data.scoreInfo.Statistics.CountKatu.ToString();

                // great
                textOptions.Origin = new PointF(790, 834);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(790, 832);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.White), null)
                );
                // geki
                textOptions.Origin = new PointF(1156, 836);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, geki, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(1156, 834);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, geki, new SolidBrush(Color.White), null)
                );
                // katu
                textOptions.Origin = new PointF(790, 909);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, katu, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(790, 907);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, katu, new SolidBrush(Color.White), null)
                );
                // ok
                textOptions.Origin = new PointF(1156, 909);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(1156, 907);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.White), null)
                );
                // meh
                textOptions.Origin = new PointF(790, 984);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(790, 982);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.White), null)
                );
                // miss
                textOptions.Origin = new PointF(1156, 984);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(1156, 982);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.White), null)
                );
            }
            else
            {
                textOptions.Font = new Font(TorusRegular, 53.09f);
                var great = data.scoreInfo.Statistics.CountGreat.ToString();
                var ok = data.scoreInfo.Statistics.CountOk.ToString();
                var meh = data.scoreInfo.Statistics.CountMeh.ToString();
                var miss = data.scoreInfo.Statistics.CountMiss.ToString();

                // great
                textOptions.Origin = new PointF(795, 857);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(795, 854);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.White), null)
                );
                // ok
                textOptions.Origin = new PointF(795, 985);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(795, 982);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.White), null)
                );
                // meh
                textOptions.Origin = new PointF(1154, 857);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(1154, 854);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.White), null)
                );
                // miss
                textOptions.Origin = new PointF(1154, 985);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(1154, 982);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.White), null)
                );
            }

            // acc
            textOptions.Font = new Font(TorusRegular, 53.56f);
            var acc = data.scoreInfo.AccAuto * 100f;
            var hsl = new Hsl(150, 1, 1);
            // ("#ffbd1f") idk?
            color = Color.ParseHex("#87ff6a");
            textOptions.Origin = new PointF(360, 966);
            score.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    $"{acc:0.0#}%",
                    new SolidBrush(Color.Black),
                    null
                )
            );
            using var acchue = new Image<Rgba32>(1950 - 2, 1088);
            var hue = acc < 60 ? 260f : (acc - 60) * 2 + 280f;
            textOptions.Origin = new PointF(360, 963);
            acchue.Mutate(x =>
                x.DrawText(drawOptions, textOptions, $"{acc:0.0#}%", new SolidBrush(color), null)
            );
            acchue.Mutate(x => x.Hue(((float)hue)));
            score.Mutate(x => x.DrawImage(acchue, 1));
            // combo
            var combo = data.scoreInfo.MaxCombo;
            if (ppInfo.maxCombo != null)
            {
                var maxCombo = ppInfo.maxCombo.Value;
                if (maxCombo > 0)
                {
                    textOptions.Origin = new PointF(1598, 966);
                    score.Mutate(x =>
                        x.DrawText(
                            drawOptions,
                            textOptions,
                            " / ",
                            new SolidBrush(Color.Black),
                            null
                        )
                    );
                    textOptions.Origin = new PointF(1598, 963);
                    score.Mutate(x =>
                        x.DrawText(
                            drawOptions,
                            textOptions,
                            " / ",
                            new SolidBrush(Color.White),
                            null
                        )
                    );
                    textOptions.HorizontalAlignment = HorizontalAlignment.Left;
                    textOptions.Origin = new PointF(1607, 966);
                    score.Mutate(x =>
                        x.DrawText(
                            drawOptions,
                            textOptions,
                            $"{maxCombo}x",
                            new SolidBrush(Color.Black),
                            null
                        )
                    );
                    textOptions.Origin = new PointF(1607, 963);
                    score.Mutate(x =>
                        x.DrawText(
                            drawOptions,
                            textOptions,
                            $"{maxCombo}x",
                            new SolidBrush(color),
                            null
                        )
                    );
                    textOptions.HorizontalAlignment = HorizontalAlignment.Right;
                    textOptions.Origin = new PointF(1588, 966);
                    score.Mutate(x =>
                        x.DrawText(
                            drawOptions,
                            textOptions,
                            $"{combo}x",
                            new SolidBrush(Color.Black),
                            null
                        )
                    );
                    using var combohue = new Image<Rgba32>(1950 - 2, 1088);
                    hue = (((float)combo / (float)maxCombo) * 100) + 260;
                    textOptions.Origin = new PointF(1588, 963);
                    combohue.Mutate(x =>
                        x.DrawText(
                            drawOptions,
                            textOptions,
                            $"{combo}x",
                            new SolidBrush(color),
                            null
                        )
                    );
                    combohue.Mutate(x => x.Hue(((float)hue)));
                    score.Mutate(x => x.DrawImage(combohue, 1));
                }
                else
                {
                    textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                    textOptions.Origin = new PointF(1598, 966);
                    score.Mutate(x =>
                        x.DrawText(
                            drawOptions,
                            textOptions,
                            $"{combo}x",
                            new SolidBrush(Color.Black),
                            null
                        )
                    );
                    textOptions.Origin = new PointF(1598, 963);
                    score.Mutate(x =>
                        x.DrawText(
                            drawOptions,
                            textOptions,
                            $"{combo}x",
                            new SolidBrush(color),
                            null
                        )
                    );
                }
            }
            else
            {
                textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                textOptions.Origin = new PointF(1598, 966);
                score.Mutate(x =>
                    x.DrawText(
                        drawOptions,
                        textOptions,
                        $"{combo}x",
                        new SolidBrush(Color.Black),
                        null
                    )
                );
                textOptions.Origin = new PointF(1598, 963);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, $"{combo}x", new SolidBrush(color), null)
                );
            }

            // 不知道为啥更新了imagesharp后对比度(亮度)变了
            score.Mutate(x => x.Brightness(0.998f));

            return score;
        }

        public static async Task<Img> DrawPPVS(PPVSPanelData data)
        {
            var ppvsImg = await Img.LoadAsync("work/legacy/ppvs.png");
            Hexagram.HexagramInfo hi =
                new()
                {
                    nodeCount = 6,
                    nodeMaxValue = 12000,
                    size = 1134,
                    sideLength = 791,
                    mode = 2,
                    strokeWidth = 6f,
                    nodesize = new SizeF(15f, 15f)
                };
            // hi.abilityLineColor = Color.ParseHex("#FF7BAC");
            var multi = new double[6] { 14.1, 69.7, 1.92, 19.8, 0.588, 3.06 };
            var exp = new double[6] { 0.769, 0.596, 0.953, 0.8, 1.175, 0.993 };
            var u1d = new int[6];
            u1d[0] = (int)data.u1.AccuracyTotal;
            u1d[1] = (int)data.u1.FlowAimTotal;
            u1d[2] = (int)data.u1.JumpAimTotal;
            u1d[3] = (int)data.u1.PrecisionTotal;
            u1d[4] = (int)data.u1.SpeedTotal;
            u1d[5] = (int)data.u1.StaminaTotal;
            var u2d = new int[6];
            u2d[0] = (int)data.u2.AccuracyTotal;
            u2d[1] = (int)data.u2.FlowAimTotal;
            u2d[2] = (int)data.u2.JumpAimTotal;
            u2d[3] = (int)data.u2.PrecisionTotal;
            u2d[4] = (int)data.u2.SpeedTotal;
            u2d[5] = (int)data.u2.StaminaTotal;
            // acc ,flow, jump, pre, speed, sta

            if (data.u1.PerformanceTotal < data.u2.PerformanceTotal)
            {
                hi.abilityFillColor = Color.FromRgba(255, 123, 172, 50);
                hi.abilityLineColor = Color.FromRgba(255, 123, 172, 255);
                using var tmp1 = Hexagram.Draw(u1d, multi, exp, hi);
                ppvsImg.Mutate(x => x.DrawImage(tmp1, new Point(0, -120), 1));
                hi.abilityFillColor = Color.FromRgba(41, 171, 226, 50);
                hi.abilityLineColor = Color.FromRgba(41, 171, 226, 255);
                using var tmp2 = Hexagram.Draw(u2d, multi, exp, hi);
                ppvsImg.Mutate(x => x.DrawImage(tmp2, new Point(0, -120), 1));
            }
            else
            {
                hi.abilityFillColor = Color.FromRgba(41, 171, 226, 50);
                hi.abilityLineColor = Color.FromRgba(41, 171, 226, 255);
                using var tmp1 = Hexagram.Draw(u2d, multi, exp, hi);
                ppvsImg.Mutate(x => x.DrawImage(tmp1, new Point(0, -120), 1));
                hi.abilityFillColor = Color.FromRgba(255, 123, 172, 50);
                hi.abilityLineColor = Color.FromRgba(255, 123, 172, 255);
                using var tmp2 = Hexagram.Draw(u1d, multi, exp, hi);
                ppvsImg.Mutate(x => x.DrawImage(tmp2, new Point(0, -120), 1));
            }

            // text
            var drawOptions = new DrawingOptions
            {
                GraphicsOptions = new GraphicsOptions { Antialias = true }
            };

            // 打印用户名
            var font = new Font(avenirLTStdMedium, 36);
            var textOptions = new RichTextOptions(font)
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
                Origin = new PointF(808, 888)
            };
            var color = Color.ParseHex("#999999");
            ppvsImg.Mutate(x =>
                x.DrawText(drawOptions, textOptions, data.u1Name, new SolidBrush(color), null)
            );
            textOptions.Origin = new PointF(264, 888);
            ppvsImg.Mutate(x =>
                x.DrawText(drawOptions, textOptions, data.u2Name, new SolidBrush(color), null)
            );

            // 打印每个用户数据
            var y_offset = new int[6] { 1485, 1150, 1066, 1234, 1318, 1403 }; // pp+数据的y轴坐标
            font = new Font(avenirLTStdMedium, 32);
            textOptions = new RichTextOptions(font)
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            for (var i = 0; i < u1d.Length; i++)
            {
                textOptions.Origin = new PointF(664, y_offset[i]);
                ppvsImg.Mutate(x =>
                    x.DrawText(
                        drawOptions,
                        textOptions,
                        u1d[i].ToString(),
                        new SolidBrush(color),
                        null
                    )
                );
            }
            textOptions.Origin = new PointF(664, 980);
            ppvsImg.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    data.u1.PerformanceTotal.ToString("0.##"),
                    new SolidBrush(color),
                    null
                )
            );
            for (var i = 0; i < u2d.Length; i++)
            {
                textOptions.Origin = new PointF(424, y_offset[i]);
                ppvsImg.Mutate(x =>
                    x.DrawText(
                        drawOptions,
                        textOptions,
                        u2d[i].ToString(),
                        new SolidBrush(color),
                        null
                    )
                );
            }
            textOptions.Origin = new PointF(424, 980);
            ppvsImg.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    data.u2.PerformanceTotal.ToString("0.##"),
                    new SolidBrush(color),
                    null
                )
            );

            // 打印数据差异
            var diffPoint = 960;
            color = Color.ParseHex("#ffcd22");
            textOptions.Origin = new PointF(diffPoint, 980);
            ppvsImg.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    string.Format("{0:0}", (data.u2.PerformanceTotal - data.u1.PerformanceTotal)),
                    new SolidBrush(color),
                    null
                )
            );
            textOptions.Origin = new PointF(diffPoint, 1066);
            ppvsImg.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    (u2d[2] - u1d[2]).ToString(),
                    new SolidBrush(color),
                    null
                )
            );
            textOptions.Origin = new PointF(diffPoint, 1150);
            ppvsImg.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    (u2d[1] - u1d[1]).ToString(),
                    new SolidBrush(color),
                    null
                )
            );
            textOptions.Origin = new PointF(diffPoint, 1234);
            ppvsImg.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    (u2d[3] - u1d[3]).ToString(),
                    new SolidBrush(color),
                    null
                )
            );
            textOptions.Origin = new PointF(diffPoint, 1318);
            ppvsImg.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    (u2d[4] - u1d[4]).ToString(),
                    new SolidBrush(color),
                    null
                )
            );
            textOptions.Origin = new PointF(diffPoint, 1403);
            ppvsImg.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    (u2d[5] - u1d[5]).ToString(),
                    new SolidBrush(color),
                    null
                )
            );
            textOptions.Origin = new PointF(diffPoint, 1485);
            ppvsImg.Mutate(x =>
                x.DrawText(
                    drawOptions,
                    textOptions,
                    (u2d[0] - u1d[0]).ToString(),
                    new SolidBrush(color),
                    null
                )
            );

            using var title = await Img.LoadAsync($"work/legacy/ppvs_title.png");
            ppvsImg.Mutate(x => x.DrawImage(title, new Point(0, 0), 1));

            return ppvsImg;
        }

        public static Img DrawString(string str, float fontSize)
        {
            var font = new Font(HarmonySans, fontSize);
            var textOptions = new RichTextOptions(new Font(HarmonySans, fontSize))
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Origin = new PointF(fontSize / 2, fontSize / 2)
            };
            var m = TextMeasurer.MeasureSize(str, textOptions);

            var img = new Image<Rgba32>((int)(m.Width + fontSize), (int)(m.Height + fontSize));
            img.Mutate(x => x.Fill(Color.White));
            var drawOptions = new DrawingOptions
            {
                GraphicsOptions = new GraphicsOptions { Antialias = true }
            };
            img.Mutate(x =>
                x.DrawText(drawOptions, textOptions, str, new SolidBrush(Color.Black), null)
            );
            return img;
        }

        #region Hexagram
        public class Hexagram
        {
            public struct R8
            {
                public required double r,
                    _8;
            }

            public struct HexagramInfo
            {
                public required int size,
                    nodeCount,
                    nodeMaxValue,
                    sideLength,
                    mode;
                public required float strokeWidth;
                public required SizeF nodesize;
                public Color abilityFillColor,
                    abilityLineColor;
            }

            // 极坐标转直角坐标系
            public static PointF r82xy(R8 r8)
            {
                PointF xy =
                    new()
                    {
                        X = (float)(r8.r * Math.Sin(r8._8 * Math.PI / 180)),
                        Y = (float)(r8.r * Math.Cos(r8._8 * Math.PI / 180))
                    };
                return xy;
            }

            // ppd          pp_plus_data, 注意要与下面的multi和exp参数数量相同且对齐
            // multi, exp   加权值 y = multi * x ^ exp
            // hi           pp+图片的一些设置参数, hi.nodeCount
            public static Img Draw(int[] ppd, double[] multi, double[] exp, HexagramInfo hi)
            {
                var image = new Image<Rgba32>(hi.size, hi.size);
                PointF[] points = new PointF[hi.nodeCount];
                for (var i = 0; i < hi.nodeCount; i++)
                {
                    var r =
                        Math.Pow((multi[i] * Math.Pow(ppd[i], exp[i]) / hi.nodeMaxValue), 0.8)
                        * hi.size
                        / 2.0;
                    if (hi.mode == 1 && r > 100.00)
                        r = 100.00;
                    if (hi.mode == 2 && r > 395.00)
                        r = 395.00;
                    if (hi.mode == 3 && r > 495.00)
                        r = 495.00;
                    Hexagram.R8 r8 = new() { r = r, _8 = 360.0 / hi.nodeCount * i + 90 };
                    var xy = Hexagram.r82xy(r8);
                    xy.X += hi.size / 2;
                    xy.Y += hi.size / 2;
                    points[i] = xy;
                    xy.X += hi.nodesize.Width / 10;
                    xy.Y += hi.nodesize.Height / 10;
                    image.Mutate(x =>
                        x.Fill(hi.abilityLineColor, new EllipsePolygon(xy, hi.nodesize))
                    );
                }
                image.Mutate(x =>
                    x.DrawPolygon(hi.abilityLineColor, hi.strokeWidth, points)
                        .FillPolygon(hi.abilityFillColor, points)
                );
                return image;
            }
        }
        #endregion

        #region RoundedCorners
        private static IImageProcessingContext ApplyRoundedCorners(
            this IImageProcessingContext ctx,
            float cornerRadius
        )
        {
            Size size = ctx.GetCurrentSize();
            IPathCollection corners = BuildCorners(size.Width, size.Height, cornerRadius);

            ctx.SetGraphicsOptions(
                new GraphicsOptions()
                {
                    Antialias = true,
                    AlphaCompositionMode = PixelAlphaCompositionMode.DestOut // enforces that any part of this shape that has color is punched out of the background
                }
            );

            // mutating in here as we already have a cloned original
            // use any color (not Transparent), so the corners will be clipped
            foreach (var c in corners)
            {
                ctx = ctx.Fill(Color.Red, c);
            }
            return ctx;
        }

        private static IImageProcessingContext RoundCorner(
            this IImageProcessingContext processingContext,
            Size size,
            float cornerRadius
        )
        {
            return processingContext
                .Resize(new ResizeOptions { Size = size, Mode = ResizeMode.Crop })
                .ApplyRoundedCorners(cornerRadius);
        }

        private static IImageProcessingContext RoundCorner(
            this IImageProcessingContext processingContext,
            float cornerRadius
        )
        {
            return processingContext.ApplyRoundedCorners(cornerRadius);
        }

        private static IPathCollection BuildCorners(
            int imageWidth,
            int imageHeight,
            float cornerRadius
        )
        {
            // first create a square
            var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

            // then cut out of the square a circle so we are left with a corner
            IPath cornerTopLeft = rect.Clip(
                new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius)
            );

            // corner is now a corner shape positions top left
            //lets make 3 more positioned correctly, we can do that by translating the original around the center of the image

            float rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
            float bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;

            // move it across the width of the image - the width of the shape
            IPath cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
            IPath cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
            IPath cornerBottomRight = cornerTopLeft
                .RotateDegree(180)
                .Translate(rightPos, bottomPos);

            return new PathCollection(
                cornerTopLeft,
                cornerBottomLeft,
                cornerTopRight,
                cornerBottomRight
            );
        }
        #endregion
    }
}
