using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using FuzzySharp;
using KanonBot.API.OSU;

namespace KanonBot;

public static partial class Utils
{
    public static int TryGetConsoleWidth()
    {
        try
        {
            return Console.WindowWidth;
        }
        catch
        {
            return 80;
        }
    } // 获取失败返回80

    public static IEnumerable<Models.ScoreLazer>? FilterMods(IEnumerable<Models.ScoreLazer>? scores, IEnumerable<string> mods) {
        if (mods.Contains("NM")) {
                return scores?.Filter(x => (x.Mods.Length == 1 && x.Mods[0].IsClassic) || x.Mods.Length == 0);
        } else {
            if (mods.Contains("CL")) {
                return scores?
                    .Filter(x =>{
                        var scoreMods = x.Mods.Map(m => m.Acronym).Order();
                        var requiredMods = mods.Order();
                        return requiredMods.SequenceEqual(scoreMods);
                    });
            } else {
                return scores?
                    .Filter(x => {
                        var scoreMods = x.Mods.Filter(m => !m.IsClassic).Map(m => m.Acronym).Order();
                        var requiredMods = mods.Order();
                        return requiredMods.SequenceEqual(scoreMods);
                    });
            }
        }
    }

    public static async Task<Option<T>> TimeOut<T>(this Task<T> task, TimeSpan delay)
    {
        var timeOutTask = Task.Delay(delay); // 设定超时任务
        var doing = await Task.WhenAny(task, timeOutTask); // 返回任何一个完成的任务
        if (doing == timeOutTask) // 如果超时任务先完成了 就返回none
            return None;
        return Some<T>(await task);
    }

