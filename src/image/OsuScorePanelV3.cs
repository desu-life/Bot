using System.IO;
using System.Numerics;
using KanonBot.API;
using KanonBot.Functions.OSU;
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
using System.Collections.Generic;
using SixLabors.ImageSharp.Diagnostics;
using KanonBot.Functions.OSUBot;
using static KanonBot.API.OSU.Models;
using LanguageExt.ClassInstances;
using OSU = KanonBot.API.OSU;
using static KanonBot.API.OSU.OSUExtensions;
using static KanonBot.Image.Fonts;
using static KanonBot.Image.ScoreV2;

namespace KanonBot.Image
{
    public static class OsuScorePanelV3
    {
        public static async Task<Img> Draw(ScorePanelData data)
        {
            var scoreimg = new Image<Rgba32>(2848, 1602);

            var ppInfo = data.ppInfo;

            //下载BG
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

            //下载头像
            using var avatar = await Utils.LoadOrDownloadAvatar(data.scoreInfo.User!);

            //panel
            using var panel = data.scoreInfo.Passed ?
                await Img.LoadAsync("./work/panelv2/score_panel/Score_v3_Passed_Panel.png") :
                await Img.LoadAsync("./work/panelv2/score_panel/Score_v3_Failed_Panel.png");
            if (data.scoreInfo.Passed)
                scoreimg.Mutate(x => x.DrawImage(panel, 1));
            else
                scoreimg.Mutate(x => x.DrawImage(panel, 1));

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
                    try { File.Delete(bgPath); } catch { }
                }
            }

            using var bgarea = new Image<Rgba32>(631, 444);
            bgarea.Mutate(x => x.Fill(Color.ParseHex("#f2f2f2")).RoundCorner(new Size(631, 444), 20));

            //beatmap status
            using var bgstatus = new Image<Rgba32>(619, 80);
            var beatmap_status_color = data.scoreInfo.Beatmap.Status switch
            {
                OSU.Models.Status.Approved => Color.ParseHex("#14b400"),
                OSU.Models.Status.Ranked => Color.ParseHex("#66bdff"),
                OSU.Models.Status.Loved => Color.ParseHex("#ff66aa"),
                _ => Color.ParseHex("#e08918")
            };
            bgstatus.Mutate(x => x.Fill(beatmap_status_color).RoundCorner(new Size(619, 80), 20));
            bgarea.Mutate(x => x.DrawImage(bgstatus, new Point(6, 358), 1));
            var bg2 = bg.Clone(x => x.RoundCorner(new Size(619, 401), 20));
            bgarea.Mutate(x => x.DrawImage(bg2, new Point(6, 6), 1));
            scoreimg.Mutate(x => x.DrawImage(bgarea, new Point(70, 51), 1));

            bg2.Dispose(); // 手动丢弃
            bg.Dispose(); // 手动丢弃

            //TODO beatmap status icon

            //beatmap difficulty icon
            using var osuscoremode_icon = await Utils.ReadImageRgba(
                        $"./work/panelv2/icons/mode_icon/score/{data.scoreInfo.Mode.ToStr()}.png"
            );
            osuscoremode_icon.Mutate(x => x.Resize(110, 110));
            var modeC = Utils.ForStarDifficulty(data.ppInfo.star);
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
            scoreimg.Mutate(
                        x =>
                            x.DrawImage(osuscoremode_icon, new Point(794, 381), 1)
                    );

            // avatar
            avatar.Mutate(x => x.Resize(100, 100).RoundCorner(new Size(100, 100), 50));
            scoreimg.Mutate(x => x.Fill(Color.White, new EllipsePolygon(140, 618, 105, 105)));
            scoreimg.Mutate(x => x.DrawImage(avatar, new Point(90, 568), 1));

            //设定textOption/drawOption
            var textOptions = new RichTextOptions(TorusSemiBold.Get(120))
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            //Beatmap infos
            var temp_string = "";
            foreach (char c in data.scoreInfo.Beatmapset!.Title)
            {
                temp_string += c;
                var m = TextMeasurer.MeasureSize(temp_string, textOptions);
                if (m.Width > 1130)
                {
                    temp_string += "...";
                    break;
                }
            }
            textOptions.Font = TorusSemiBold.Get(100);
            textOptions.Origin = new PointF(769, 160);
            scoreimg.Mutate(
                x => x.DrawText(textOptions,  temp_string, Color.ParseHex("#404040"))
            );
            textOptions.Origin = new PointF(769, 158);
            scoreimg.Mutate(
                x => x.DrawText(textOptions,  temp_string, Color.ParseHex("#4d4d4d"))
            );

