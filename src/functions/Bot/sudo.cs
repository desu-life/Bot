﻿using System.Collections.Generic;
using System.IO;
using Flurl.Util;
using KanonBot.Drivers;
using KanonBot.Functions.OSUBot;
using KanonBot.Message;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using static KanonBot.Functions.Accounts;

namespace KanonBot.Functions.OSU
{
    public static class Sudo
    {
        public static async Task Execute(Target target, string cmd)
        {
            if (target.isFromAdmin == false) return;
            try
            {
                var AccInfo = GetAccInfo(target);
                var userinfo = await Database.Client.GetUsersByUID(AccInfo.uid, AccInfo.platform);
                if (userinfo == null)
                {
                    return; //直接忽略
                }
                List<string> permissions = new();
                if (userinfo!.permissions!.IndexOf(";") < 1) //一般不会出错，默认就是user
                {
                    permissions.Add(userinfo.permissions);
                }
                else
                {
                    var t1 = userinfo.permissions.Split(";");
                    foreach (var x in t1)
                    {
                        permissions.Add(x);
                    }
                }
                //检查用户权限
                int permissions_flag = -1;
                foreach (var x in permissions)
                {
                    switch (x)
                    {
                        case "restricted":
                            permissions_flag = -3;
                            break;
                        case "banned":
                            permissions_flag = -1;
                            break;
                        case "user":
                            if (permissions_flag < 1)
                                permissions_flag = 1;
                            break;
                        case "mod":
                            if (permissions_flag < 2)
                                permissions_flag = 2;
                            break;
                        case "admin":
                            if (permissions_flag < 3)
                                permissions_flag = 3;
                            break;
                        case "system":
                            permissions_flag = -2;
                            break;
                        default:
                            break;
                    }
                }
                //foreach (var x in permissions)
                //{
                //    Console.WriteLine(x);
                //}
                //Console.WriteLine(permissions + "\n" + permissions_flag);

                if (permissions_flag < 2)
                    return; //权限不够不处理

                //execute
                string rootCmd,
                    childCmd = "";
                try
                {
                    var tmp = cmd.Split(' ', 2, StringSplitOptions.TrimEntries);;
                    rootCmd = tmp[0];
                    childCmd = tmp[1];
                }
                catch
                {
                    rootCmd = cmd;
                }

                switch (rootCmd.ToLower())
                {
                    //get all queued items
                    case "listall":
                        await InfoImageV1(target, -1, childCmd); await Task.Delay(1000);
                        await InfoPanelV1(target, -1, childCmd); await Task.Delay(1000);
                        await InfoImageV2(target, -1, childCmd); await Task.Delay(1000);
                        await InfoPanelV2(target, -1, childCmd);
                        return;
                    //approve all items
                    case "vall":
                        await InfoImageV1(target, 0, childCmd); await Task.Delay(1000);
                        await InfoPanelV1(target, 0, childCmd); await Task.Delay(1000);
                        await InfoImageV2(target, 0, childCmd); await Task.Delay(1000);
                        await InfoPanelV2(target, 0, childCmd);
                        return;
                    //v1infoImg
                    case "v1imagelist":
                        await InfoImageV1(target, -1, childCmd);
                        return;
                    case "1il":
                        await InfoImageV1(target, -1, childCmd);
                        return;
                    case "v1imageapproveall":
                        await InfoImageV1(target, 0, childCmd);
                        return;
                    case "1iall":
                        await InfoImageV1(target, 0, childCmd);
                        return;
                    case "v1imageapprove":
                        await InfoImageV1(target, 1, childCmd);
                        return;
                    case "1ia":
                        await InfoImageV1(target, 1, childCmd);
                        return;
                    case "v1imagereject":
                        await InfoImageV1(target, 2, childCmd);
                        return;
                    case "1ij":
                        await InfoImageV1(target, 2, childCmd);
                        return;
                    //v1infoPanel
                    case "v1panellist":
                        await InfoPanelV1(target, -1, childCmd);
                        return;
                    case "1pl":
                        await InfoPanelV1(target, -1, childCmd);
                        return;
                    case "v1panelapproveall":
                        await InfoPanelV1(target, 0, childCmd);
                        return;
                    case "1pall":
                        await InfoPanelV1(target, 0, childCmd);
                        return;
                    case "v1panelapprove":
                        await InfoPanelV1(target, 1, childCmd);
                        return;
                    case "1pa":
                        await InfoPanelV1(target, 1, childCmd);
                        return;
                    case "v1panelreject":
                        await InfoPanelV1(target, 2, childCmd);
                        return;
                    case "1pj":
                        await InfoPanelV1(target, 2, childCmd);
                        return;
                    //v2infoImg
                    case "v2imagelist":
                        await InfoImageV2(target, -1, childCmd);
                        return;
                    case "2il":
                        await InfoImageV2(target, -1, childCmd);
                        return;
                    case "v2imageapproveall":
                        await InfoImageV2(target, 0, childCmd);
                        return;
                    case "2iall":
                        await InfoImageV2(target, 0, childCmd);
                        return;
                    case "v2imageapprove":
                        await InfoImageV2(target, 1, childCmd);
                        return;
                    case "2ia":
                        await InfoImageV2(target, 1, childCmd);
                        return;
                    case "v2imagereject":
                        await InfoImageV2(target, 2, childCmd);
                        return;
                    case "2ij":
                        await InfoImageV2(target, 2, childCmd);
                        return;
                    //v2infoPanel
                    case "v2panellist":
                        await InfoPanelV2(target, -1, childCmd);
                        return;
                    case "2pl":
                        await InfoPanelV2(target, -1, childCmd);
                        return;
                    case "v2panelapproveall":
                        await InfoPanelV2(target, 0, childCmd);
                        return;
                    case "2pall":
                        await InfoPanelV2(target, 0, childCmd);
                        return;
                    case "v2panelapprove":
                        await InfoPanelV2(target, 1, childCmd);
                        return;
                    case "2pa":
                        await InfoPanelV2(target, 1, childCmd);
                        return;
                    case "v2panelreject":
                        await InfoPanelV2(target, 2, childCmd);
                        return;
                    case "2pj":
                        await InfoPanelV2(target, 2, childCmd);
                        return;
                    default:
                        return;
                }
            }
            catch { } //直接忽略
        }

