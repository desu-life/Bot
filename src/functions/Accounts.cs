#pragma warning disable CS8602 // 解引用可能出现空引用。
using KanonBot.API;
using KanonBot.Drivers;
using KanonBot.Functions.OSU;
using KanonBot.Message;
using LanguageExt.SomeHelp;
using LanguageExt.UnsafeValueAccess;

namespace KanonBot.Functions
{
    public static partial class Accounts
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

        public static async Task<(Option<API.OSU.Models.UserExtended>, string?, bool)> ParseAtOsu(
            string atmsg
        )
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

        public static async Task<(Option<API.OSU.Models.UserExtended>, string?, bool)> ParseAtPpysb(
            string atmsg
        )
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
            try
            {
                provider = API.IAM.Client.PlatformToProvider(platform);
            }
            catch (NotSupportedException)
            {
                return null;
            }

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
                        return new AccInfo()
                        {
                            platform = Platform.OneBot,
                            uid = o.UserId.ToString()
                        };
                    }
                    break;
                case Platform.KOOK:
                    if (target.raw is Kook.WebSocket.SocketMessage k)
                    {
                        return new AccInfo()
                        {
                            platform = Platform.KOOK,
                            uid = k.Author.Id.ToString()
                        };
                    }
                    break;
                case Platform.Discord:
                    if (target.raw is Discord.IMessage d)
                    {
                        return new AccInfo()
                        {
                            platform = Platform.Discord,
                            uid = d.Author.Id.ToString()
                        };
                    }
                    break;
            }
            return new() { platform = Platform.Unknown, uid = "" };
        }
    }
}
