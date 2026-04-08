#pragma warning disable CS8602 // 解引用可能出现空引用。
using Flurl.Util;
using KanonBot.API;
using KanonBot.Drivers;
using KanonBot.Functions.OSU;
using KanonBot.Message;
using LanguageExt.SomeHelp;
using LanguageExt.UnsafeValueAccess;
using System.Net;

namespace KanonBot.Functions
{
    public static class Accounts
    {
        public struct AccInfo
        {
            public required Platform platform;
            public required string uid;
        }

        /// <summary>
        /// Resolved user context from IAM + local DB.
        /// </summary>
        public class UserContext
        {
            public required string IamUserId { get; set; }
            public long? OsuUid { get; set; }
            public string? DisplayName { get; set; }
            public Database.Model.UserOSU? OsuSettings { get; set; }
        }

        /// <summary>
        /// Resolve a platform user to IAM UUID + osu UID + local osu settings.
        /// </summary>
        public static async Task<UserContext?> ResolveIamUser(Target target)
        {
            var accInfo = GetAccInfo(target);
            if (accInfo.platform == Platform.Unknown) return null;

            var provider = API.IAM.Client.PlatformToProvider(accInfo.platform);
            var iamUserId = await API.IAM.Client.GetIamUserIdByExternalId(provider, accInfo.uid);
            if (iamUserId == null) return null;

            var profile = await API.IAM.Client.GetIamUserPublicProfile(iamUserId);
            if (profile == null) return new UserContext { IamUserId = iamUserId };

            var osuUid = API.IAM.Client.ExtractOsuUid(profile);
            Database.Model.UserOSU? osuSettings = null;
            if (osuUid.HasValue)
            {
                osuSettings = await Database.Client.GetOsuUser(osuUid.Value);
            }

            return new UserContext
            {
                IamUserId = iamUserId,
                OsuUid = osuUid,
                DisplayName = profile.DisplayName ?? profile.UserName,
                OsuSettings = osuSettings
            };
        }

        /// <summary>
        /// Resolve IAM user by IAM User ID directly (for @osu:xxx lookups).
        /// </summary>
        public static async Task<UserContext?> ResolveIamUserById(string iamUserId)
        {
            var profile = await API.IAM.Client.GetIamUserPublicProfile(iamUserId);
            if (profile == null) return null;

            var osuUid = API.IAM.Client.ExtractOsuUid(profile);
            Database.Model.UserOSU? osuSettings = null;
            if (osuUid.HasValue)
            {
                osuSettings = await Database.Client.GetOsuUser(osuUid.Value);
            }

            return new UserContext
            {
                IamUserId = iamUserId,
                OsuUid = osuUid,
                DisplayName = profile.DisplayName ?? profile.UserName,
                OsuSettings = osuSettings
            };
        }

        public static async Task RegAccount(Target target, string cmd)
        {
            var code = cmd.Trim();

            if (string.IsNullOrEmpty(code) || code.Length > 6)
            {
                await target.reply("请输入您在网页端获取的验证码。\n用法: !reg 验证码");
                return;
            }

            var accInfo = GetAccInfo(target);
            if (accInfo.platform == Platform.Unknown)
            {
                await target.reply("无法获取您的平台信息。");
                return;
            }

            // Check if already bound
            var provider = API.IAM.Client.PlatformToProvider(accInfo.platform);
            var existingIamId = await API.IAM.Client.GetIamUserIdByExternalId(provider, accInfo.uid);
            if (existingIamId != null)
            {
                await target.reply("您的账户已经绑定了 desu.life 账户。");
                return;
            }

            var result = await API.IAM.Client.SubmitVerification(provider, code, accInfo.uid);

            switch (result)
            {
                case API.IAM.VerifyResult.Success:
                    await target.reply("绑定成功！您的平台账户已与 desu.life 账户关联。");
                    break;
                case API.IAM.VerifyResult.InvalidCode:
                    await target.reply("验证码无效或已过期，请重新在网页端生成验证码。");
                    break;
                case API.IAM.VerifyResult.InvalidApiKey:
                    Log.Error("IAM API Key is invalid for provider {Provider}", provider);
                    await target.reply("服务配置错误，请联系管理员。");
                    break;
                case API.IAM.VerifyResult.Misconfigured:
                    Log.Error("IAM integration is misconfigured for provider {Provider}", provider);
                    await target.reply("服务配置错误，请联系管理员。");
                    break;
                default:
                    await target.reply("绑定过程中出现错误，请稍后再试。");
                    break;
            }
        }

        public static async Task BindService(Target target, string cmd)
        {
            string childCmd_1 = "";
            try
            {
                var tmp = cmd.Split(' ', 2, StringSplitOptions.TrimEntries);
                childCmd_1 = tmp[0];
            }
            catch { }

            if (childCmd_1 == "osu")
            {
                await target.reply("osu 账户绑定已迁移至网页端，请前往 https://iam.neonprizma.com 绑定您的 osu 账户。");
                return;
            }
            else if (childCmd_1 == "ppysb")
            {
                await target.reply("ppy.sb 账户绑定已迁移至网页端，请前往 https://iam.neonprizma.com 绑定您的 ppy.sb 账户。");
                return;
            }
            else
            {
                await target.reply("请按照以下格式进行绑定。\n!bind osu\n!bind ppysb");
                return;
            }
        }

