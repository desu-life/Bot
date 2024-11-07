using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KanonBot.API;
using LanguageExt.UnsafeValueAccess;
using OSU = KanonBot.API.OSU;
using static KanonBot.API.OSU.OSUExtensions;
using KanonBot.API.PPYSB;

namespace KanonBot
{
public static class BotCmdHelper
{
    public struct BotParameter
    {
        public API.OSU.Mode? osu_mode;
        public API.PPYSB.Mode? sb_osu_mode;
        public string osu_username, osu_mods, match_name, search_arg, server;           //用于获取具体要查询的模式，未提供返回osu
        public long osu_user_id, bid;
        public int order_number;//用于info的查询n天之前、pr，bp的序号，score的bid，如未提供则返回0
        public bool lazer; //是否获取lazer数据
        public bool self_query;
        public Option<int> StartAt;
        public Option<int> EndAt;
        public string extra_args;
    }

    public enum FuncType
    {
        Info,
        BestPerformance,
        Recent,
        PassRecent,
        Score,
        Leeway,
        RoleCost,
        BPList,
        Search,
    }

    public static string? ParseString(string? input) {
        input = input?.Trim();
        if (String.IsNullOrEmpty(input)) {
            return null;
        }

        var startquote = input.IndexOf('\"');
        if (startquote >= 0 && startquote < input.Length - 1) {
            var endquote = input.LastIndexOf('\"');
            if (endquote > startquote) {
                // 找到并提取出来
                return input.Substring(startquote + 1, endquote - startquote - 1);
            }
        }

        return input;
    }

    public static (string? arg1, int? arg2) ParseArg1(string? input) {
        input = input?.Trim();
        if (String.IsNullOrEmpty(input)) {
            return (null, null);
        }

        // 先检查这边是不是数字
        Option<int> number = parseInt(input);
        string? s = null;
        
        if (number.IsNone) {
            // 先处理有引号的字符串
            var startquote = input.IndexOf('\"');
            if (startquote >= 0 && startquote < input.Length - 1) {
                var endquote = input.LastIndexOf('\"');
                if (endquote > startquote) {
                    // 找到并提取出来
                    s = input.Substring(startquote + 1, endquote - startquote - 1);

                    // 这里处理数字
                    if (startquote > 0 && number.IsNone) {
                        number = parseInt(input[..startquote]);
                    }

                    if (endquote < input.Length - 1 && number.IsNone) {
                        number = parseInt(input[(endquote + 1)..]);
                    }
                }
            }

            // 如果没找出引号，继续处理
            if (String.IsNullOrEmpty(s)) {
                string[] args = input.Split(' ');
                if (args.Length == 2) {
                    if (number.IsNone) {
                        // 第一位为数字，第二位就为字符串
                        number = parseInt(args[0]);
                        s = args[1];
                    }

                    if (number.IsNone) {
                        // 第一位不为数字，尝试解析第二位
                        number = parseInt(args[1]);
                        s = args[0];
                    }
                    
                    if (number.IsNone) {
                        // 都不为数字，全部直接取
                        s = input;
                    }
                } else {
                    // 如果只有一位或大于两位，全部直接取
                    s = input;
                }
            }
        }

        return (s, number.ToNullable());
    }

