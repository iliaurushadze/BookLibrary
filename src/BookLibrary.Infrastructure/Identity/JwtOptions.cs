namespace BookLibrary.Infrastructure.Identity;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "BookLibrary.Api";
    public string Audience { get; set; } = "BookLibrary.Client";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 30;
    public int RefreshTokenExpirationDays { get; set; } = 14;
}
