using CommandSystem.Parsing;
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
            ParsedCommand command
        )
        {
            if (command.SelfQuery)
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
            ParsedCommand command
        )
        {
            var accInfo = GetAccInfo(target);
            if (accInfo.platform == Platform.Unknown)
            {
                await target.Treply("account.platform_unknown");
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
                await target.Treply("account.platform_unsupported");
                return null;
            }

            var iamUserId = await API.IAM.Client.GetIamUserIdByExternalId(provider, accInfo.uid);
            if (iamUserId == null)
            {
                await target.Treply("account.not_bound");
                return null;
            }

            var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
            if (bindings == null)
            {
                await target.Treply("account.fetch_failed");
                return null;
            }

            bool isQuerySb = command.Flag("sb_server");

            if (isQuerySb)
            {
                // ppysb self-query
                var ppysbUid = API.IAM.Client.ExtractPpysbUid(bindings);
                if (!ppysbUid.HasValue)
                {
                    await target.Treply("account.ppysb_not_bound");
                    return null;
                }

                // Resolve ppysb mode: command > Kagami preference > default
                API.PPYSB.Mode? sbmode = command.GetString("osu_mode")?.ParsePpysbMode();
                if (!sbmode.HasValue)
                {
                    var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                    sbmode = KagamiExtensions.ParseKagamiPpysbMode(
                        kagamiProfile?.KanonBot?.PpySbPreferredGameMode
                    );
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
                    await target.Treply("account.osu_not_bound");
                    return null;
                }

                // Resolve osu mode: command > Kagami preference > default
                API.OSU.Mode? mode = command.GetString("osu_mode")?.ParseMode();
                if (!mode.HasValue)
                {
                    var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                    mode = KagamiExtensions.ParseKagamiMode(
                        kagamiProfile?.KanonBot?.PreferredGameMode
                    );
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
            ParsedCommand command
        )
        {
            bool isQuerySb = command.Flag("sb_server");

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
            ParsedCommand command
        )
        {
            var username = command.GetString("username") ?? "";
            var osuMode = command.GetString("osu_mode")?.ParseMode();

            // Try at-query first
            var (atOSU, iamUserId, hasOsuBinding) = await ParseAtOsu(username);

            if (atOSU.IsNone && iamUserId != null)
            {
                // User registered but osu not available
                if (!hasOsuBinding)
                    await target.Treply("account.other_osu_not_bound");
                else
                    await target.Treply("account.banned");
                return null;
            }
            else if (!atOSU.IsNone && iamUserId == null)
            {
                // osu account found, not registered
                var osuInfo = atOSU.ValueUnsafe();
                return new CommandUserResult
                {
                    OsuId = osuInfo.Id,
                    Mode = osuMode ?? osuInfo.Mode,
                    IsPpysb = false,
                };
            }
            else if (!atOSU.IsNone && iamUserId != null)
            {
                // Both found - get mode preference from Kagami
                var osuInfo = atOSU.ValueUnsafe();
                var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                var kagamiMode = KagamiExtensions.ParseKagamiMode(
                    kagamiProfile?.KanonBot?.PreferredGameMode
                );
                return new CommandUserResult
                {
                    OsuId = osuInfo.Id,
                    Mode = osuMode ?? kagamiMode ?? osuInfo.Mode,
                    IsPpysb = false,
                    IamUserId = iamUserId,
                };
            }
            else
            {
                // Not an at-query, try name query
                var onlineUser = await API.OSU
                    .Client
                    .GetUser(username, osuMode ?? API.OSU.Mode.OSU);
                if (onlineUser == null)
                {
                    await target.Treply("error.user_not_found");
                    return null;
                }

                var iamId = await API.IAM.Client.GetIamUserIdByOsuUid(onlineUser.Id);
                API.OSU.Mode mode = osuMode ?? onlineUser.Mode;

                if (iamId != null)
                {
                    var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamId);
                    var kagamiMode = KagamiExtensions.ParseKagamiMode(
                        kagamiProfile?.KanonBot?.PreferredGameMode
                    );
                    mode = osuMode ?? kagamiMode ?? onlineUser.Mode;
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
            ParsedCommand command
        )
        {
            var username = command.GetString("username") ?? "";
            var sbMode = command.GetString("osu_mode")?.ParsePpysbMode();

            // Try at-query first
            var (atOSU, iamUserId, hasPpysbBinding) = await ParseAtPpysb(username);

            if (atOSU.IsNone && iamUserId != null)
            {
                if (!hasPpysbBinding)
                    await target.Treply("account.other_ppysb_not_bound");
                else
                    await target.Treply("account.banned");
                return null;
            }
            else if (!atOSU.IsNone && iamUserId == null)
            {
                var osuInfo = atOSU.ValueUnsafe();
                return new CommandUserResult
                {
                    OsuId = osuInfo.Id,
                    Mode = null,
                    SbMode = sbMode ?? ((int)osuInfo.Mode).ToPpysbMode(),
                    IsPpysb = true,
                };
            }
            else if (!atOSU.IsNone && iamUserId != null)
            {
                // Both found - get mode preference from Kagami
                var osuInfo = atOSU.ValueUnsafe();
                var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamUserId);
                var kagamiMode = KagamiExtensions.ParseKagamiPpysbMode(
                    kagamiProfile?.KanonBot?.PpySbPreferredGameMode
                );
                return new CommandUserResult
                {
                    OsuId = osuInfo.Id,
                    Mode = null,
                    SbMode = sbMode ?? kagamiMode ?? ((int)osuInfo.Mode).ToPpysbMode(),
                    IsPpysb = true,
                    IamUserId = iamUserId,
                };
            }
            else
            {
                // Name query
                var onlineUser = await API.PPYSB.Client.GetUser(username);
                if (onlineUser == null)
                {
                    await target.Treply("error.user_not_found");
                    return null;
                }

                var iamUserIds = await API.IAM.Client.GetIamUserIdsByPpysbUid(onlineUser.Info.Id);
                var iamId = iamUserIds?.FirstOrDefault();
                API.PPYSB.Mode? resolvedSbMode = sbMode;

                if (iamId != null)
                {
                    var kagamiProfile = await API.Kagami.Client.GetPublicKanonBotProfile(iamId);
                    resolvedSbMode ??= KagamiExtensions.ParseKagamiPpysbMode(
                        kagamiProfile?.KanonBot?.PpySbPreferredGameMode
                    );
                }
                resolvedSbMode ??= onlineUser.Info.PreferredMode;

                return new CommandUserResult
                {
                    OsuId = onlineUser.Info.Id,
                    Mode = null,
                    SbMode = resolvedSbMode,
                    IsPpysb = true,
                    IamUserId = iamId,
                };
            }
        }
    }
}