        //approve -1=sendlist 0=approveall 1=approve 2=reject
        public static async Task InfoPanelV1(Target target, int approve, string cmd)
        {
            switch (approve)
            {
                case -1:
                    //send list
                    var files = Directory.GetFiles(@"./work/legacy/v1_infopanel/verify/");
                    if (files.Length > 0)
                    {
                        var msg = new Chain();
                        msg.msg("以下v1 info panel需要审核");
                        foreach (var file in files)
                        {
                            msg.msg($"\n{Path.GetFileName(file)}\n");
                            using var stream = new MemoryStream();
                            await SixLabors.ImageSharp.Image
                                .Load(file)
                                .CloneAs<Rgba32>()
                                .SaveAsync(stream, new PngEncoder());
                            msg.image(
                                Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                                ImageSegment.Type.Base64
                            );
                        }
                        await target.reply(msg);
                        return;
                    }
                    await target.reply("[panelv1]暂时没有待审核的内容。");
                    return;
                case 0:
                    //approve all
                    var filesall = Directory.GetFiles(@"./work/legacy/v1_infopanel/verify/");
                    if (filesall.Length > 0)
                    {
                        foreach (var file in filesall)
                        {
                            var destpath = @"./work/legacy/v1_infopanel/" + Path.GetFileName(file);
                            if (File.Exists(destpath))
                                File.Delete(destpath);
                            File.Move(file, destpath);
                        }
                        await target.reply("approved.");
                        return;
                    }
                    await target.reply("[panelv1]暂时没有待审核的内容。");
                    return;
                case 1:
                    //approve
                    if (File.Exists(@$"./work/legacy/v1_infopanel/verify/{cmd}.png"))
                    {
                        if (File.Exists(@$"./work/legacy/v1_infopanel/{cmd}.png"))
                            File.Delete(@$"./work/legacy/v1_infopanel/{cmd}.png");
                        File.Move(
                            @$"./work/legacy/v1_infopanel/verify/{cmd}.png",
                            @$"./work/legacy/v1_infopanel/{cmd}.png"
                        );
                        await target.reply("approved.");
                        return;
                    }
                    await target.reply("[panelv1]指定内容不存在，请重新检查。");
                    return;
                case 2:
                    //reject
                    if (File.Exists(@$"./work/legacy/v1_infopanel/verify/{cmd}.png"))
                    {
                        File.Delete(@$"./work/legacy/v1_infopanel/verify/{cmd}.png");
                        await target.reply("rejected.");
                        return;
                    }
                    await target.reply("[panelv1]要审核的内容不存在，请重新检查。");
                    return;
                default:
                    //do nothing
                    await target.reply("[panelv1]无效的指令，请重新检查。");
                    return;
            }
        }

