﻿using System.IO;
using System.Numerics;
using OSU = KanonBot.API.OSU;
using static KanonBot.API.OSU.OSUExtensions;
using KanonBot.Image;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Img = SixLabors.ImageSharp.Image;
using ResizeOptions = SixLabors.ImageSharp.Processing.ResizeOptions;
using KanonBot.OsuPerformance;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static KanonBot.Image.Fonts;

namespace KanonBot.Image
{
    public static class ScoreList
    {
        public enum Type
        {
            TODAYBP,
            BPLIST,
            RECENTLIST
        }

        public class ScoreRank {
            public required int Rank;
            public required OSU.Models.ScoreLazer Score;
            public PPInfo? PPInfo;
        }


        public static async Task<Img> Draw(
            Type type,
            List<ScoreRank> scoreList,
            OSU.Models.User userInfo
        )
        {
            //get pp
       
            // for (int i = 0; i < scoreList.Count; i++) {
            //     scoreList[i].PPInfo = await UniversalCalculator.CalculateData(scoreList[i], kind);
            // }

            
            // scoreList.Sort((a, b) => b.pp > a.pp ? 1 : -1);
            // ppinfos.Sort((a, b) => b.ppStat.total > a.ppStat.total ? 1 : -1);
            

            //设定textOption/drawOption
            var textOptions = new RichTextOptions(TorusSemiBold.Get(120))
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
                FallbackFontFamilies = [HarmonySans, HarmonySansArabic]
            };

            //页眉 2000x697
            //页中 2000x186
            //页脚 2000x70

            string? MainPicPath = type switch
            {
                Type.TODAYBP => "./work/panelv2/tbp_main_score.png",
                Type.BPLIST => "./work/panelv2/bplist_main_score.png",
                _ => null
            };

            //计算图像大小并生成原始图像
            Img image;
            if (scoreList.Count > 1)
            {
                if (MainPicPath is not null) {
                    var t = 70 + 697 + 186 * (scoreList.Count - 1);
                    image = new Image<Rgba32>(2000, t);
                } else {
                    var t = 70 + 454 + 186 * (scoreList.Count - 1);
                    image = new Image<Rgba32>(2000, t);
                }
            }
            else
            {
                image = new Image<Rgba32>(2000, 767);
            }

            image.Mutate(x => x.Fill(Color.White));

            //头像、用户名
            using var avatar = await Utils.LoadOrDownloadAvatar(userInfo);
            avatar.Mutate(x => x.Resize(160, 160).RoundCorner(new Size(160, 160), 25));
            image.Mutate(x => x.DrawImage(avatar, new Point(56, 60), 1));
            //username
            textOptions.Origin = new PointF(256, 195);
            textOptions.Font = TorusSemiBold.Get(100);
            image.Mutate(
                x =>
                    x.DrawText(textOptions, userInfo.Username, Color.ParseHex("#4d4d4d"))
            );

