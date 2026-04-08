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

public class IamUserProfile
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public List<ExternalUidInfo> ExternalUids { get; set; } = [];
}

public class ExternalUidInfo
{
    public string Provider { get; set; } = "";
    public string Uid { get; set; } = "";
    public string? DisplayName { get; set; }
}

public class IamErrorResponse
{
    public string Error { get; set; } = "";
    public string? Message { get; set; }
}