        public static async Task InfoPanelV2(Target target, int approve, string cmd)
        {
            switch (approve)
            {
                case -1:
                    //send list
                    var files = Directory.GetFiles(@"./work/panelv2/user_infopanel/verify/");
                    if (files.Length > 0)
                    {
                        var msg = new Chain();
                        msg.msg("以下v2 info panel需要审核");
                        foreach (var file in files)
                        {
                            msg.msg($"\n{Path.GetFileName(file)}\n");
                            using var stream = new MemoryStream();
                            await SixLabors.ImageSharp.Image
                                .Load(file)
                                .CloneAs<Rgba32>()
                                .SaveAsync(stream, new PngEncoder());
                            msg.image(
                                Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                                ImageSegment.Type.Base64
                            );
                        }
                        await target.reply(msg);
                        return;
                    }
                    await target.reply("[panelv2]暂时没有待审核的内容。");
                    return;
                case 0:
                    //approve all
                    var filesall = Directory.GetFiles(@"./work/panelv2/user_infopanel/verify/");
                    if (filesall.Length > 0)
                    {
                        foreach (var file in filesall)
                        {
                            var destpath =
                                @"./work/panelv2/user_infopanel/" + Path.GetFileName(file);
                            if (File.Exists(destpath))
                                File.Delete(destpath);
                            File.Move(file, destpath);
                        }
                        await target.reply("approved.");
                        return;
                    }
                    await target.reply("[panelv2]暂时没有待审核的内容。");
                    return;
                case 1:
                    //approve
                    if (File.Exists(@$"./work/panelv2/user_infopanel/verify/{cmd}.png"))
                    {
                        if (File.Exists(@$"./work/panelv2/user_infopanel/{cmd}.png"))
                            File.Delete(@$"./work/panelv2/user_infopanel/{cmd}.png");
                        File.Move(
                            @$"./work/panelv2/user_infopanel/verify/{cmd}.png",
                            @$"./work/panelv2/user_infopanel/{cmd}.png"
                        );
                        await target.reply("approved.");
                        return;
                    }
                    await target.reply("[panelv2]指定内容不存在，请重新检查。");
                    return;
                case 2:
                    //reject
                    if (File.Exists(@$"./work/panelv2/user_infopanel/verify/{cmd}.png"))
                    {
                        File.Delete(@$"./work/panelv2/user_infopanel/verify/{cmd}.png");
                        await target.reply("rejected.");
                        return;
                    }
                    await target.reply("[panelv2]要审核的内容不存在，请重新检查。");
                    return;
                default:
                    //do nothing
                    await target.reply("[panelv2]无效的指令，请重新检查。");
                    return;
            }
        }

