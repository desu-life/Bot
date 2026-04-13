using KanonBot.API.OSU;
using KanonBot.Drivers;
using LanguageExt.UnsafeValueAccess;

namespace KanonBot.Functions
{
    public static partial class Accounts
    {
        /// <summary>
        /// Unified user resolution for command handlers.
        /// Handles self-query (via IAM), at-query, and name-query paths.
        /// Returns null if an error message was sent to the user (caller should return).
        /// </summary>
        public static async Task<CommandUserResult?> ResolveCommandUser(
            Target target,
            BotCmdHelper.BotParameter command
        )
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
            BotCmdHelper.BotParameter command
        )
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
                await target.reply(
                    "你还没有绑定 desu.life 账户，请使用 !bind 进行绑定。"
                );
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
                    await target.reply("你还没有绑定 ppy.sb 账户，请前往 https://iam.neonprizma.com/ 绑定。");
                    return null;
                }

                // Resolve ppysb mode: command > Kagami preference > default
                API.PPYSB.Mode? sbmode = command.sb_osu_mode;
                if (!sbmode.HasValue)
                {
                    var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                    sbmode = KagamiExtensions.ParseKagamiPpysbMode(kagamiProfile?.KanonBot?.PpySbPreferredGameMode);
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
                    await target.reply("你还没有绑定 osu! 账户，请前往 https://iam.neonprizma.com/ 绑定。");
                    return null;
                }

                // Resolve osu mode: command > Kagami preference > default
                API.OSU.Mode? mode = command.osu_mode;
                if (!mode.HasValue)
                {
                    var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                    mode = KagamiExtensions.ParseKagamiMode(kagamiProfile?.KanonBot?.PreferredGameMode);
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
            BotCmdHelper.BotParameter command
        )
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
            BotCmdHelper.BotParameter command
        )
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
                // Both found - get mode preference from Kagami
                var osuInfo = atOSU.ValueUnsafe();
                var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                var kagamiMode = KagamiExtensions.ParseKagamiMode(kagamiProfile?.KanonBot?.PreferredGameMode);
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
                    var kagamiMode = KagamiExtensions.ParseKagamiMode(kagamiProfile?.KanonBot?.PreferredGameMode);
                    mode = command.osu_mode ?? kagamiMode ?? onlineUser.Mode;
                }

                _ = await Database.Client.GetOsuUser(onlineUser.Id);

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
            BotCmdHelper.BotParameter command
        )
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
                // Both found - get mode preference from Kagami
                var osuInfo = atOSU.ValueUnsafe();
                var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                var kagamiMode = KagamiExtensions.ParseKagamiPpysbMode(kagamiProfile?.KanonBot?.PpySbPreferredGameMode);
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
                    sbmode ??= KagamiExtensions.ParseKagamiPpysbMode(kagamiProfile?.KanonBot?.PpySbPreferredGameMode);
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
