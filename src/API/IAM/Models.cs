using KanonBot.Serializer;
using Newtonsoft.Json;

namespace KanonBot.API.IAM;

public class SubmitVerificationRequest
{
    public required string Code { get; set; }
    public required string ExternalId { get; set; }
}

public class BoundUserLookupResponse
{
    public string UserId { get; set; } = "";
}

public class PpysbBoundUsersLookupResponse
{
    public List<string> UserIds { get; set; } = [];
}

public class UserBindingsResponse
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
    public DateTimeOffset? CreateAt { get; set; }
    [JsonConverter(typeof(FlexibleDateTimeOffsetConverter))]
    public DateTimeOffset? LastLoginAt { get; set; }
    public UserBindings Bindings { get; set; } = new();
}

public class UserBindings
{
    public string? Qq { get; set; }
    public string? Discord { get; set; }
    public string? QqGuild { get; set; }
    public string? Osu { get; set; }
    public string? PpySb { get; set; }
}

public class IamErrorResponse
{
    public string Error { get; set; } = "";
    public string? Message { get; set; }
}