        public static async Task InfoImageV2(Target target, int approve, string cmd)
        {
            switch (approve)
            {
                case -1:
                    //send list
                    var files = Directory.GetFiles(@"./work/panelv2/user_customimg/verify/");
                    if (files.Length > 0)
                    {
                        var msg = new Chain();
                        msg.msg("以下v2 info image需要审核");
                        foreach (var x in files)
                        {
                            msg.msg($"\n{Path.GetFileName(x)}\n");
                            using var stream = new MemoryStream();
                            await SixLabors.ImageSharp.Image
                                .Load(x)
                                .CloneAs<Rgba32>()
                                .SaveAsync(stream, new PngEncoder());
                            msg.image(
                                Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                                ImageSegment.Type.Base64
                            );
                        }
                        await target.reply(msg);
                        return;
                    }
                    await target.reply("[coverv2]暂时没有待审核的内容。");
                    return;
                case 0:
                    //approve all
                    var filesall = Directory.GetFiles(@"./work/panelv2/user_customimg/verify/");
                    if (filesall.Length > 0)
                    {
                        foreach (var x in filesall)
                        {
                            var destpath = @"./work/panelv2/user_customimg/" + Path.GetFileName(x);
                            if (File.Exists(destpath))
                                File.Delete(destpath);
                            File.Move(x, destpath);
                        }
                        await target.reply("approved.");
                        return;
                    }
                    await target.reply("[coverv2]暂时没有待审核的内容。");
                    return;
                case 1:
                    //approve
                    if (File.Exists(@$"./work/panelv2/user_customimg/verify/{cmd}.png"))
                    {
                        if (File.Exists(@$"./work/panelv2/user_customimg/{cmd}.png"))
                            File.Delete(@$"./work/panelv2/user_customimg/{cmd}.png");
                        File.Move(
                            @$"./work/panelv2/user_customimg/verify/{cmd}.png",
                            @$"./work/panelv2/user_customimg/{cmd}.png"
                        );
                        await target.reply("approved.");
                        return;
                    }
                    await target.reply("[coverv2]指定内容不存在，请重新检查。");
                    return;
                case 2:
                    //reject
                    if (File.Exists(@$"./work/panelv2/user_customimg/verify/{cmd}.png"))
                    {
                        File.Delete(@$"./work/panelv2/user_customimg/verify/{cmd}.png");
                        await target.reply("rejected.");
                        return;
                    }
                    await target.reply("[coverv2]要审核的内容不存在，请重新检查。");
                    return;
                default:
                    //do nothing
                    await target.reply("[coverv2]无效的指令，请重新检查。");
                    return;
            }
        }

        public static async Task InfoImageV1(Target target, int approve, string cmd)
        {
            switch (approve)
            {
                case -1:
                    //send list
                    var files = Directory.GetFiles(@"./work/legacy/v1_cover/custom/verify/");
                    if (files.Length > 0)
                    {
                        var msg = new Chain();
                        msg.msg("以下v1 info image需要审核");
                        foreach (var x in files)
                        {
                            msg.msg($"\n{Path.GetFileName(x)}\n");
                            using var stream = new MemoryStream();
                            await SixLabors.ImageSharp.Image
                                .Load(x)
                                .CloneAs<Rgba32>()
                                .SaveAsync(stream, new PngEncoder());
                            msg.image(
                                Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                                ImageSegment.Type.Base64
                            );
                        }
                        await target.reply(msg);
                        return;
                    }
                    await target.reply("[coverv1]暂时没有待审核的内容。");
                    return;
                case 0:
                    //approve all
                    var filesall = Directory.GetFiles(@"./work/legacy/v1_cover/custom/verify/");
                    if (filesall.Length > 0)
                    {
                        foreach (var x in filesall)
                        {
                            var destpath = @"./work/legacy/v1_cover/custom/" + Path.GetFileName(x);
                            if (File.Exists(destpath))
                                File.Delete(destpath);
                            File.Move(x, destpath);
                        }
                        await target.reply("approved.");
                        return;
                    }
                    await target.reply("[coverv1]暂时没有待审核的内容。");
                    return;
                case 1:
                    //approve
                    if (File.Exists(@$"./work/legacy/v1_cover/custom/verify/{cmd}.png"))
                    {
                        if (File.Exists(@$"./work/legacy/v1_cover/custom/{cmd}.png"))
                            File.Delete(@$"./work/legacy/v1_cover/custom/{cmd}.png");
                        File.Move(
                            @$"./work/legacy/v1_cover/custom/verify/{cmd}.png",
                            @$"./work/legacy/v1_cover/custom/{cmd}.png"
                        );
                        await target.reply("approved.");
                        return;
                    }
                    await target.reply("[coverv1]指定内容不存在，请重新检查。");
                    return;
                case 2:
                    //reject
                    if (File.Exists(@$"./work/legacy/v1_cover/custom/verify/{cmd}.png"))
                    {
                        File.Delete(@$"./work/legacy/v1_cover/custom/verify/{cmd}.png");
                        await target.reply("rejected.");
                        return;
                    }
                    await target.reply("[coverv1]要审核的内容不存在，请重新检查。");
                    return;
                default:
                    //do nothing
                    await target.reply("[coverv1]无效的指令，请重新检查。");
                    return;
            }
        }
    }
}