        public static async Task<(Option<API.OSU.Models.UserExtended>, Option<Database.Model.User>)> ParseAtOsu(string atmsg)
        {
            var res = Utils.SplitKvp(atmsg);
            if (res.IsNone)
                return (None, None);

            var (k, v) = res.Value();

            if (k == "osu")
            {
                var uid = parseLong(v).IfNone(() => 0);
                if (uid == 0)
                    return (None, None);

                var osuacc_ = await API.OSU.Client.GetUser(uid);
                if (osuacc_ is null)
                    return (None, None);

                var dbuser_ = await GetAccountByOsuUid(uid);
                if (dbuser_ is null)
                    return (Some(osuacc_!), None);
                else
                    return (Some(osuacc_!), Some(dbuser_!));
            }

            var user = await ParseAtBase(atmsg);
            if (user.IsNone)
                return (None, None);

            var dbuser = user.ValueUnsafe();

            var dbosu = await CheckOsuAccount(dbuser.uid);
            if (dbosu is null)
                return (None, Some(dbuser));

            var osuacc = await API.OSU.Client.GetUser(dbosu.osu_uid);
            if (osuacc is null)
                return (None, Some(dbuser));
            else
                return (Some(osuacc), Some(dbuser));
        }

        public static async Task<(Option<API.OSU.Models.UserExtended>, Option<Database.Model.User>)> ParseAtPpysb(string atmsg)
        {
            var res = Utils.SplitKvp(atmsg);
            if (res.IsNone)
                return (None, None);

            var (k, v) = res.Value();

            if (k == "sb")
            {
                var uid = parseLong(v).IfNone(() => 0);
                if (uid == 0)
                    return (None, None);

                var osuacc_ = await API.PPYSB.Client.GetUser(uid);
                if (osuacc_ is null)
                    return (None, None);

                var dbuser_ = await GetAccountByPpysbUid(uid);
                if (dbuser_ is null)
                    return (Some(osuacc_.ToOsu(null)!), None);
                else
                    return (Some(osuacc_.ToOsu(null)!), Some(dbuser_!));
            }

            var user = await ParseAtBase(atmsg);
            if (user.IsNone)
                return (None, None);

            var dbuser = user.ValueUnsafe();

            var dbosu = await CheckPpysbAccount(dbuser.uid);
            if (dbosu is null)
                return (None, Some(dbuser));

            var osuacc = await API.PPYSB.Client.GetUser(dbosu.osu_uid);
            if (osuacc is null)
                return (None, Some(dbuser));
            else
                return (Some(osuacc.ToOsu(dbosu.mode?.ToPpysbMode())!), Some(dbuser));
        }

        public static async Task<Option<Database.Model.User>> ParseAtBase(string atmsg)
        {
            var res = Utils.SplitKvp(atmsg);
            if (res.IsNone)
                return None;

            var (k, v) = res.Value();

            var platform = k switch
            {
                "qq" => Platform.OneBot,
                "guild" => Platform.Guild,
                "discord" => Platform.Discord,
                "kook" => Platform.KOOK,
                _ => Platform.Unknown
            };
            if (platform == Platform.Unknown)
                return None;

            var dbuser = await GetAccount(v, platform);
            if (dbuser is null)
                return None;

            return Some(dbuser!);
        }

        /// <summary>
        /// Get platform account info from target (unchanged).
        /// </summary>
        public static AccInfo GetAccInfo(Target target)
        {
            switch (target.platform)
            {
                case Platform.Guild:
                    if (target.raw is QQGuild.Models.MessageData g)
                    {
                        return new AccInfo() { platform = Platform.Guild, uid = g.Author.ID };
                    }
                    break;
                case Platform.OneBot:
                    if (target.raw is OneBot.Models.CQMessageEventBase o)
                    {
                        return new AccInfo() { platform = Platform.OneBot, uid = o.UserId.ToString() };
                    }
                    break;
                case Platform.KOOK:
                    if (target.raw is Kook.WebSocket.SocketMessage k)
                    {
                        return new AccInfo() { platform = Platform.KOOK, uid = k.Author.Id.ToString() };
                    }
                    break;
                case Platform.Discord:
                    if (target.raw is Discord.IMessage d)
                    {
                        return new AccInfo() { platform = Platform.Discord, uid = d.Author.Id.ToString() };
                    }
                    break;
            }
            return new() { platform = Platform.Unknown, uid = "" };
        }

        // === Backward compatibility methods (delegate to local DB, still used by many callers) ===

        public static async Task<Database.Model.User?> GetAccount(string uid, Platform platform)
        {
            return await Database.Client.GetUsersByUID(uid, platform);
        }
        public static async Task<Database.Model.User?> GetAccountByOsuUid(long osu_uid)
        {
            return await Database.Client.GetUserByOsuUID(osu_uid);
        }
        public static async Task<Database.Model.UserOSU?> CheckOsuAccount(long uid)
        {
            return await Database.Client.GetOsuUserByUID(uid);
        }
        public static async Task<Database.Model.User?> GetAccountByPpysbUid(long osu_uid)
        {
            return await Database.Client.GetUserByPpysbUID(osu_uid);
        }
        public static async Task<Database.Model.UserPPYSB?> CheckPpysbAccount(long uid)
        {
            return await Database.Client.GetPpysbUserByUID(uid);
        }
    }
}
