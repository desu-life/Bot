using System.ComponentModel;
using System.Text.RegularExpressions;
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.PPYSB;

public partial class Models
{
    public enum Privileges
    {
        UNRESTRICTED = 1 << 0,
        VERIFIED = 1 << 1,

        WHITELISTED = 1 << 2,

        SUPPORTER = 1 << 4,
        PREMIUM = 1 << 5,

        ALUMNI = 1 << 7,

        TOURNEY_MANAGER = 1 << 10,
        NOMINATOR = 1 << 11,
        MODERATOR = 1 << 12,
        ADMINISTRATOR = 1 << 13,
        DEVELOPER = 1 << 14,

        DONATOR = SUPPORTER | PREMIUM,
        STAFF = MODERATOR | ADMINISTRATOR | DEVELOPER,
    }
}
