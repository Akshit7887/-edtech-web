using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EdTechApi.DTOs;

public class GenerateOtpRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email must be at most 255 characters")]
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MaxLength(128, ErrorMessage = "Password must be at most 128 characters")]
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [RegularExpression("^(teacher|student)$", ErrorMessage = "Role must be 'teacher' or 'student'")]
    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

public class GenerateOtpResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("otp_code")]
    public string? OtpCode { get; set; }
}

public class VerifyOtpRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be 6 digits")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "OTP code must be 6 digits")]
    [JsonPropertyName("otp_code")]
    public string OtpCode { get; set; } = string.Empty;

    [RegularExpression("^(teacher|student)$", ErrorMessage = "Role must be 'teacher' or 'student'")]
    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

public class VerifyOtpResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Name is required")]
    [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
    [MaxLength(255, ErrorMessage = "Name must be at most 255 characters")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email must be at most 255 characters")]
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [MaxLength(128, ErrorMessage = "Password must be at most 128 characters")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("^(teacher|student)$", ErrorMessage = "Role must be 'teacher' or 'student'")]
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

public class RegisterOtpResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("otp_code")]
    public string? OtpCode { get; set; }
}

public class VerifyRegisterOtpRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be 6 digits")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "OTP code must be 6 digits")]
    [JsonPropertyName("otp_code")]
    public string OtpCode { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be 6 digits")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "OTP code must be 6 digits")]
    [JsonPropertyName("otp_code")]
    public string OtpCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [MaxLength(128, ErrorMessage = "Password must be at most 128 characters")]
    [JsonPropertyName("new_password")]
    public string NewPassword { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Token is required")]
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [MaxLength(255, ErrorMessage = "Name must be at most 255 characters")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email must be at most 255 characters")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [RegularExpression(@"^\+?[0-9\s\-\(\)]{7,20}$", ErrorMessage = "Invalid phone number format")]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    [JsonPropertyName("current_password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [MaxLength(128, ErrorMessage = "Password must be at most 128 characters")]
    [JsonPropertyName("new_password")]
    public string NewPassword { get; set; } = string.Empty;
}

public class ExternalAuthRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(255, ErrorMessage = "Name must be at most 255 characters")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("^(teacher|student)$", ErrorMessage = "Role must be 'teacher' or 'student'")]
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "External user ID is required")]
    [JsonPropertyName("external_user_id")]
    public string ExternalUserId { get; set; } = string.Empty;
}
