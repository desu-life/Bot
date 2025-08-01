#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable IDE0044 // 添加只读修饰符
using System;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using KanonBot.API.OSU;
using KanonBot.Drivers;
using KanonBot.Functions.OSUBot;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using MySqlConnector;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Ocsp;
using RosuPP;
using Serilog;
using static KanonBot.API.OSU.Models;
using static KanonBot.Database.Model;
using static LinqToDB.Reflection.Methods.LinqToDB.Insert;

namespace KanonBot.Database;

public class Client
{
    private static Config.Base config = Config.inner!;

    public static DB GetInstance()
    {
        var options = new DataOptions().UseMySqlConnector(
            new MySqlConnectionStringBuilder
            {
                Server = config.database.host,
                Port = (uint)config.database.port,
                UserID = config.database.user,
                Password = config.database.password,
                Database = config.database.db,
                CharacterSet = "utf8mb4",
                CancellationTimeout = 5,
                DefaultCommandTimeout = 30
            }.ConnectionString
        );
        // 暂时只有Mysql
        return new DB(options);
    }

    public static async Task<bool> SetVerifyMail(string mailAddr, string verify)
    {
        using var db = GetInstance();
        var newverify = new Model.MailVerify()
        {
            mailAddr = mailAddr,
            verify = verify,
            gen_time = Utils.GetTimeStamp(false)
        };

        try
        {
            await db.InsertAsync(newverify);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> IsRegd(string mailAddr)
    {
        using var db = GetInstance();
        var li = db.User.Where(it => it.email == mailAddr).Select(it => it.uid);
        if (await li.CountAsync() > 0)
            return true;
        return false;
    }

    public static async Task<Model.User?> GetUser(string mailAddr)
    {
        using var db = GetInstance();
        return await db.User.Where(it => it.email == mailAddr).FirstOrDefaultAsync();
    }

    public static async Task<Model.User?> GetUser(int uid)
    {
        using var db = GetInstance();
        return await db.User.Where(it => it.uid == uid).FirstOrDefaultAsync();
    }

    public static async Task<Model.User?> GetUsersByUID(string UID, Platform platform)
    {
        using var db = GetInstance();
        switch (platform)
        {
            case Platform.OneBot:
                if (long.TryParse(UID, out var qid))
                    return await db.User.Where(it => it.qq_id == qid).FirstOrDefaultAsync();
                else
                    return null;
            case Platform.Guild:
                return await db.User.Where(it => it.qq_guild_uid == UID).FirstOrDefaultAsync();
            case Platform.KOOK:
                return await db.User.Where(it => it.kook_uid == UID).FirstOrDefaultAsync();
            case Platform.Discord:
                return await db.User.Where(it => it.discord_uid == UID).FirstOrDefaultAsync();
            default:
                return null;
        }
    }

    public static async Task<Model.User?> GetUserByOsuUID(long osu_uid)
    {
        using var db = GetInstance();
        var user = await GetOsuUser(osu_uid);
        if (user == null)
        {
            return null;
        }
        return await db.User.Where(it => it.uid == user.uid).FirstOrDefaultAsync();
    }

    public static async Task<Model.User?> GetUserByPpysbUID(long osu_uid)
    {
        using var db = GetInstance();
        var user = await GetPpysbUser(osu_uid);
        if (user == null)
        {
            return null;
        }
        return await db.User.Where(it => it.uid == user.uid).FirstOrDefaultAsync();
    }

    public static async Task<Model.UserPPYSB?> GetPpysbUser(long osu_uid)
    {
        using var db = GetInstance();
        return await db.UserPPYSB.Where(it => it.osu_uid == osu_uid).FirstOrDefaultAsync();
    }

    public static async Task<Model.UserPPYSB?> GetPpysbUserByUID(long kanon_uid)
    {
        using var db = GetInstance();
        return await db.UserPPYSB.Where(it => it.uid == kanon_uid).FirstOrDefaultAsync();
    }

    public static async Task<Model.UserOSU?> GetOsuUser(long osu_uid)
    {
        using var db = GetInstance();
        return await db.UserOSU.Where(it => it.osu_uid == osu_uid).FirstOrDefaultAsync();
    }

    public static async Task<Model.UserOSU?> GetOsuUserByUID(long kanon_uid)
    {
        using var db = GetInstance();
        return await db.UserOSU.Where(it => it.uid == kanon_uid).FirstOrDefaultAsync();
    }

    public static async Task<bool> InsertPpysbUser(
        long kanon_uid,
        long osu_uid
    )
    {
        using var db = GetInstance();
        var d = new Model.UserPPYSB()
        {
            uid = kanon_uid,
            osu_uid = osu_uid,
            mode = 0,
        };
        var result = await db.InsertOrReplaceAsync(d);
        return result > 0;
    }

    public static async Task<bool> InsertOsuUser(
        long kanon_uid,
        long osu_uid
    )
    {
        using var db = GetInstance();
        var d = new Model.UserOSU()
        {
            uid = kanon_uid,
            osu_uid = osu_uid,
            osu_mode = "osu",
            customInfoEngineVer = 2,
            InfoPanelV2_Mode = 1
        };
        var result = await db.InsertAsync(d);
        return result > 0;
    }

    public static async Task<API.OSU.Models.PPlusData.UserData?> GetOsuPPlusData(long osu_uid)
    {
        using var db = GetInstance();
        var data = await db.OsuPPlus.FirstOrDefaultAsync(it => it.uid == osu_uid && it.pp != 0);
        if (data != null)
        {
            var realData = new API.OSU.Models.PPlusData.UserData
            {
                UserId = osu_uid,
                PerformanceTotal = data.pp,
                AccuracyTotal = data.acc,
                FlowAimTotal = data.flow,
                JumpAimTotal = data.jump,
                PrecisionTotal = data.pre,
                SpeedTotal = data.spd,
                StaminaTotal = data.sta
            };
            return realData;
        }
        else
        {
            return null;
        }
    }
    
    public static async Task<API.OSU.Models.PPlusData.UserDataNext?> GetOsuPPlusDataNext(long osu_uid)
    {
        using var db = GetInstance();
        var data = await db.OsuPPlus.FirstOrDefaultAsync(it => it.uid == osu_uid && it.pp != 0);
        if (data != null)
        {
            var realData = new API.OSU.Models.PPlusData.UserDataNext
            {
                Id = data.uid,
                Performances = new API.OSU.Models.PPlusData.UserPerformancesNext
                {
                    PerformanceTotal = data.pp,
                    AccuracyTotal = data.acc,
                    FlowAimTotal = data.flow,
                    JumpAimTotal = data.jump,
                    PrecisionTotal = data.pre,
                    SpeedTotal = data.spd,
                    StaminaTotal = data.sta
                }
            };
            return realData;
        }
        else
        {
            return null;
        }
    }

     public static async Task<bool> UpdateOsuPPlusDataNext(
        API.OSU.Models.PPlusData.UserDataNext ppdata
    )
    {
        using var db = GetInstance();
        var data = await db.OsuPPlus.FirstOrDefaultAsync(it => it.uid == ppdata.Id);
        var result = await db.InsertOrReplaceAsync(
            new Model.OsuPPlus()
            {
                uid = ppdata.Id,
                pp = ppdata.Performances.PerformanceTotal,
                acc = ppdata.Performances.AccuracyTotal,
                flow = ppdata.Performances.FlowAimTotal,
                jump = ppdata.Performances.JumpAimTotal,
                pre = ppdata.Performances.PrecisionTotal,
                spd = ppdata.Performances.SpeedTotal,
                sta = ppdata.Performances.StaminaTotal
            }
        );
        return result > 0;
    }

    public static async Task<bool> UpdateOsuPPlusData(
        API.OSU.Models.PPlusData.UserData ppdata,
        long osu_uid
    )
    {
        using var db = GetInstance();
        var data = await db.OsuPPlus.FirstOrDefaultAsync(it => it.uid == osu_uid);
        var result = await db.InsertOrReplaceAsync(
            new Model.OsuPPlus()
            {
                uid = osu_uid,
                pp = ppdata.PerformanceTotal,
                acc = ppdata.AccuracyTotal,
                flow = ppdata.FlowAimTotal,
                jump = ppdata.JumpAimTotal,
                pre = ppdata.PrecisionTotal,
                spd = ppdata.SpeedTotal,
                sta = ppdata.StaminaTotal
            }
        );
        return result > 0;
    }

    public static async Task<bool> SetDisplayedBadge(string userid, string displayed_ids)
    {
        using var db = GetInstance();
        var data = await db.User.FirstOrDefaultAsync(it => it.uid == long.Parse(userid));
        var res = await db.User
            .Where(it => it.uid == long.Parse(userid))
            .Set(it => it.displayed_badge_ids, displayed_ids)
            .UpdateAsync();

        return res > 0;
    }

    public static async Task<Model.BadgeList?> GetBadgeInfo(string badgeid)
    {
        using var db = GetInstance();
        return await db.BadgeList.Where(it => it.id == int.Parse(badgeid)).FirstOrDefaultAsync();
    }

    public static async Task<bool> SetOwnedBadge(string email, string? owned_ids)
    {
        using var db = GetInstance();
        var data = await db.User.FirstOrDefaultAsync(it => it.email == email);
        data.owned_badge_ids = owned_ids;
        var res = await db.UpdateAsync(data);
        return res > 0;
    }

    public static async Task<bool> SetOwnedBadge(int uid, string? owned_ids)
    {
        using var db = GetInstance();
        var data = await db.User.FirstOrDefaultAsync(it => it.uid == uid);
        data.owned_badge_ids = owned_ids;
        var res = await db.UpdateAsync(data);
        return res > 0;
    }

    public static async Task<bool> SetOwnedBadgeByOsuUid(string osu_uid, string? owned_ids)
    {
        var user = await GetOsuUser(long.Parse(osu_uid));
        if (user == null)
        {
            return false;
        }
        using var db = GetInstance();
        var userinfo = await db.User.Where(it => it.uid == user.uid).FirstOrDefaultAsync();
        userinfo.owned_badge_ids = owned_ids;
        var res = await db.UpdateAsync(userinfo);
        return res > 0;
    }

    public static async Task<List<long>> GetOsuUserList()
    {
        using var db = GetInstance();
        return await db.UserOSU.Select(it => it.osu_uid).ToListAsync();
    }

    public static async Task<int> InsertOsuUserData(OsuArchivedRec rec, bool is_newuser)
    {
        using var db = GetInstance();
        rec.lastupdate = is_newuser ? DateTime.Today.AddDays(-1) : DateTime.Today;
        return await db.InsertOrReplaceAsync(rec);
    }

    public static async Task<bool> SetOsuUserMode(long osu_uid, API.OSU.Mode mode)
    {
        using var db = GetInstance();
        var result = await db.UserOSU
            .Where(it => it.osu_uid == osu_uid)
            .Set(it => it.osu_mode, mode.ToStr())
            .UpdateAsync();
        return result > 0;
    }

    public static async Task<bool> SetPpysbUserMode(long osu_uid, API.PPYSB.Mode mode)
    {
        using var db = GetInstance();
        var result = await db.UserPPYSB
            .Where(it => it.osu_uid == osu_uid)
            .Set(it => it.mode, mode.ToNum())
            .UpdateAsync();
        return result > 0;
    }

    //返回值为天数（几天前）
    public static async Task<(int, API.OSU.Models.UserExtended?)> GetOsuUserData(
        long oid,
        API.OSU.Mode mode,
        int days = 0
    )
    {
        OsuArchivedRec? data;
        using var db = GetInstance();
        var ui = new API.OSU.Models.UserExtended();
        if (days <= 0)
        {
            var q =
                from p in db.OsuArchivedRec
                where p.uid == oid && p.gamemode == mode.ToStr()
                orderby p.lastupdate descending
                select p;
            
            data = await q.FirstOrDefaultAsync();
        }
        else
        {
            var date = DateTime.Today;
            try
            {
                date = date.AddDays(-days);
            }
            catch (ArgumentOutOfRangeException)
            {
                return (-1, null);
            }
            var q =
                from p in db.OsuArchivedRec
                where
                    p.uid == oid
                    && p.gamemode == mode.ToStr()
                    && p.lastupdate <= date
                orderby p.lastupdate descending
                select p;
            data = await q.FirstOrDefaultAsync();
            if (data == null)
            {
                var tq =
                    from p in db.OsuArchivedRec
                    where p.uid == oid && p.gamemode == mode.ToStr()
                    orderby p.lastupdate
                    select p;
                data = await tq.FirstOrDefaultAsync();
            }
        }
        if (data == null)
            return (-1, null);

        ui.StatisticsCurrent = new() { GradeCounts = new(), Level = new() };
        ui.Id = oid;
        ui.Statistics.TotalScore = data.total_score;
        ui.Statistics.TotalHits = data.total_hit;
        ui.Statistics.PlayCount = data.play_count;
        ui.Statistics.RankedScore = data.ranked_score;
        ui.Statistics.CountryRank = data.country_rank;
        ui.Statistics.GlobalRank = data.global_rank;
        ui.Statistics.HitAccuracy = data.accuracy;
        ui.Statistics.GradeCounts.SSH = data.count_SSH;
        ui.Statistics.GradeCounts.SS = data.count_SS;
        ui.Statistics.GradeCounts.SH = data.count_SH;
        ui.Statistics.GradeCounts.S = data.count_S;
        ui.Statistics.GradeCounts.A = data.count_A;
        ui.Statistics.Level.Current = data.level;
        ui.Statistics.Level.Progress = data.level_percent;
        ui.Statistics.PP = data.performance_point;
        ui.Mode = mode;
        ui.Statistics.PlayTime = data.playtime;
        //ui.daysBefore = (t - data.lastupdate).Days;
        return ((DateTime.Today - data.lastupdate).Days, ui);
    }

    //return badge_id
    public static async Task<int> InsertBadge(
        string ENG_NAME,
        string CHN_NAME,
        string CHN_DECS,
        DateTimeOffset expire_at
    )
    {
        using var db = GetInstance();
        BadgeList bl =
            new()
            {
                name = ENG_NAME,
                name_chinese = CHN_NAME,
                description = CHN_DECS,
                expire_at = expire_at
            };
        return await db.InsertWithInt32IdentityAsync(bl);
    }

    public static async Task<bool> UpdateSeasonalPass(long oid, string mode, int add_point)
    {
        //检查数据库中有无信息
        using var db = GetInstance();
        var db_info = db.OSUSeasonalPass.Where(it => it.osu_id == oid).Where(it => it.mode == mode);
        if (await db_info.CountAsync() > 0)
        {
            return await db.OSUSeasonalPass
                    .Where(it => it.osu_id == oid && it.mode == mode)
                    .Set(it => it.point, it => it.point + add_point)
                    .UpdateAsync() > 0;
        }
        var res = await db.InsertAsync(
            new OSUSeasonalPass()
            {
                point = add_point,
                mode = mode,
                osu_id = oid
            }
        );
        return res > 0;
    }

    public static async Task<bool> SetOsuInfoPanelVersion(long osu_uid, int ver)
    {
        using var db = GetInstance();
        var result = await db.UserOSU
            .Where(it => it.osu_uid == osu_uid)
            .Set(it => it.customInfoEngineVer, ver)
            .UpdateAsync();
        return result > 0;
    }

    public static async Task<bool> SetOsuInfoPanelV2ColorMode(long osu_uid, int ver)
    {
        using var db = GetInstance();
        var result = await db.UserOSU
            .Where(it => it.osu_uid == osu_uid)
            .Set(it => it.InfoPanelV2_Mode, ver)
            .UpdateAsync();
        return result > 0;
    }

    public static async Task<bool> UpdateInfoPanelV2CustomCmd(long osu_uid, string cmd)
    {
        using var db = GetInstance();
        var result = await db.UserOSU
            .Where(it => it.osu_uid == osu_uid)
            .Set(it => it.InfoPanelV2_CustomMode, cmd)
            .UpdateAsync();
        return result > 0;
    }

    public static async Task<bool> SetOsuUserPermissionByOid(long osu_uid, string permission)
    {
        var DBUser = await GetUserByOsuUID(osu_uid);
        using var db = GetInstance();
        var result = await db.User
            .Where(it => it.uid == DBUser.uid)
            .Set(it => it.permissions, permission)
            .UpdateAsync();
        return result > 0;
    }

    public static async Task<bool> SetOsuUserPermissionByEmail(string email, string permission)
    {
        using var db = GetInstance();
        var result = await db.User
            .Where(it => it.email == email)
            .Set(it => it.permissions, permission)
            .UpdateAsync();
        return result > 0;
    }

    public static async Task<bool> InsertOsuStandardBeatmapTechData(
        long bid,
        double stars,
        int total,
        int acc,
        int speed,
        int aim,
        int a99,
        int a98,
        int a97,
        int a95,
        string[] mods
    )
    {
        using var db = GetInstance();
        var modstring = "";
        if (mods.Length > 0)
        {
            foreach (var x in mods)
            {
                if (x == "NC")
                {
                    modstring += "DT,";
                }
                else
                {
                    modstring += x + ",";
                }
                if (x == "PF" || x == "SD" || x == "AP" || x == "RX" || x.ToLower() == "v2")
                    return true; //不保存以上mod
            }
            modstring = modstring[..^1];
        }

        //查找谱面对应的mod数据是否存在
        var db_info = db.OsuStandardBeatmapTechData
            .Where(it => it.bid == bid)
            .Where(it => it.mod == modstring);
        if (await db_info.CountAsync() == 0)
        {
            //不存在再执行添加
            OsuStandardBeatmapTechData t =
                new()
                {
                    bid = bid,
                    stars = stars,
                    total = total,
                    acc = acc,
                    speed = speed,
                    aim = aim,
                    mod = modstring,
                    pp_95acc = a95,
                    pp_97acc = a97,
                    pp_98acc = a98,
                    pp_99acc = a99,
                };
            var result = await db.InsertAsync(t);
            return result > 0;
        }
        else
        {
            return true;
        }
    }

    public static async Task<OSUSeasonalPass?> GetSeasonalPassInfo(long oid, string mode)
    {
        using var db = GetInstance();
        return await db.OSUSeasonalPass
            .Where(it => it.osu_id == oid)
            .Where(it => it.mode == mode)
            .FirstOrDefaultAsync();
    }

    //true=数据库不存在，已成功插入数据，可以进行pt计算
    public static async Task<bool> SeasonalPass_Query_Score_Status(string mode, long score_id)
    {
        using var db = GetInstance();
        var li = db.OSUSeasonalPass_ScoreRecords
            .Where(it => it.score_id == score_id && it.mode == mode)
            .Select(it => it.score_id);
        if (await li.CountAsync() > 0)
            return false;

        //insert
        var d = new OSUSeasonalPass_ScoreRecords() { score_id = score_id, mode = mode };
        var result = await db.InsertAsync(d);
        return result > 0;
    }

    public static async Task<List<OsuStandardBeatmapTechData>> GetOsuStandardBeatmapTechData(
        int aim,
        int speed,
        int acc,
        int range = 20,
        bool boost = false
    )
    {
        using var db = GetInstance();
        var trange = range;
        if (boost)
            trange = 50;
        return await db.OsuStandardBeatmapTechData
            .Where(
                it =>
                    it.aim > aim - range / 2
                    && it.aim < aim + trange
                    && it.speed > speed - range / 2
                    && it.speed < speed + trange
                    && it.acc > acc - range / 2
                    && it.acc < acc + trange
            )
            .ToListAsync();
    }

    //true=成功生成代码
    public static async Task<bool> CreateBadgeRedemptionCode(
        int badge_id,
        string code,
        bool can_repeatedly,
        DateTimeOffset expire_at,
        int badge_expire_days
    )
    {
        using var db = GetInstance();
        var li = db.BadgeRedemptionCode.Where(it => it.code == code).Select(it => it.id);
        if (await li.CountAsync() > 0)
            return false;

        //insert
        var d = new BadgeRedemptionCode()
        {
            badge_id = badge_id,
            gen_time = DateTime.Now,
            code = code,
            can_repeatedly = can_repeatedly,
            expire_at = expire_at,
            badge_expiration_day = badge_expire_days
        };
        var result = await db.InsertAsync(d);
        return result > 0;
    }

    public static async Task<BadgeRedemptionCode> RedeemBadgeRedemptionCode(long uid, string code)
    {
        using var db = GetInstance();
        var li = await db.BadgeRedemptionCode.Where(it => it.code == code).FirstOrDefaultAsync();

        if (li == null)
            return null!;
        if (!li.can_repeatedly)
            if (li.redeem_count > 0)
                return null!;

        return li!;
    }

    public static async Task<bool> SetBadgeRedemptionCodeStatus(int id, long uid, string code)
    {
        using var db = GetInstance();
        var result = await db.BadgeRedemptionCode
            .Where(it => it.id == id)
            .Set(it => it.redeem_time, DateTime.Now)
            .Set(
                it => it.redeem_user,
                it =>
                    it.redeem_user == null ? uid.ToString() : it.redeem_user + "," + uid.ToString()
            )
            .Set(it => it.redeem_count, it => it.redeem_count + 1)
            .UpdateAsync();
        if (result > 0)
            return true;
        return false;
    }

    public static async Task<BadgeExpirationDateRec?> GetBadgeExpirationTime(
        int userid,
        int badgeid
    )
    {
        using var db = GetInstance();
        return await db.BadgeExpirationDateRec
            .Where(it => it.uid == userid && it.badge_id == badgeid)
            .FirstOrDefaultAsync();
    }

    public static async Task<List<BadgeExpirationDateRec>?> GetAllBadgeExpirationTime()
    {
        using var db = GetInstance();
        return await db.BadgeExpirationDateRec.ToListAsync();
    }

    public static async Task<bool> UpdateBadgeExpirationTime(
        int userid,
        int badgeid,
        int daysneedtobeadded
    )
    {
        using var db = GetInstance();
        var result = await db.BadgeExpirationDateRec
            .Where(it => it.uid == userid && it.badge_id == badgeid)
            .FirstOrDefaultAsync();
        if (result == null)
        {
            try
            {
                BadgeExpirationDateRec bed =
                    new()
                    {
                        badge_id = badgeid,
                        uid = userid,
                        expire_at = DateTimeOffset.Now.AddDays(daysneedtobeadded)
                    };
                await db.InsertAsync(bed);
                return true;
            }
            catch
            {
                return false;
            }
        }
        else
        {
            try
            {
                result.expire_at.AddDays(daysneedtobeadded);
                _ =
                    await db.BadgeExpirationDateRec
                        .Where(it => it.uid == userid && it.badge_id == badgeid)
                        .Set(
                            it => it.expire_at,
                            it => it.expire_at.DateTime.AddDays(daysneedtobeadded)
                        )
                        .UpdateAsync() > 0;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static async Task<List<Model.User>> GetAllUsersWhoHadBadge()
    {
        using var db = GetInstance();
        return await db.User.Where(it => it.owned_badge_ids != null).ToListAsync();
    }

    public static async Task<List<Model.BadgeList>> GetAllBadges()
    {
        using var db = GetInstance();
        return await db.BadgeList.ToListAsync();
    }

    public static async Task<int> RemoveBadgeExpirationRecord(int userid, int badgeid)
    {
        using var db = GetInstance();
        return await db.BadgeExpirationDateRec
            .Where(x => x.uid == userid && x.badge_id == badgeid)
            .DeleteAsync();
    }

    public static async Task<bool> UpdateChatBotInfo(
        long uid,
        string botdefine,
        string openaikey,
        string organization
    )
    {
        using var db = GetInstance();
        var data = await db.ChatBot.FirstOrDefaultAsync(it => it.uid == uid);
        var result = await db.InsertOrReplaceAsync(
            new Model.ChatBot()
            {
                uid = (int)uid,
                botdefine = botdefine,
                openaikey = openaikey,
                organization = organization
            }
        );
        return result > 0;
    }

    public static async Task<Model.ChatBot?> GetChatBotInfo(long uid)
    {
        using var db = GetInstance();
        return await db.ChatBot.Where(it => it.uid == uid).FirstOrDefaultAsync();
    }
}