            //绘制页眉
            if (MainPicPath is not null) {
                using var MainPic = await Utils.ReadImageRgba(MainPicPath);

                //绘制beatmap图像
                var scorebgPath = $"./work/background/{scoreList[0].Score.Beatmap!.BeatmapId}.png";
                if (!File.Exists(scorebgPath))
                {
                    scorebgPath = null;
                    try
                    {
                        scorebgPath = await OSU.Client.SayoDownloadBeatmapBackgroundImg(
                            scoreList[0].Score.Beatmapset!.Id,
                            scoreList[0].Score.Beatmap!.BeatmapId,
                            "./work/background/"
                        );
                    }
                    catch (Exception ex)
                    {
                        var msg = $"从Sayo API下载背景图片时发生了一处异常\n异常类型: {ex.GetType()}\n异常信息: '{ex.Message}'";
                        Log.Warning(msg);
                    }

                    if (scorebgPath is null)
                    {
                        try
                        {
                            scorebgPath = await OSU.Client.DownloadBeatmapBackgroundImg(
                                scoreList[0].Score.Beatmapset!.Id,
                                "./work/background/",
                                $"{scoreList[0].Score.Beatmap!.BeatmapId}.png"
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
                
                Image<Rgba32> scorebg;
                if (scorebgPath is null)
                {
                    scorebg = await Img.LoadAsync<Rgba32>("./work/legacy/load-failed-img.png");
                }
                else
                {
                    try
                    {
                        scorebg = await Img.LoadAsync<Rgba32>(scorebgPath);
                    }
                    catch
                    {
                        scorebg = await Img.LoadAsync<Rgba32>("./work/legacy/load-failed-img.png");
                        try { File.Delete(scorebgPath); } catch { }
                    }
                }

                scorebg.Mutate(
                    x =>
                        x.Resize(new ResizeOptions() { Size = new Size(365, 0), Mode = ResizeMode.Max })
                );

                using var bgtemp = new Image<Rgba32>(365, 210);
                bgtemp.Mutate(x => x.DrawImage(scorebg, new Point(0, 0), 1));
                image.Mutate(x => x.DrawImage(bgtemp, new Point(92, 433), 1));
                image.Mutate(x => x.DrawImage(MainPic, new Point(0, 0), 1));

                scorebg.Dispose(); // 手动释放资源

                //pp
                textOptions.HorizontalAlignment = HorizontalAlignment.Left;
                textOptions.VerticalAlignment = VerticalAlignment.Center;
                textOptions.Origin = new PointF(1782, 132);
                if (userInfo.Statistics.PP > 9999) {
                    textOptions.Font = TorusRegular.Get(48);
                } else {
                    textOptions.Font = TorusRegular.Get(58);
                }
                image.Mutate(
                    x =>
                        x.DrawText(textOptions, string.Format("{0:N0}", userInfo.Statistics.PP), Color.ParseHex("#e36a79"))
                );

                textOptions.VerticalAlignment = VerticalAlignment.Bottom;

                //绘制页眉的信息 496x585
                //title  +mods
                textOptions.Font = TorusRegular.Get(90);
                textOptions.Origin = new PointF(485, 540);
                var mainTitle = "";
                foreach (char c in scoreList[0].Score.Beatmapset!.Title)
                {
                    mainTitle += c;
                    var m = TextMeasurer.MeasureSize(mainTitle, textOptions);
                    if (m.Width > 725)
                    {
                        mainTitle += "...";
                        break;
                    }
                }
                image.Mutate(
                    x =>
                        x.DrawText(textOptions, mainTitle, Color.ParseHex("#656b6d"))
                );
                //mods

                if (scoreList[0].Score.IsClassic) {
                    scoreList[0].Score.Mods = scoreList[0].Score.Mods.Filter(x => !x.IsClassic).ToArray();
                }

                if (scoreList[0].Score.Mods.Length > 0)
                {
                    textOptions.Origin = new PointF(
                        485 + TextMeasurer.MeasureSize(mainTitle, textOptions).Width + 25,
                        530
                    );
                    textOptions.Font = TorusRegular.Get(40);
                    var mainscoremods = "+";
                    foreach (var x in scoreList[0].Score.Mods) {
                        mainscoremods += $"{x.Acronym}, ";
                    }
                    image.Mutate(
                        x =>
                            x.DrawText(textOptions, mainscoremods[..mainscoremods.LastIndexOf(",")] + $" #{scoreList[0].Rank}", Color.ParseHex("#656b6d"))
                    );
                }
                else
                {
                    textOptions.Origin = new PointF(
                        485 + TextMeasurer.MeasureSize(mainTitle, textOptions).Width + 25,
                        530
                    );
                    textOptions.Font = TorusRegular.Get(40);
                    image.Mutate(
                        x =>
                            x.DrawText(textOptions, $"#{scoreList[0].Rank}", Color.ParseHex("#656b6d"))
                    );
                }

                int mainScoreXPos = 585;
                //artist
                textOptions.Font = TorusRegular.Get(38);
                textOptions.Origin = new PointF(495, mainScoreXPos);
                var artist = "";
                foreach (char c in scoreList[0].Score.Beatmapset!.Artist)
                {
                    artist += c;
                    var m = TextMeasurer.MeasureSize(artist, textOptions);
                    if (m.Width > 205)
                    {
                        artist += "...";
                        break;
                    }
                }
                image.Mutate(
                    x =>
                        x.DrawText(textOptions, artist, Color.ParseHex("#656b6d"))
                );

                //creator
                textOptions.Origin = new PointF(769, mainScoreXPos);
                var creator = "";
                foreach (char c in scoreList[0].Score.Beatmapset!.Creator)
                {
                    creator += c;
                    var m = TextMeasurer.MeasureSize(creator, textOptions);
                    if (m.Width > 145)
                    {
                        creator += "...";
                        break;
                    }
                }
                image.Mutate(
                    x =>
                        x.DrawText(textOptions, creator, Color.ParseHex("#656b6d"))
                );

                //bid
                textOptions.Origin = new PointF(985, mainScoreXPos);
                image.Mutate(
                    x =>
                        x.DrawText(textOptions, scoreList[0].Score.Beatmap!.BeatmapId.ToString(), Color.ParseHex("#656b6d"))
                );

                

                textOptions.Origin = new PointF(1182, mainScoreXPos);
                image.Mutate(
                    x =>
                        x.DrawText(textOptions, scoreList[0].PPInfo!.star.ToString("0.##*"), Color.ParseHex("#656b6d"))
                );

                //acc
                textOptions.Origin = new PointF(1308, mainScoreXPos);
                image.Mutate(
                    x =>
                        x.DrawText(textOptions, scoreList[0].Score.AccAuto.ToString("0.##%"), Color.ParseHex("#656b6d"))
                );

                //rank
                textOptions.Origin = new PointF(1459, mainScoreXPos);
                image.Mutate(
                    x =>
                        x.DrawText(textOptions, scoreList[0].Score.RankAuto, Color.ParseHex("#656b6d"))
                );

                //pp
                textOptions.Font = TorusRegular.Get(90);
                textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                textOptions.Origin = new PointF(1790, 608);
                image.Mutate(
                    x =>
                        x.DrawText(textOptions, string.Format("{0:N1}", scoreList[0].PPInfo!.ppStat.total), Color.ParseHex("#364a75"))
                );
                var bp1pptextMeasure = TextMeasurer.MeasureSize(
                    string.Format("{0:N1}", scoreList[0].PPInfo!.ppStat.total),
                    textOptions
                );
                int bp1pptextpos = 1790 - (int)bp1pptextMeasure.Width / 2;
                textOptions.Font = TorusRegular.Get(40);
                textOptions.Origin = new PointF(bp1pptextpos, 522);
                textOptions.HorizontalAlignment = HorizontalAlignment.Left;
                image.Mutate(
                    x =>
                        x.DrawText(textOptions, "pp", Color.ParseHex("#656b6d"))
                );
            } else {
                using var MainPic = await Utils.ReadImageRgba("./work/panelv2/bplist_user_pp.png");
                image.Mutate(x => x.DrawImage(MainPic, new Point(0, 0), 1));
            
                //pp
                textOptions.HorizontalAlignment = HorizontalAlignment.Left;
                textOptions.VerticalAlignment = VerticalAlignment.Center;
                textOptions.Origin = new PointF(1782, 132);
                if (userInfo.Statistics.PP > 9999) {
                    textOptions.Font = TorusRegular.Get(48);
                } else {
                    textOptions.Font = TorusRegular.Get(58);
                }
                image.Mutate(
                    x =>
                        x.DrawText(textOptions, string.Format("{0:N0}", userInfo.Statistics.PP), Color.ParseHex("#e36a79"))
                );
            }

            textOptions.VerticalAlignment = VerticalAlignment.Bottom;

            var startIndex = MainPicPath is null ? 0 : 1;
            var startPos = MainPicPath is null ? 455 : 698;

            //页中
            using var ScoreListSingle = await Utils.ReadImageRgba("./work/panelv2/score_list.png");
            for (int i = startIndex; i < scoreList.Count; ++i)
            {
                using var SubPic = ScoreListSingle.Clone();
                using var osuscoremode_icon = await Utils.ReadImageRgba(
                    $"./work/panelv2/icons/mode_icon/score/{scoreList[i].Score.Mode.ToStr()}.png"
                );

                //Difficulty icon
                Color modeC = Utils.ForStarDifficulty(scoreList[i].PPInfo!.star);
                osuscoremode_icon.Mutate(x => x.Resize(92, 92));
                osuscoremode_icon.Mutate(
                    x =>
                        x.ProcessPixelRowsAsVector4(row =>
                        {
                            for (int p = 0; p < row.Length; p++)
                            {
                                row[p].X = ((Vector4)modeC).X;
                                row[p].Y = ((Vector4)modeC).Y;
                                row[p].Z = ((Vector4)modeC).Z;
                            }
                        })
                );
                SubPic.Mutate(x => x.DrawImage(osuscoremode_icon, new Point(92, 48), 1));

                //main title
                textOptions.HorizontalAlignment = HorizontalAlignment.Left;
                textOptions.Font = TorusRegular.Get(50);
                var title = "";
                foreach (char c in scoreList[i].Score.Beatmapset!.Title)
                {
                    title += c;
                    var m = TextMeasurer.MeasureSize(title, textOptions);
                    if (m.Width > 710)
                    {
                        title += "...";
                        break;
                    }
                }
                textOptions.Origin = new PointF(204, 96);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, title, Color.ParseHex("#656b6d"))
                );
                //Rank
                textOptions.Font = TorusRegular.Get(34);
                textOptions.Origin = new PointF(204, 138);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, $"#{scoreList[i].Rank}", Color.ParseHex("#656b6d"))
                );
                var textMeasurePos =
                    204 + TextMeasurer.MeasureSize($"#{scoreList[i].Rank}", textOptions).Width + 5;

                //split
                textOptions.Origin = new PointF(textMeasurePos, 138);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, " | ", Color.ParseHex("#656b6d"))
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //version
                title = "";
                foreach (char c in scoreList[i].Score.Beatmap!.Version)
                {
                    title += c;
                    var m = TextMeasurer.MeasureSize(title, textOptions);
                    if (m.Width > 130)
                    {
                        title += "...";
                        break;
                    }
                }
                textOptions.Origin = new PointF(textMeasurePos, 138);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, title, Color.ParseHex("#656b6d"))
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(title, textOptions).Width + 5;

