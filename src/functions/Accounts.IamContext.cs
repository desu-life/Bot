using KanonBot.Drivers;

namespace KanonBot.Functions
{
    public static partial class Accounts
    {
        /// <summary>
        /// Resolve a platform user to IAM UUID + osu UID + local osu settings.
        /// </summary>
        public static async Task<UserContext?> ResolveIamUser(Target target)
        {
            var accInfo = GetAccInfo(target);
            if (accInfo.platform == Platform.Unknown)
                return null;

            var provider = API.IAM.Client.PlatformToProvider(accInfo.platform);
            var iamUserId = await API.IAM.Client.GetIamUserIdByExternalId(provider, accInfo.uid);
            if (iamUserId == null)
                return null;

            var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
            if (bindings == null)
                return new UserContext { IamUserId = iamUserId };

            var osuUid = API.IAM.Client.ExtractOsuUid(bindings);

            return new UserContext
            {
                IamUserId = iamUserId,
                OsuUid = osuUid,
                DisplayName = bindings.DisplayName ?? bindings.UserName,
            };
        }

        /// <summary>
        /// Resolve IAM user by IAM User ID directly (for @osu:xxx lookups).
        /// </summary>
        public static async Task<UserContext?> ResolveIamUserById(string iamUserId)
        {
            var bindings = await API.IAM.Client.GetUserBindings(iamUserId);
            if (bindings == null)
                return null;

            var osuUid = API.IAM.Client.ExtractOsuUid(bindings);

            return new UserContext
            {
                IamUserId = iamUserId,
                OsuUid = osuUid,
                DisplayName = bindings.DisplayName ?? bindings.UserName,
            };
        }
    }
}
