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
            public long? PpysbUid { get; set; }
            public string? DisplayName { get; set; }
            public Database.Model.UserOSU? OsuSettings { get; set; }
        }

        /// <summary>
        /// Result of unified command user resolution.
        /// Replaces the scattered osuID/mode/DBUser/DBOsuInfo/is_ppysb variables across command handlers.
        /// </summary>
        public class CommandUserResult
        {
            /// <summary>The resolved osu / ppysb user ID.</summary>
            public long OsuId { get; set; }

            /// <summary>Resolved osu game mode.</summary>
            public API.OSU.Mode? Mode { get; set; }

            /// <summary>Resolved ppysb game mode (only set when IsPpysb == true).</summary>
            public API.PPYSB.Mode? SbMode { get; set; }

            /// <summary>Whether this is a ppysb server query.</summary>
            public bool IsPpysb { get; set; }

            /// <summary>IAM user ID (null if user is not registered).</summary>
            public string? IamUserId { get; set; }

            /// <summary>Whether the user is a registered desu.life user.</summary>
            public bool IsRegistered => IamUserId != null;
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

            var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
            if (bindings == null) return new UserContext { IamUserId = iamUserId };

            var osuUid = API.IAM.Client.ExtractOsuUid(bindings);
            Database.Model.UserOSU? osuSettings = null;
            if (osuUid.HasValue)
            {
                osuSettings = await Database.Client.GetOsuUser(osuUid.Value);
            }

            return new UserContext
            {
                IamUserId = iamUserId,
                OsuUid = osuUid,
                DisplayName = bindings.DisplayName ?? bindings.UserName,
                OsuSettings = osuSettings
            };
        }

        /// <summary>
        /// Resolve IAM user by IAM User ID directly (for @osu:xxx lookups).
        /// </summary>
        public static async Task<UserContext?> ResolveIamUserById(string iamUserId)
        {
            var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
            if (bindings == null) return null;

            var osuUid = API.IAM.Client.ExtractOsuUid(bindings);
            Database.Model.UserOSU? osuSettings = null;
            if (osuUid.HasValue)
            {
                osuSettings = await Database.Client.GetOsuUser(osuUid.Value);
            }

            return new UserContext
            {
                IamUserId = iamUserId,
                OsuUid = osuUid,
                DisplayName = bindings.DisplayName ?? bindings.UserName,
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

        public static async Task<(Option<API.OSU.Models.UserExtended>, string?, bool)> ParseAtOsu(string atmsg)
        {
            var res = Utils.SplitKvp(atmsg);
            if (res.IsNone)
                return (None, null, false);

            var (k, v) = res.Value();

            if (k == "osu")
            {
                var uid = parseLong(v).IfNone(() => 0);
                if (uid == 0)
                    return (None, null, false);

                var osuacc = await API.OSU.Client.GetUser(uid);
                if (osuacc is null)
                    return (None, null, false);

                var iamId = await API.IAM.Client.GetIamUserIdByOsuUid(uid);
                return (Some(osuacc), iamId, true);
            }

            var iamUserId = await ParseAtBase(atmsg);
            if (iamUserId == null)
                return (None, null, false);

            var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
            if (bindings == null)
                return (None, iamUserId, false);

            var osuUid = API.IAM.Client.ExtractOsuUid(bindings);
            if (!osuUid.HasValue)
                return (None, iamUserId, false); // registered, no osu binding

            var osuUser = await API.OSU.Client.GetUser(osuUid.Value);
            if (osuUser is null)
                return (None, iamUserId, true); // registered, has binding, banned

            return (Some(osuUser), iamUserId, true);
        }

        public static async Task<(Option<API.OSU.Models.UserExtended>, string?, bool)> ParseAtPpysb(string atmsg)
        {
            var res = Utils.SplitKvp(atmsg);
            if (res.IsNone)
                return (None, null, false);

            var (k, v) = res.Value();

            if (k == "sb")
            {
                var uid = parseLong(v).IfNone(() => 0);
                if (uid == 0)
                    return (None, null, false);

                var sbUser = await API.PPYSB.Client.GetUser(uid);
                if (sbUser is null)
                    return (None, null, false);

                var iamUserIds = await API.IAM.Client.GetIamUserIdsByPpysbUid(uid);
                var iamId = iamUserIds?.FirstOrDefault();
                return (Some(sbUser.ToOsu(null)!), iamId, true);
            }

            var iamUserId = await ParseAtBase(atmsg);
            if (iamUserId == null)
                return (None, null, false);

            var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
            if (bindings == null)
                return (None, iamUserId, false);

            var ppysbUid = API.IAM.Client.ExtractPpysbUid(bindings);
            if (!ppysbUid.HasValue)
                return (None, iamUserId, false); // registered, no ppysb binding

            var sbUser2 = await API.PPYSB.Client.GetUser(ppysbUid.Value);
            if (sbUser2 is null)
                return (None, iamUserId, true); // registered, has binding, banned

            return (Some(sbUser2.ToOsu(null)!), iamUserId, true);
        }

        public static async Task<string?> ParseAtBase(string atmsg)
        {
            var res = Utils.SplitKvp(atmsg);
            if (res.IsNone)
                return null;

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
                return null;

            string provider;
            try { provider = API.IAM.Client.PlatformToProvider(platform); }
            catch (NotSupportedException) { return null; }

            return await API.IAM.Client.GetIamUserIdByExternalId(provider, v);
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

        // === Kagami mode string <-> enum conversion ===

        public static API.OSU.Mode? KagamiModeToOsu(string? mode) => mode?.ToLowerInvariant() switch
        {
            "osu" => API.OSU.Mode.OSU,
            "taiko" => API.OSU.Mode.Taiko,
            "fruits" or "ctb" => API.OSU.Mode.Fruits,
            "mania" => API.OSU.Mode.Mania,
            _ => null
        };

        public static API.PPYSB.Mode? KagamiModeToPpysb(string? mode) => mode?.ToLowerInvariant() switch
        {
            "osu" => API.PPYSB.Mode.OSU,
            "taiko" => API.PPYSB.Mode.Taiko,
            "fruits" or "ctb" => API.PPYSB.Mode.Fruits,
            "mania" => API.PPYSB.Mode.Mania,
            _ => null
        };

        /// <summary>
        /// Unified user resolution for command handlers.
        /// Handles self-query (via IAM), at-query, and name-query paths.
        /// Returns null if an error message was sent to the user (caller should return).
        /// </summary>
        public static async Task<CommandUserResult?> ResolveCommandUser(
            Target target,
            BotCmdHelper.BotParameter command)
        {
            if (command.self_query)
            {
                return await ResolveSelfQuery(target, command);
            }
            else
            {
                return await ResolveOtherQuery(target, command);
            }
        }

        private static async Task<CommandUserResult?> ResolveSelfQuery(
            Target target,
            BotCmdHelper.BotParameter command)
        {
            var accInfo = GetAccInfo(target);
            if (accInfo.platform == Platform.Unknown)
            {
                await target.reply("无法获取您的平台信息。");
                return null;
            }

            // Resolve via IAM
            string provider;
            try
            {
                provider = API.IAM.Client.PlatformToProvider(accInfo.platform);
            }
            catch (NotSupportedException)
            {
                await target.reply("当前平台暂不支持此功能。");
                return null;
            }

            var iamUserId = await API.IAM.Client.GetIamUserIdByExternalId(provider, accInfo.uid);
            if (iamUserId == null)
            {
                await target.reply("你还没有绑定 desu.life 账户，请先在 https://iam.neonprizma.com 注册并使用 !reg 验证码 进行绑定。");
                return null;
            }

            var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
            if (bindings == null)
            {
                await target.reply("获取账户信息失败，请稍后再试。");
                return null;
            }

            bool isQuerySb = command.sb_server;

            if (isQuerySb)
            {
                // ppysb self-query
                var ppysbUid = API.IAM.Client.ExtractPpysbUid(bindings);
                if (!ppysbUid.HasValue)
                {
                    await target.reply("你还没有绑定 ppy.sb 账户，请前往 https://iam.neonprizma.com 绑定。");
                    return null;
                }

                // Resolve ppysb mode: command > Kagami preference > default
                API.PPYSB.Mode? sbmode = command.sb_osu_mode;
                if (!sbmode.HasValue)
                {
                    var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                    sbmode = KagamiModeToPpysb(kagamiProfile?.KanonBot?.PpySbPreferredGameMode);
                }
                sbmode ??= API.PPYSB.Mode.OSU;

                return new CommandUserResult
                {
                    OsuId = ppysbUid.Value,
                    Mode = null,
                    SbMode = sbmode,
                    IsPpysb = true,
                    IamUserId = iamUserId,
                };
            }
            else
            {
                // osu self-query
                var osuUid = API.IAM.Client.ExtractOsuUid(bindings);
                if (!osuUid.HasValue)
                {
                    await target.reply("你还没有绑定 osu! 账户，请前往 https://iam.neonprizma.com 绑定。");
                    return null;
                }

                // Resolve osu mode: command > Kagami preference > default
                API.OSU.Mode? mode = command.osu_mode;
                if (!mode.HasValue)
                {
                    var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                    mode = KagamiModeToOsu(kagamiProfile?.KanonBot?.PreferredGameMode);
                }
                mode ??= API.OSU.Mode.OSU;

                return new CommandUserResult
                {
                    OsuId = osuUid.Value,
                    Mode = mode,
                    SbMode = null,
                    IsPpysb = false,
                    IamUserId = iamUserId,
                };
            }
        }

        private static async Task<CommandUserResult?> ResolveOtherQuery(
            Target target,
            BotCmdHelper.BotParameter command)
        {
            bool isQuerySb = command.sb_server;

            if (isQuerySb)
            {
                return await ResolveOtherQueryPpysb(target, command);
            }
            else
            {
                return await ResolveOtherQueryOsu(target, command);
            }
        }

        private static async Task<CommandUserResult?> ResolveOtherQueryOsu(
            Target target,
            BotCmdHelper.BotParameter command)
        {
            // Try at-query first
            var (atOSU, iamUserId, hasOsuBinding) = await ParseAtOsu(command.osu_username);

            if (atOSU.IsNone && iamUserId != null)
            {
                // User registered but osu not available
                if (!hasOsuBinding)
                    await target.reply("ta还没有绑定osu账户呢。");
                else
                    await target.reply("被办了。");
                return null;
            }
            else if (!atOSU.IsNone && iamUserId == null)
            {
                // osu account found, not registered
                var osuInfo = atOSU.ValueUnsafe();
                return new CommandUserResult
                {
                    OsuId = osuInfo.Id,
                    Mode = command.osu_mode ?? osuInfo.Mode,
                    IsPpysb = false,
                };
            }
            else if (!atOSU.IsNone && iamUserId != null)
            {
                // Both found — get mode preference from Kagami
                var osuInfo = atOSU.ValueUnsafe();
                var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                var kagamiMode = KagamiModeToOsu(kagamiProfile?.KanonBot?.PreferredGameMode);
                return new CommandUserResult
                {
                    OsuId = osuInfo.Id,
                    Mode = command.osu_mode ?? kagamiMode ?? osuInfo.Mode,
                    IsPpysb = false,
                    IamUserId = iamUserId,
                };
            }
            else
            {
                // Not an at-query, try name query
                var onlineUser = await API.OSU.Client.GetUser(
                    command.osu_username,
                    command.osu_mode ?? API.OSU.Mode.OSU
                );
                if (onlineUser == null)
                {
                    await target.reply("猫猫没有找到此用户。");
                    return null;
                }

                var iamId = await API.IAM.Client.GetIamUserIdByOsuUid(onlineUser.Id);
                API.OSU.Mode mode = command.osu_mode ?? onlineUser.Mode;

                if (iamId != null)
                {
                    var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamId);
                    var kagamiMode = KagamiModeToOsu(kagamiProfile?.KanonBot?.PreferredGameMode);
                    mode = command.osu_mode ?? kagamiMode ?? onlineUser.Mode;
                }

                var dbOsuInfo = await Database.Client.GetOsuUser(onlineUser.Id);

                return new CommandUserResult
                {
                    OsuId = onlineUser.Id,
                    Mode = mode,
                    IsPpysb = false,
                    IamUserId = iamId,
                };
            }
        }

        private static async Task<CommandUserResult?> ResolveOtherQueryPpysb(
            Target target,
            BotCmdHelper.BotParameter command)
        {
            // Try at-query first
            var (atOSU, iamUserId, hasPpysbBinding) = await ParseAtPpysb(command.osu_username);

            if (atOSU.IsNone && iamUserId != null)
            {
                if (!hasPpysbBinding)
                    await target.reply("ta还没有绑定ppy.sb账户呢。");
                else
                    await target.reply("被办了。");
                return null;
            }
            else if (!atOSU.IsNone && iamUserId == null)
            {
                var osuInfo = atOSU.ValueUnsafe();
                return new CommandUserResult
                {
                    OsuId = osuInfo.Id,
                    Mode = null,
                    SbMode = command.sb_osu_mode ?? ((int)osuInfo.Mode).ToPpysbMode(),
                    IsPpysb = true,
                };
            }
            else if (!atOSU.IsNone && iamUserId != null)
            {
                // Both found — get mode preference from Kagami
                var osuInfo = atOSU.ValueUnsafe();
                var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                var kagamiMode = KagamiModeToPpysb(kagamiProfile?.KanonBot?.PpySbPreferredGameMode);
                return new CommandUserResult
                {
                    OsuId = osuInfo.Id,
                    Mode = null,
                    SbMode = command.sb_osu_mode ?? kagamiMode ?? ((int)osuInfo.Mode).ToPpysbMode(),
                    IsPpysb = true,
                    IamUserId = iamUserId,
                };
            }
            else
            {
                // Name query
                var onlineUser = await API.PPYSB.Client.GetUser(command.osu_username);
                if (onlineUser == null)
                {
                    await target.reply("猫猫没有找到此用户。");
                    return null;
                }

                var iamUserIds = await API.IAM.Client.GetIamUserIdsByPpysbUid(onlineUser.Info.Id);
                var iamId = iamUserIds?.FirstOrDefault();
                API.PPYSB.Mode? sbmode = command.sb_osu_mode;

                if (iamId != null)
                {
                    var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamId);
                    sbmode ??= KagamiModeToPpysb(kagamiProfile?.KanonBot?.PpySbPreferredGameMode);
                }
                sbmode ??= onlineUser.Info.PreferredMode;

                return new CommandUserResult
                {
                    OsuId = onlineUser.Info.Id,
                    Mode = null,
                    SbMode = sbmode,
                    IsPpysb = true,
                    IamUserId = iamId,
                };
            }
        }
    }
}
