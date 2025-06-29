using KanonBot.API;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading.Tasks;
using static KanonBot.Database.Model;
using CronNET.Impl;
using System.IO;
using static KanonBot.API.OSU.OSUExtensions;
using DotNext.Collections.Generic;

namespace KanonBot.Functions.OSU
{
    public static class GeneralUpdate
    {
        private static readonly CronDaemon daemon = new();
        public static void DailyUpdate()
        {
            // *    *    *    *    *
            // ┬    ┬    ┬    ┬    ┬
            // │    │    │    │    │
            // │    │    │    │    │
            // │    │    │    │    └───── day of week (0 - 6) (Sunday=0 )
            // │    │    │    └────────── month (1 - 12)
            // │    │    └─────────────── day of month (1 - 31)
            // │    └──────────────────── hour (0 - 23)
            // └───────────────────────── min (0 - 59)
            // `* * * * *`        Every minute.
            // `0 * * * *`        Top of every hour.
            // `0,1,2 * * * *`    Every hour at minutes 0, 1, and 2.
            // `*/2 * * * *`      Every two minutes.
            // `1-55 * * * *`     Every minute through the 55th minute.
            // `* 1,10,20 * * *`  Every 1st, 10th, and 20th hours.

            daemon.Add(new CronJob(async () =>
            {
                Log.Information("开始每日用户数据更新");
                var (count, span) = await UpdateUsers();
                Log.Information("更新完毕，总花费时间 {0}s", span.TotalSeconds);
                Log.Information("启动检查徽章有效期任务");
                await OSUBot.Badge.CheckBadgeIsVaildJob();
                Log.Information("检查徽章有效期任务完成");
                // Environment.Exit(0);
            }, "DailyUpdate", "0 4 * * *"));   // 每天早上4点运行的意思，具体参考https://crontab.cronhub.io/
            daemon.Start(CancellationToken.None);
        }



        async public static Task<(long, TimeSpan)> UpdateUsers()
        {
            var stopwatch = Stopwatch.StartNew();
            var userList = await Database.Client.GetOsuUserList();

            await Parallel.ForEachAsync(userList, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (userID, _) => {
                try
                {
                    await UpdateUser(userID, false);
                }
                catch (System.Exception e)
                {
                    Log.Warning("更新用户信息时出错，ex: {@0}", e);
                }
            });
            
            stopwatch.Stop();

            //删除头像以及osu!web缓存
            Directory.GetFiles($"./work/avatar/").ForEach(file => {
                try { File.Delete(file); } catch { }
            });
            
            Directory.GetFiles($"./work/legacy/v1_cover/osu!web/").ForEach(file => {
                try { File.Delete(file); } catch { }
            });
            
            return (userList.Count, stopwatch.Elapsed);
        }

        static readonly IReadOnlyList<API.OSU.Mode> modes = [API.OSU.Mode.OSU, API.OSU.Mode.Taiko, API.OSU.Mode.Fruits, API.OSU.Mode.Mania];
        async public static Task UpdateUser(long userID, bool is_newuser)
        {
            foreach (var mode in modes)
            {
                Log.Information($"正在更新用户数据....[{userID}/{mode}]");
                var userInfo = await API.OSU.Client.GetUser(userID, mode);
                if (userInfo == null) continue;
                if (userInfo.Statistics.PP == 0) continue;
                OsuArchivedRec rec = new()
                {
                    uid = userInfo!.Id,
                    play_count = (int)userInfo.Statistics.PlayCount,
                    ranked_score = userInfo.Statistics.RankedScore,
                    total_score = userInfo.Statistics.TotalScore,
                    total_hit = userInfo.Statistics.TotalHits,
                    level = userInfo.Statistics.Level.Current,
                    level_percent = userInfo.Statistics.Level.Progress,
                    performance_point = userInfo.Statistics.PP,
                    accuracy = userInfo.Statistics.HitAccuracy,
                    count_SSH = userInfo.Statistics.GradeCounts.SSH,
                    count_SS = userInfo.Statistics.GradeCounts.SS,
                    count_SH = userInfo.Statistics.GradeCounts.SH,
                    count_S = userInfo.Statistics.GradeCounts.S,
                    count_A = userInfo.Statistics.GradeCounts.A,
                    playtime = (int)userInfo.Statistics.PlayTime,
                    country_rank = (int)userInfo.Statistics.CountryRank,
                    global_rank = (int)userInfo.Statistics.GlobalRank,
                    gamemode = mode.ToStr()
                };
                await Database.Client.InsertOsuUserData(rec, false);
            }
        }
    }
}
