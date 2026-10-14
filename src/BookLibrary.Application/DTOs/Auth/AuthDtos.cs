namespace BookLibrary.Application.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string DisplayName);

public record LoginRequest(
    string Email,
    string Password);

public record RefreshTokenRequest(
    string RefreshToken);

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    UserProfileDto User);

public record UserProfileDto(
    string Id,
    string Email,
    string DisplayName,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc);

public record UpdateProfileRequest(
    string DisplayName);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public record ForgotPasswordRequest(
    string Email);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword);

public record VerifyEmailRequest(
    string UserId,
    string Token);

public record AdminUserDto(
    string Id,
    string Email,
    string DisplayName,
    bool EmailConfirmed,
    bool IsLockedOut,
    IReadOnlyList<string> Roles);

public record UpdateUserRoleRequest(
    string Role);