            //creator
            temp_string = "";
            foreach (char c in data.scoreInfo.Beatmapset.Creator)
            {
                temp_string += c;
                var m = TextMeasurer.MeasureSize(temp_string, textOptions);
                if (m.Width > 810)
                {
                    temp_string += "...";
                    break;
                }
            }
            textOptions.Font = TorusRegular.Get(60);
            textOptions.Origin = new PointF(1070, 234);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions,  temp_string, Color.ParseHex("#e36a79"))
            );

            // artist
            temp_string = "";
            foreach (char c in data.scoreInfo.Beatmapset.Artist)
            {
                temp_string += c;
                var m = TextMeasurer.MeasureSize(temp_string, textOptions);
                if (m.Width > 450)
                {
                    temp_string += "...";
                    break;
                }
            }
            textOptions.Origin = new PointF(1005, 322);
            scoreimg.Mutate(
                x => x.DrawText(textOptions,  temp_string, Color.ParseHex("#6cac9c"))
            );
            // beatmap_id
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.Font = TorusRegular.Get(50);
            textOptions.Origin = new PointF(1770, 322);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, data.scoreInfo.Beatmap.BeatmapId.ToString(), Color.ParseHex("#5872df"))
            );

            //stars 
            textOptions.HorizontalAlignment = HorizontalAlignment.Left;
            textOptions.Font = TorusSemiBold.Get(50);

            var stars = $"Stars: {data.ppInfo.star:0.##}";
            var stars_measure = TextMeasurer.MeasureSize(stars, textOptions);

            textOptions.Origin = new PointF(924, 442);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, stars, Color.ParseHex("#3a3b3c"))
            );
            textOptions.Origin = new PointF(924, 441);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, stars, Color.ParseHex("#f1c959"))
            );

            //star icons

            var stars_i = (int)Math.Floor(data.ppInfo.star);
            var stars_d = data.ppInfo.star - Math.Truncate(data.ppInfo.star);
            int stars_pos = 924 + (int)stars_measure.Width + 10;
            using var stars_icon = await Utils.ReadImageRgba(
                    $"./work/panelv2/score_panel/Star.png"
            );
            stars_icon.Mutate(x => x.Resize(30, 30));
            if (stars_i < 19)
            {
                while (stars_i > 0)
                {
                    scoreimg.Mutate(
                            x =>
                                x.DrawImage(stars_icon, new Point(stars_pos, 401), 1)
                        );
                    stars_pos += 34;
                    stars_i--;
                }
            }
            var stars_d_t = 10 + (int)(20.0 * stars_d);
            var stars_d_t_1 = ((30 - stars_d_t) / 2);
            stars_icon.Mutate(x => x.Resize(stars_d_t, stars_d_t));
            scoreimg.Mutate(
                        x =>
                            x.DrawImage(stars_icon, new Point(stars_pos + stars_d_t_1, 401 + stars_d_t_1), 1)
                    );

            //version
            textOptions.Font = TorusRegular.Get(40);

            temp_string = "";
            foreach (char c in data.scoreInfo.Beatmap.Version)
            {
                temp_string += c;
                var m = TextMeasurer.MeasureSize(temp_string, textOptions);
                if (m.Width > 740)
                {
                    temp_string += "...";
                    break;
                }
            }

            textOptions.Origin = new PointF(924, 480);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, $"Version: {temp_string}", Color.ParseHex("#3a3b3c"))
            );

            textOptions.Origin = new PointF(924, 478);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, $"Version: {temp_string}", Color.ParseHex("#333333"))
            );

            //username 
            textOptions.Font = TorusSemiBold.Get(50);
            textOptions.Origin = new PointF(235, 630);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, data.scoreInfo.User!.Username, Color.ParseHex("#333333"))
            );

            //archived at
            textOptions.Font = TorusRegular.Get(36);
            textOptions.Origin = new PointF(235, 664);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, data.scoreInfo.EndedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"), Color.ParseHex("#333333"))
            );

            //draw mods
            if (data.scoreInfo.Mods.Length > 0)
            {
                var username_measure = TextMeasurer.MeasureSize(data.scoreInfo.User!.Username, textOptions);
                var archived_time_measure = TextMeasurer.MeasureSize(data.scoreInfo.EndedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"), textOptions);
                var ModAreaStartPos = 90 + 198 + (int)Math.Max(username_measure.Width, archived_time_measure.Width);
                foreach (var x in data.scoreInfo.Mods)
                {
                    if (!File.Exists($"./work/mods_v2/2x/{x.Acronym}.png")) continue;
                    using var modicon = await Img.LoadAsync($"./work/mods_v2/2x/{x.Acronym}.png");
                    modicon.Mutate(x => x.Resize(90, 90));
                    scoreimg.Mutate(
                                x =>
                                    x.DrawImage(
                                        modicon,
                                        new Point(ModAreaStartPos, 573),
                                        1
                                    )
                            );
                    ModAreaStartPos += 110;
                }
            }

            //main pp 
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.Font = TorusSemiBold.Get(80);
            textOptions.Origin = new PointF(2745, 655);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, "pp", Color.ParseHex("#cf93ae"))
            );
            var pp_measure = TextMeasurer.MeasureSize("pp", textOptions);
            textOptions.Origin = new PointF(2745 - pp_measure.Width, 655);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, ((int)data.ppInfo.ppStat.total).ToString(), Color.ParseHex("#fc65a9"))
            );


            //length graph 70x? -50    max 2708
            textOptions.Font = TorusRegular.Get(30);
            textOptions.Origin = new PointF(2750, 747);
            var beatmap_length_text = Utils.Duration2TimeStringForScoreV3(data.scoreInfo.Beatmap.TotalLength);
            var beatmap_length_text_measure = TextMeasurer.MeasureSize(beatmap_length_text, textOptions);
            var length_graph_length = 2708;

            if (!data.scoreInfo.Passed)
            {
                if (ppInfo.maxCombo != null)
                {
                    double online_obj_count = (double)(data.scoreInfo.Beatmap.CountCircles + data.scoreInfo.Beatmap.CountSliders + data.scoreInfo.Beatmap.CountSpinners);
                    double score_obj_count = 0;

                    if (data.scoreInfo.Mode == OSU.Mode.Mania)
                    {
                        score_obj_count = (int)(data.scoreInfo.Statistics.CountGeki
                            + data.scoreInfo.Statistics.CountKatu
                            + data.scoreInfo.Statistics.CountOk
                            + data.scoreInfo.Statistics.CountMiss
                            + data.scoreInfo.Statistics.CountMeh
                            + data.scoreInfo.Statistics.CountGreat);
                    }
                    else
                    {
                        score_obj_count = (int)(data.scoreInfo.Statistics.CountOk
                            + data.scoreInfo.Statistics.CountMiss
                            + data.scoreInfo.Statistics.CountMeh
                            + data.scoreInfo.Statistics.CountGreat);
                    }

                    length_graph_length = (int)((2708.0 - (double)beatmap_length_text_measure.Width - 60.0) * (score_obj_count / online_obj_count));
                }
                else
                {
                    length_graph_length = 2708 - (int)beatmap_length_text_measure.Width - 60;
                }

            }
            length_graph_length = Math.Max(85, length_graph_length);

            using var length_graph_area = new Image<Rgba32>(length_graph_length, 50);

            if (data.scoreInfo.Passed)
            {
                length_graph_area.Mutate(x => x.Fill(Color.ParseHex("#c5e8f7")).RoundCorner(new Size(length_graph_length, 50), 26));
            }
            else
            {
                length_graph_area.Mutate(x => x.Fill(Color.ParseHex("#cc4e53")).RoundCorner(new Size(length_graph_length, 50), 26));
            }

            scoreimg.Mutate(x => x.DrawImage(length_graph_area, new Point(70, 706), 1));

            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, beatmap_length_text, data.scoreInfo.Passed ? Color.ParseHex("#311314") : Color.ParseHex("#3d3d3d"))
            );
            textOptions.Origin = new PointF(2750, 746);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, beatmap_length_text, data.scoreInfo.Passed ? Color.ParseHex("#585858") : Color.ParseHex("#333333"))
            );
            if (!data.scoreInfo.Passed)
            {
                textOptions.Font = TorusSemiBold.Get(80);
                textOptions.Origin = new PointF(length_graph_length + 120, 770);
                scoreimg.Mutate(
                    x =>
                        x.DrawText(textOptions, "×", Color.ParseHex("#cc4e53"))
                );
            }
            textOptions.HorizontalAlignment = HorizontalAlignment.Left;
            textOptions.Font = TorusRegular.Get(30);
            textOptions.Origin = new PointF(90, 747);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, data.scoreInfo.Passed ? "Finish" : "Fail", Color.ParseHex("#311314"))
            );

            textOptions.Origin = new PointF(90, 746);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, data.scoreInfo.Passed ? "Finish" : "Fail", data.scoreInfo.Passed ? Color.ParseHex("#585858") : Color.ParseHex("#e6e6e6"))
            );

            //main pp details
            var mainpp_details_pos_base = 2196;
            var pp_details_posy_base = 938; //942
            textOptions.Font = TorusSemiBold.Get(50);
            textOptions.Origin = new PointF(mainpp_details_pos_base, pp_details_posy_base);
            //aim
            var mainpp_text = ((int)data.ppInfo.ppStat.aim!).ToString();
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, mainpp_text, Color.ParseHex("#fc65a9"))
            );

            pp_measure = TextMeasurer.MeasureSize(mainpp_text, textOptions);
            textOptions.Origin = new PointF(mainpp_details_pos_base + pp_measure.Width, pp_details_posy_base);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, "pp", Color.ParseHex("#cf93ae"))
            );

            //spd
            mainpp_details_pos_base = 2401;
            mainpp_text = ((int)data.ppInfo.ppStat.speed!).ToString();
            textOptions.Origin = new PointF(mainpp_details_pos_base, pp_details_posy_base);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, mainpp_text, Color.ParseHex("#fc65a9"))
            );

            pp_measure = TextMeasurer.MeasureSize(mainpp_text, textOptions);
            textOptions.Origin = new PointF(mainpp_details_pos_base + pp_measure.Width, pp_details_posy_base);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, "pp", Color.ParseHex("#cf93ae"))
            );

            //spd
            mainpp_details_pos_base = 2596;
            mainpp_text = ((int)data.ppInfo.ppStat.acc!).ToString();
            textOptions.Origin = new PointF(mainpp_details_pos_base, pp_details_posy_base);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, mainpp_text, Color.ParseHex("#fc65a9"))
            );

            pp_measure = TextMeasurer.MeasureSize(mainpp_text, textOptions);
            textOptions.Origin = new PointF(mainpp_details_pos_base + pp_measure.Width, pp_details_posy_base);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, "pp", Color.ParseHex("#cf93ae"))
            );

            //prediction pps
            mainpp_details_pos_base = 108;

            for (int i = 0; i < 5; i++)
            {
                mainpp_text = ((int)data.ppInfo.ppStats![4 - i].total).ToString();
                textOptions.Origin = new PointF(mainpp_details_pos_base, pp_details_posy_base);
                scoreimg.Mutate(
                    x =>
                        x.DrawText(textOptions, mainpp_text, Color.ParseHex("#fc65a9"))
                );

                pp_measure = TextMeasurer.MeasureSize(mainpp_text, textOptions);
                textOptions.Origin = new PointF(mainpp_details_pos_base + pp_measure.Width, pp_details_posy_base);
                scoreimg.Mutate(
                    x =>
                        x.DrawText(textOptions, "pp", Color.ParseHex("#cf93ae"))
                );
                mainpp_details_pos_base += 204;
            }

            //if fc
            textOptions.Font = TorusRegular.Get(36);
            mainpp_details_pos_base = 178;
            pp_details_posy_base = 831;
            mainpp_text = ((int)data.ppInfo.ppStats![5].total).ToString();
            textOptions.Origin = new PointF(mainpp_details_pos_base, pp_details_posy_base);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, mainpp_text, Color.ParseHex("#3b3b3b"))
            );

            pp_measure = TextMeasurer.MeasureSize(mainpp_text, textOptions);
            textOptions.Origin = new PointF(mainpp_details_pos_base + pp_measure.Width, pp_details_posy_base);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, "pp", Color.ParseHex("#3b3b3b"))
            );
            pp_details_posy_base = 830;
            mainpp_text = ((int)data.ppInfo.ppStats![5].total).ToString();
            textOptions.Origin = new PointF(mainpp_details_pos_base, pp_details_posy_base);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, mainpp_text, Color.ParseHex("#fc65a9"))
            );

            pp_measure = TextMeasurer.MeasureSize(mainpp_text, textOptions);
            textOptions.Origin = new PointF(mainpp_details_pos_base + pp_measure.Width, pp_details_posy_base);
            scoreimg.Mutate(
                x =>
                    x.DrawText(textOptions, "pp", Color.ParseHex("#cf93ae"))
            );








            




            return scoreimg;
        }
    }
}