    public static BotParameter CmdParser(string? cmd, FuncType type, bool parsearg1 = true, bool parsearg2 = true, bool parsearg3 = true, bool parsearg4 = true, bool tolower = true)
    {
        cmd = cmd?.Trim();
        if (tolower) {
            cmd?.ToLower();
        }
        BotParameter param = new() { lazer = false, self_query = false };
        if (!String.IsNullOrEmpty(cmd)) {
            string arg1 = "", arg2 = "", arg3 = "", arg4 = "", arg5 = "";
            int section = 0;
            // 解析所有可能的参数
            for (var i = 0; i < cmd.Length; i++) {
                if (cmd[i] == ':' && parsearg1) {
                    section = 1;
                } else if (cmd[i] == '#' && parsearg2) {
                    section = 2;
                } else if (cmd[i] == '+' && parsearg3) {
                    section = 3;
                } else if (cmd[i] == '&' && parsearg4) {
                    section = 4;
                }

                switch (section)
                {
                    case 1:
                        arg2 += cmd[i]; // :
                        break;
                    case 2:
                        arg3 += cmd[i]; // #
                        break;
                    case 3:
                        arg4 += cmd[i]; // +
                        break;
                    case 4:
                        param.lazer = true;
                        arg5 += cmd[i]; // &
                        break;
                    default:
                        arg1 += cmd[i];
                        break;
                }
            }

            arg1 = arg1.Trim();
            arg3 = arg3.Trim();
            arg4 = arg4.Trim();

            if (!string.IsNullOrWhiteSpace(arg5)) {
                param.server = arg5[1..].Trim();
            }

            if (!string.IsNullOrWhiteSpace(arg2)) {
                try {
                    param.osu_mode = arg2[1..].Trim().ParseMode();
                } catch {
                    param.osu_mode = null;
                }

                try {
                    param.sb_osu_mode = arg2[1..].Trim().ParsePpysbMode();
                } catch {
                    param.osu_mode = null;
                }
            }

            if (type == FuncType.BPList) {
                // bplist处理
                // arg1 = username
                // arg2 = osu_mode
                // arg3 = osu_days_before_to_query
                // #1-100

                if (arg3 != "") {
                    param.osu_username = arg1;
                    var tmp = arg3[1..];
                    if (tmp.Contains('-')) {
                        var t = tmp.Split('-');
                        param.StartAt = parseInt(t[0].Trim());  // StartAt
                        param.EndAt = parseInt(t[1].Trim());  // EndAt
                    } else { //只指定了最大值
                        param.StartAt = Some(1);  // StartAt
                        param.EndAt = parseInt(tmp.Trim());  // EndAt
                    }
                } else {
                    param.self_query = true;
                    if (arg1.Contains('-')) {
                        var t = arg1.Split('-');
                        param.StartAt = parseInt(t[0].Trim());  // StartAt
                        param.EndAt = parseInt(t[1].Trim());  // EndAt
                    } else { //只指定了最大值
                        param.StartAt = Some(1);  // StartAt
                        param.EndAt = parseInt(arg1.Trim());  // EndAt
                    }
                }
            } else if (type == FuncType.Info) {
                // arg1 = username
                // arg2 = osu_mode
                // arg3 = osu_days_before_to_query
                param.osu_username = arg1;


                if (arg3 == "" || param.osu_username == null) {
                    param.order_number = 0;
                } else {
                    try {
                        param.order_number = int.Parse(arg3[1..]);
                    } catch {
                        param.order_number = 0;
                    }
                }

                if (String.IsNullOrEmpty(param.osu_username)) {
                    param.self_query = true;
                }
            } else if (type == FuncType.BestPerformance) {
                // bp
                // arg1 = username / order_number
                // arg2 = osu_mode
                // arg3 = order_number (序号)

                // order_number 解析成功
                if (arg3 != "" && int.TryParse(arg3[1..], out param.order_number)) {
                    param.osu_username = ParseString(arg1) ?? String.Empty;
                } else {
                    // arg1的处理
                    var tmp = ParseArg1(arg1);
                    param.osu_username = tmp.arg1 ?? String.Empty;
                    param.order_number = tmp.arg2 ?? -1;
                }

                if (String.IsNullOrEmpty(param.osu_username)) {
                    param.self_query = true;
                }


            } else if (type == FuncType.Recent || type == FuncType.PassRecent) {
                // 处理pr/re解析
                // arg1 = username
                // arg2 = osu_mode
                // arg3 = order_number (序号)
                param.osu_username = arg1;

                // 成绩必须为1
                if (arg3 == "" || param.osu_username == null) {
                    param.order_number = 1;
                } else {
                    try {
                        var t = param.order_number = int.Parse(arg3[1..]);
                        if (t > 100 || t < 1) {
                            param.order_number = 1;
                        }
                    } catch {
                        param.order_number = 0;
                    }
                }

                if (String.IsNullOrEmpty(param.osu_username)) {
                    param.self_query = true;
                }
            } else if (type == FuncType.Score) {
                // 处理score解析
                // arg1 = username
                // arg2 = osu_mode :
                // arg3 = bid #
                // arg4 = mods +

                // bid 解析成功
                if (arg3 != "" && int.TryParse(arg3[1..], out param.order_number)) {
                    param.osu_username = ParseString(arg1) ?? String.Empty;
                } else {
                    // arg1的处理
                    var tmp = ParseArg1(arg1);
                    param.osu_username = tmp.arg1 ?? String.Empty;
                    param.order_number = tmp.arg2 ?? -1;
                }

                if (String.IsNullOrEmpty(param.osu_username)) {
                    param.self_query = true;
                }

                param.osu_mods = arg4 != "" ? arg4[1..] : "";
            } else if (type == FuncType.Search) {
                // 处理score解析
                // arg1 = search arg
                // arg3 = num #
                // arg4 = mods +

                // bid 解析成功
                if (arg3 != "" && int.TryParse(arg3[1..], out param.order_number)) {
                    param.search_arg = ParseString(input: arg1) ?? String.Empty;
                } else {
                    // arg1的处理
                    param.search_arg = ParseString(input: arg1) ?? String.Empty;
                }

                param.osu_mods = arg4 != "" ? arg4[1..] : "";
            } else if (type == FuncType.Leeway) {
                // arg1 = bid
                // arg2 = osu_mode
                // arg3 =
                // arg4 = mods

                // 若bid为空，返回0
                if (arg1 == "" || param.osu_username == null) {
                    param.order_number = 0;
                } else {
                    try {
                        var index = int.Parse(arg1);
                        param.order_number = index < 1 ? -1 : index;
                    } catch {
                        param.order_number = 0;
                    }
                }

                param.osu_mods = arg4 != "" ? arg4[1..] : "";
                param.self_query = true; // 只查自己
            } else if (type == FuncType.RoleCost) {
                // arg1 = match name
                // arg3 = username #
                if (arg3 != "") {
                    param.osu_username = arg3[1..];
                }

                param.match_name = arg1;
                
                if (String.IsNullOrEmpty(param.osu_username)) {
                    param.self_query = true;
                }
            }
        } else {
            param.self_query = true;
            if (type == FuncType.Info) {
                param.order_number = 0; //由于cmd为空，所以默认查询当日信息
            } else if (type == FuncType.Score) {
                param.order_number = -1; //由于cmd为空，所以没有bid等关键信息，返回错误
            } else if (type == FuncType.Recent || type == FuncType.PassRecent || type == FuncType.BestPerformance) {
                param.order_number = 1; //由于cmd为空，所以默认返回第一张谱面成绩
            }
        }
        return param;
    }
}
}