    public static Option<(String, String)> SplitKvp(String msg)
    {
        if (msg.Filter((c) => c == '=').Count() == 1)
        {
            var p = msg.Split('=');

            var (k, v) = (p[0], p[1]);
            if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(v))
                return None;
            return Some((k, v));
        }
        return None;
    }

    public static List<T> Slice<T>(this List<T> myList, int startIndex, int endIndex)
    {
        return myList.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
    }

    public static Stream Byte2Stream(byte[] buffer)
    {
        var stream = new MemoryStream(buffer);
        //设置 stream 的 position 为流的开始
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    public static string Dict2String(Dictionary<String, Object> dict)
    {
        var lines = dict.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());
        return string.Join(Environment.NewLine, lines);
    }

    public static string KOOKUnEscape(string str)
    {
        str = str.Replace("\\\\n", "\\n");
        str = str.Replace("\\(", "(");
        str = str.Replace("\\)", ")");
        return str;
    }

    public static string KOOKEscape(string str)
    {
        str = str.Replace("\\n", "\\\\n");
        str = str.Replace("(", "\\(");
        str = str.Replace(")", "\\)");
        return str;
    }

    public static string GuildUnEscape(string str)
    {
        str = str.Replace("&amp;", "&");
        str = str.Replace("&lt;", "<");
        str = str.Replace("&gt;", ">");
        return str;
    }

    public static string GuildEscape(string str)
    {
        str = str.Replace("&", "&amp;");
        str = str.Replace("<", "&lt;");
        str = str.Replace(">", "&gt;");
        return str;
    }

    public static string CQUnEscape(string str)
    {
        str = str.Replace("&amp;", "&");
        str = str.Replace("&#91;", "[");
        str = str.Replace("&#93;", "]");
        return str;
    }

    public static string CQEscape(string str)
    {
        str = str.Replace("&", "&amp;");
        str = str.Replace("[", "&#91;");
        str = str.Replace("]", "&#93;");
        return str;
    }

    // 全角转半角
    public static string ToDBC(this string input)
    {
        char[] c = input.ToCharArray();
        for (int i = 0; i < c.Length; i++)
        {
            if (c[i] == 12288)
            {
                c[i] = (char)32;
                continue;
            }
            if (c[i] > 65280 && c[i] < 65375)
                c[i] = (char)(c[i] - 65248);
        }
        return new String(c);
    }

    [GeneratedRegex(@"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$")]
    private static partial Regex UrlRegex();

    public static bool IsUrl(string str)
    {
        try
        {
            return UrlRegex().IsMatch(str);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string NewYearMessage()
    {
        Random r = new();
        int i = r.Next(1, 26);
        switch (i)
        {
            case 1:
                return "新的一年要保持一个好心态，去年的留下的遗憾，要在今年努力争取改变！";
            case 2:
                return "(  )";
            case 3:
                return "新的一年要特别注意健康状况喔！";
            case 4:
                return "遗憾的事情假如已无法改变，那就勇敢地接受，不要后悔，要继续前进才是！";
            case 5:
                return "新的一年也要去勇敢地追求梦想呢~";
            case 6:
                return "遗憾不是尽了全力没有成功，而是可能成功却没尽全力。新的一年调整好自己，";
            case 7:
                return "把2021的失意揉碎。2022年好好生活，慢慢相遇，保持热爱。";
            case 8:
                return "愿岁并谢，与长友兮。";
            case 9:
                return "今年你没有压岁钱！";
            case 10:
                return "用一句话告别2021，你会说些什么呢？（    ）";
            case 11:
                return "さよならがあんたに捧ぐ愛の言葉 ——さよならべいべ(藤井风)";
            case 12:
                return "You're just too good to be true, Can't take my eyes off of you.";
            case 13:
                return "希望你的未来一片光明≥v≤";
            case 14:
                return "或许大家不再和过去认识的一些朋友联系了，但还是会记得那一段美好的时光不是吗？";
            case 15:
                return "啊就是不听话！！！就是想放假！！！";
            case 16:
                return "没太多计划，不知道要去哪，那就这样一直向前，什么都不管啦！";
            case 17:
                return "唱一首心爱的人喜欢的歌曲给自己听好吗";
            case 18:
                return "给心爱的人唱一首自己喜欢的歌吧！";
            case 19:
                return "没有说得出口的话，没有做得出来的事，或是最终没有争取到的人，那都没有关系。与其痛苦，不如坦然接受，继续向前。";
            case 20:
                return "Darling darling 今晚 一定要喝 只要 有你在 就够了 继续 反覆着 那痛苦快乐 不完美 的人生 才动人 —— 八三夭";
            case 21:
                return "在过去的一年里，最打动你的一件事是（   ）";
            case 22:
                return "朋友之间的抱歉，如果可以好好说出来的话，如今也不会像个傻瓜一样充满后悔与遗憾了吧";
            case 23:
                return "愿你不卑不亢不自叹，一生热爱不遗憾。";
            case 24:
                return "今年，对自己温柔一些。好吗？";
            case 25:
                return "对2021年的自己说一声辛苦了！";
            case 26:
                return "答应我，不要再玩OSU了！TAT";
            default:
                break;
        }
        return "";
    }

    // 计算 Levenshtein 距离
    public static int CalculateLevenshteinDistance(string s1, string s2)
    {
        int n = s1.Length;
        int m = s2.Length;
        int[,] dp = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++)
            dp[i, 0] = i;
        for (int j = 0; j <= m; j++)
            dp[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[n, m];
    }

    // 计算相似度
    public static double CalculateSimilarity(string s1, string s2, int distance)
    {
        int maxLength = Math.Max(s1.Length, s2.Length);
        return maxLength == 0 ? 1.0 : 1.0 - (double)distance / maxLength;
    }

    // 泛型方法：返回最接近匹配的索引
    public static int FindClosestMatchIndex<T>(string target, IEnumerable<T> collection, Func<T, string> selector)
    {
        int bestIndex = -1;
        int highestScore = int.MinValue;
        int currentIndex = 0;

        foreach (var item in collection)
        {
            string value = selector(item); // 使用闭包选择字段
            int score = Fuzz.PartialRatio(target, value); // 计算相似度分数

            if (score > highestScore)
            {
                highestScore = score;
                bestIndex = currentIndex;
            }

            currentIndex++;
        }


        return bestIndex;
    }

}