                //split
                textOptions.Origin = new PointF(textMeasurePos, 138);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, " | ", Color.ParseHex("#656b6d"))
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //bid
                textOptions.Origin = new PointF(textMeasurePos, 138);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, scoreList[i].Score.Beatmap!.BeatmapId.ToString(), Color.ParseHex("#656b6d"))
                );
                textMeasurePos =
                    textMeasurePos
                    + TextMeasurer.MeasureSize(scoreList[i].Score.Beatmap!.BeatmapId.ToString(), textOptions).Width
                    + 5;

                //split
                textOptions.Origin = new PointF(textMeasurePos, 138);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, " | ", Color.ParseHex("#656b6d"))
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //star
                textOptions.Origin = new PointF(textMeasurePos, 138);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, scoreList[i].PPInfo!.star.ToString("0.##*"), Color.ParseHex("#656b6d"))
                );
                textMeasurePos =
                    textMeasurePos
                    + TextMeasurer.MeasureSize(scoreList[i].PPInfo!.star.ToString("0.##*"), textOptions).Width
                    + 5;

                //split
                textOptions.Origin = new PointF(textMeasurePos, 138);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, " | ", Color.ParseHex("#656b6d"))
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //acc
                textOptions.Origin = new PointF(textMeasurePos, 138);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, scoreList[i].Score.AccAuto.ToString("0.##%"), Color.ParseHex("#ffcd22"))
                );
                textMeasurePos =
                    textMeasurePos
                    + TextMeasurer.MeasureSize(scoreList[i].Score.AccAuto.ToString("0.##%"), textOptions).Width
                    + 5;

                //split
                textOptions.Origin = new PointF(textMeasurePos, 138);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, " | ", Color.ParseHex("#656b6d"))
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //ranking
                textOptions.Origin = new PointF(textMeasurePos, 138);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, scoreList[i].Score.RankAuto, Color.ParseHex("#656b6d"))
                );

                //mods
                if (scoreList[i].Score.IsClassic) {
                    scoreList[i].Score.Mods = scoreList[i].Score.Mods.Filter(x => !x.IsClassic).ToArray();
                }

                if (scoreList[i].Score.Mods.Length > 0)
                {
                    var mods_pos_x = 1043;
                    if (scoreList[i].Score.Mods.Length > 6)
                    {
                        //大于6个
                        foreach (var x in scoreList[i].Score.Mods)
                        {
                            if (!File.Exists($"./work/mods_v2/2x/{x.Acronym}.png")) continue;
                            using var modicon = await Img.LoadAsync($"./work/mods_v2/2x/{x.Acronym}.png");
                            modicon.Mutate(x => x.Resize(90, 90));
                            SubPic.Mutate(x => x.DrawImage(modicon, new Point(mods_pos_x, 48), 1));
                            mods_pos_x += 70 - (scoreList[i].Score.Mods.Length - 7) * 9;
                        }
                    }
                    else if (scoreList[i].Score.Mods.Length > 5)
                    {
                        //等于6个
                        foreach (var x in scoreList[i].Score.Mods)
                        {
                            if (!File.Exists($"./work/mods_v2/2x/{x.Acronym}.png")) continue;
                            using var modicon = await Img.LoadAsync($"./work/mods_v2/2x/{x.Acronym}.png");
                            modicon.Mutate(x => x.Resize(90, 90));
                            SubPic.Mutate(x => x.DrawImage(modicon, new Point(mods_pos_x, 48), 1));
                            mods_pos_x += 84;
                        }
                    }
                    else
                    {
                        //小于6个
                        foreach (var x in scoreList[i].Score.Mods)
                        {
                            if (!File.Exists($"./work/mods_v2/2x/{x.Acronym}.png")) continue;
                            using var modicon = await Img.LoadAsync($"./work/mods_v2/2x/{x.Acronym}.png");
                            modicon.Mutate(x => x.Resize(90, 90));
                            SubPic.Mutate(x => x.DrawImage(modicon, new Point(mods_pos_x, 48), 1));
                            mods_pos_x += 105;
                        }
                    }
                }

                //pp
                textOptions.Font = TorusRegular.Get(70);
                textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                textOptions.Origin = new PointF(1790, 128);
                SubPic.Mutate(
                    x =>
                        x.DrawText(textOptions, string.Format("{0:N0}pp", scoreList[i].PPInfo!.ppStat.total), Color.ParseHex("#ff7bac"))
                );

                //draw
                image.Mutate(x => x.DrawImage(SubPic, new Point(0, startPos + (i - 1) * 186 + 1), 1));
            }
            //页尾
            using var FooterPic = await Utils.ReadImageRgba("./work/panelv2/score_list_footer.png");
            image.Mutate(
                x => x.DrawImage(FooterPic, new Point(0, startPos + (scoreList.Count - 1) * 186 + 1), 1)
            );

            // 不知道为啥更新了imagesharp后对比度(亮度)变了
            image.Mutate(x => x.Brightness(0.998f));

            return image;
        }
    }
}
