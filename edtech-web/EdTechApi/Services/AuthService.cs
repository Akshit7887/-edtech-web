using System.Data;
using Dapper;
using EdTechApi.Data;
using EdTechApi.DTOs;
using EdTechApi.Models;

namespace EdTechApi.Services;

    public interface IAuthService
{
    Task<VerifyOtpResponse> AdminLoginAsync(string email, string password);
    Task<GenerateOtpResponse> GenerateOtpAsync(string identifier, string? role, string? password);
    Task<VerifyOtpResponse> VerifyOtpAsync(string identifier, string otpCode, string? role);
    Task<RegisterOtpResponse> SendRegisterOtpAsync(string name, string identifier, string password, string role);
    Task<VerifyOtpResponse> VerifyRegisterOtpAsync(string identifier, string otpCode);
    Task<object> ForgotPasswordAsync(string identifier);
    Task<object> ResetPasswordAsync(string identifier, string otpCode, string newPassword);
    Task<RefreshTokenResponse> RefreshTokenAsync(string oldToken);
    Task<UserInfo> UpdateProfileAsync(int userId, UpdateProfileRequest updates);
    Task<object> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<VerifyOtpResponse> ExternalAuthSessionAsync(string email, string name, string role, string externalUserId);
    Task DeleteProfileAsync(int userId);
    Task<string?> GetLatestOtpAsync(string identifier, string type);
}

public class AuthService : IAuthService
{
    private readonly IDbConnectionFactory _db;
    private readonly IJwtService _jwt;
    private readonly IOtpService _otp;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AuthService> _logger;
    private readonly IRedisCacheService _cache;

    public AuthService(IDbConnectionFactory db, IJwtService jwt, IOtpService otp, IEmailService email, IConfiguration config, IWebHostEnvironment env, ILogger<AuthService> logger, IRedisCacheService cache)
    {
        _db = db;
        _jwt = jwt;
        _otp = otp;
        _email = email;
        _config = config;
        _env = env;
        _logger = logger;
        _cache = cache;
    }

    private async Task EnsureStudentIdAsync(IDbConnection conn, User user)
    {
        if (user.Role != "student" || !string.IsNullOrEmpty(user.StudentId))
            return;

        var random = new Random();
        string sid;
        do
        {
            sid = random.Next(0, 1000000000).ToString("D10");
        } while (await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT 1 FROM \"Users\" WHERE \"student_id\" = @Sid",
            new { Sid = sid }) != null);

        await conn.ExecuteAsync(
            "UPDATE \"Users\" SET \"student_id\" = @Sid WHERE \"id\" = @Id",
            new { Sid = sid, Id = user.Id });
        user.StudentId = sid;
    }

    private static UserInfo UserToInfo(User user)
    {
        return new UserInfo
        {
            Id = user.Id,
            Name = user.Name,
            Role = user.Role,
            Phone = user.Phone,
            Email = user.Email,
            StudentId = user.StudentId,
            ApprovalStatus = user.ApprovalStatus
        };
    }

    private static bool IsValidBcryptHash(string? hash)
    {
        if (string.IsNullOrEmpty(hash) || hash.Length < 50)
            return false;
        var prefix = hash.Length >= 4 ? hash.Substring(0, 4) : hash;
        return prefix is "$2a$" or "$2b$" or "$2y$";
    }

    private async Task NotifyAdminsAsync(IDbConnection conn, string title, string message)
    {
        var admins = await conn.QueryAsync<int>("SELECT \"id\" FROM \"Users\" WHERE \"role\" = 'admin'");
        foreach (var adminId in admins)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO ""Notifications"" (""user_id"", ""title"", ""message"", ""type"", ""created_at"")
                  VALUES (@UserId, @Title, @Message, 'admin', @Now)",
                new { UserId = adminId, Title = title, Message = message, Now = DateTime.UtcNow });
        }
    }

    private void EnsureTeacherApproved(User user)
    {
        if (user.Role != "teacher" || user.ApprovalStatus == "approved")
            return;

        if (user.ApprovalStatus == "rejected")
            throw new AppException(403, string.IsNullOrEmpty(user.RejectionReason)
                ? "Your teacher account was rejected by the admin. Please contact support for more information."
                : $"Your teacher account was rejected by the admin. Reason: {user.RejectionReason}");

        throw new AppException(403, "Your teacher account is pending admin approval. Please wait until an admin confirms and approves your account.");
    }

    public async Task<VerifyOtpResponse> AdminLoginAsync(string email, string password)
    {
        if (!email.Contains('@'))
            throw new AppException(400, "Please enter your email address");

        using var conn = _db.CreateConnection();

        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"email\" = @Email AND \"role\" = 'admin'",
            new { Email = email });

        if (user == null)
            throw new AppException(401, "Invalid admin credentials");

        if (!IsValidBcryptHash(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new AppException(401, "Invalid admin credentials");

        var token = _jwt.GenerateToken(user.Id, user.Role, user.TokenVersion);

        return new VerifyOtpResponse
        {
            Token = token,
            User = UserToInfo(user)
        };
    }

    public async Task<GenerateOtpResponse> GenerateOtpAsync(string identifier, string? role, string? password)
    {
        if (!identifier.Contains('@'))
            throw new AppException(400, "Please use your email address to login");

        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var user = await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM \"Users\" WHERE \"email\" = @Email", new { Email = identifier }, tx);

            if (user == null && !string.IsNullOrEmpty(role))
            {
                if (role != "teacher" && role != "student")
                    throw new AppException(400, "Invalid role");

                var hash = BCrypt.Net.BCrypt.HashPassword(password);
                var newUser = new User
                {
                    Name = identifier.Split('@')[0],
                    Role = role,
                    PasswordHash = hash,
                    Email = identifier,
                    ApprovalStatus = role == "teacher" ? "pending" : "approved",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                var sql = @"INSERT INTO ""Users"" (""name"", ""role"", ""password_hash"", ""email"", ""approval_status"", ""created_at"", ""updated_at"")
                    VALUES (@Name, @Role, @PasswordHash, @Email, @ApprovalStatus, @CreatedAt, @UpdatedAt) RETURNING *";
                user = await conn.QuerySingleAsync<User>(sql, newUser, tx);
                await EnsureStudentIdAsync(conn, user);

                if (user.Role == "teacher")
                {
                    await NotifyAdminsAsync(conn, "New teacher registration",
                        $"A new teacher ({user.Name}, {user.Email}) registered and is awaiting your approval.");
                    tx.Commit();
                    throw new AppException(403, "Your teacher account has been created and is pending admin approval. Please wait until an admin confirms your account.");
                }
            }

            if (user == null)
                throw new AppException(404, "User not found. Please contact admin.");

            if (!string.IsNullOrEmpty(role) && user.Role != role)
                throw new AppException(403, "Role mismatch. Please use the correct login method.");

            EnsureTeacherApproved(user);

            if (!string.IsNullOrEmpty(password))
            {
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    throw new AppException(400, "No password is set for this account. Please use the 'Forgot Password' option to set one.");
                }
                if (!IsValidBcryptHash(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                    throw new AppException(401, "Invalid password");
            }

            var otpCode = _otp.GenerateOtp();
            var expiresAt = DateTime.UtcNow.AddMinutes(5);
            await conn.ExecuteAsync(
                "INSERT INTO \"OtpTokens\" (\"user_id\", \"otp_code\", \"expires_at\", \"is_used\", \"created_at\", \"updated_at\") VALUES (@UserId, @OtpCode, @ExpiresAt, false, @Now, @Now)",
                new { UserId = user.Id, OtpCode = otpCode, ExpiresAt = expiresAt, Now = DateTime.UtcNow }, tx);

            tx.Commit();

            var otpEmailHtml = "<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'><h2 style='color:#333;'>EdTech Examination App</h2><p>Your verification code is:</p><div style='background:#f4f4f4;padding:20px;text-align:center;border-radius:8px;margin:20px 0;'><span style='font-size:32px;font-weight:bold;color:#007bff;letter-spacing:8px;'>" + otpCode + "</span></div><p style='color:#666;font-size:14px;'>This code will expire in 5 minutes.<br>Do not share this code with anyone.</p></div>";

            var sendResult = await _email.SendEmailAsync(user.Email!, "Your OTP Code - EdTech Examination App", otpEmailHtml);

            var emailDelivered = sendResult.Status == "sent";

            return new GenerateOtpResponse
            {
                Success = true,
                Message = emailDelivered
                    ? "OTP sent successfully. Please check your email."
                    : "OTP sent. If email is not configured, check server logs or use the debug endpoint.",
                UserId = user.Id,
                Identifier = identifier,
                OtpCode = null
            };
        }
        catch
        {
            try
            {
                tx.Rollback();
            }
            catch
            {
                // Transaction was already committed (e.g. a new teacher account was
                // created and committed before the pending-approval notice is thrown).
            }
            throw;
        }
    }

    public async Task<VerifyOtpResponse> VerifyOtpAsync(string identifier, string otpCode, string? role)
    {
        using var conn = _db.CreateConnection();

        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"email\" = @Email", new { Email = identifier });

        if (user == null) throw new AppException(404, "User not found");
        if (!string.IsNullOrEmpty(role) && user.Role != role) throw new AppException(403, "Role mismatch");
        EnsureTeacherApproved(user);

        var now = DateTime.UtcNow;
        var affected = await conn.ExecuteAsync(
            @"UPDATE ""OtpTokens"" SET ""is_used"" = true, ""updated_at"" = @Now
              WHERE ""user_id"" = @UserId AND ""otp_code"" = @OtpCode AND ""is_used"" = false AND ""expires_at"" > @Now",
            new { UserId = user.Id, OtpCode = otpCode, Now = now });

        if (affected == 0)
        {
            var expired = await conn.QueryFirstOrDefaultAsync<OtpToken>(
                "SELECT * FROM \"OtpTokens\" WHERE \"user_id\" = @UserId AND \"otp_code\" = @OtpCode AND \"is_used\" = true",
                new { UserId = user.Id, OtpCode = otpCode });
            throw new AppException(400, expired != null ? "OTP has expired" : "Invalid or expired OTP");
        }

        await EnsureStudentIdAsync(conn, user);

        var token = _jwt.GenerateToken(user.Id, user.Role, user.TokenVersion);

        return new VerifyOtpResponse
        {
            Token = token,
            User = UserToInfo(user)
        };
    }

    public async Task<RegisterOtpResponse> SendRegisterOtpAsync(string name, string identifier, string password, string role)
    {
        if (!identifier.Contains('@'))
            throw new AppException(400, "Please use your email address to register");

        using var conn = _db.CreateConnection();

        var existing = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"email\" = @Email", new { Email = identifier });
        if (existing != null)
            throw new AppException(409, "An account with this email already exists");

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var otpCode = _otp.GenerateOtp();
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        await conn.ExecuteAsync(
            "DELETE FROM \"PendingRegistrations\" WHERE \"identifier\" = @Id AND \"is_used\" = false",
            new { Id = identifier });

        await conn.ExecuteAsync(
            @"INSERT INTO ""PendingRegistrations"" (""name"", ""identifier"", ""password_hash"", ""role"", ""otp_code"", ""expires_at"", ""is_used"", ""created_at"")
              VALUES (@Name, @Identifier, @PasswordHash, @Role, @OtpCode, @ExpiresAt, false, @Now)",
            new { Name = name, Identifier = identifier, PasswordHash = hash, Role = role, OtpCode = otpCode, ExpiresAt = expiresAt, Now = DateTime.UtcNow });

        var otpEmailHtml = $@"<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
            <h2 style='color:#333;'>EdTech Examination App - Registration</h2>
            <p>Your verification code is:</p>
            <div style='background:#f4f4f4;padding:20px;text-align:center;border-radius:8px;margin:20px 0;'>
              <span style='font-size:32px;font-weight:bold;color:#007bff;letter-spacing:8px;'>{otpCode}</span>
            </div>
            <p style='color:#666;font-size:14px;'>This code will expire in 5 minutes.<br>Do not share this code with anyone.</p>
          </div>";

        var sendResult = await _email.SendEmailAsync(identifier, "Your OTP Code - EdTech Examination App", otpEmailHtml);

        var emailDelivered = sendResult.Status == "sent";

        return new RegisterOtpResponse
        {
            Success = true,
            Message = emailDelivered ? "OTP sent successfully. Please check your email." : "OTP sent. If email is not configured, check server logs.",
            OtpCode = null
        };
    }

    public async Task<VerifyOtpResponse> VerifyRegisterOtpAsync(string identifier, string otpCode)
    {
        using var conn = _db.CreateConnection();

        var now = DateTime.UtcNow;
        var affected = await conn.ExecuteAsync(
            @"UPDATE ""PendingRegistrations"" SET ""is_used"" = true
              WHERE ""identifier"" = @Id AND ""otp_code"" = @OtpCode AND ""is_used"" = false AND ""expires_at"" > @Now",
            new { Id = identifier, OtpCode = otpCode, Now = now });

        if (affected == 0)
        {
            var expired = await conn.QueryFirstOrDefaultAsync<PendingRegistration>(
                "SELECT * FROM \"PendingRegistrations\" WHERE \"identifier\" = @Id AND \"otp_code\" = @OtpCode AND \"is_used\" = true",
                new { Id = identifier, OtpCode = otpCode });
            throw new AppException(400, expired != null ? "OTP has expired. Please register again." : "Invalid OTP. Please start registration again.");
        }

        var pending = await conn.QueryFirstOrDefaultAsync<PendingRegistration>(
            "SELECT * FROM \"PendingRegistrations\" WHERE \"identifier\" = @Id AND \"is_used\" = true AND \"otp_code\" = @OtpCode",
            new { Id = identifier, OtpCode = otpCode });

        if (pending == null)
            throw new AppException(400, "No pending registration found. Please start registration again.");

        var now2 = DateTime.UtcNow;
        var isTeacher = pending.Role == "teacher";
        var user = new User
        {
            Name = pending.Name,
            Role = pending.Role,
            PasswordHash = pending.PasswordHash ?? "",
            Email = pending.Email ?? pending.Identifier,
            ApprovalStatus = isTeacher ? "pending" : "approved",
            CreatedAt = now2,
            UpdatedAt = now2
        };

        user = await conn.QuerySingleAsync<User>(
            @"INSERT INTO ""Users"" (""name"", ""role"", ""password_hash"", ""email"", ""approval_status"", ""created_at"", ""updated_at"")
              VALUES (@Name, @Role, @PasswordHash, @Email, @ApprovalStatus, @CreatedAt, @UpdatedAt) RETURNING *",
            user);

        await EnsureStudentIdAsync(conn, user);

        if (isTeacher)
        {
            await NotifyAdminsAsync(conn, "New teacher registration",
                $"A new teacher ({user.Name}, {user.Email}) registered and is awaiting your approval. Review it in the Admin panel.");
            return new VerifyOtpResponse
            {
                Token = "",
                PendingApproval = true,
                ApprovalStatus = "pending",
                User = UserToInfo(user)
            };
        }

        var token = _jwt.GenerateToken(user.Id, user.Role, user.TokenVersion);

        return new VerifyOtpResponse
        {
            Token = token,
            User = UserToInfo(user)
        };
    }

    public async Task<object> ForgotPasswordAsync(string identifier)
    {
        if (!identifier.Contains('@'))
            throw new AppException(400, "Please enter your email address");

        using var conn = _db.CreateConnection();

        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"email\" = @Email", new { Email = identifier });

        if (user == null)
            throw new AppException(404, "No account found with this email");

        var otpCode = _otp.GenerateOtp();
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        await conn.ExecuteAsync(
            "DELETE FROM \"PendingRegistrations\" WHERE \"identifier\" = @Id AND \"is_used\" = false",
            new { Id = identifier });

        await conn.ExecuteAsync(
            @"INSERT INTO ""PendingRegistrations"" (""name"", ""identifier"", ""password_hash"", ""role"", ""otp_code"", ""expires_at"", ""is_used"", ""created_at"")
              VALUES (@Name, @Identifier, '', @Role, @OtpCode, @ExpiresAt, false, @Now)",
            new { Name = user.Name, Identifier = identifier, Role = user.Role, OtpCode = otpCode, ExpiresAt = expiresAt, Now = DateTime.UtcNow });

        var otpEmailHtml = $@"<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
            <h2 style='color:#333;'>EdTech Examination App - Reset Password</h2>
            <p>Your password reset code is:</p>
            <div style='background:#f4f4f4;padding:20px;text-align:center;border-radius:8px;margin:20px 0;'>
              <span style='font-size:32px;font-weight:bold;color:#007bff;letter-spacing:8px;'>{otpCode}</span>
            </div>
            <p style='color:#666;font-size:14px;'>This code will expire in 5 minutes.<br>Do not share this code with anyone.</p>
          </div>";

        await _email.SendEmailAsync(identifier, "Reset Your Password - EdTech Examination App", otpEmailHtml);

        return new { success = true, message = "OTP sent successfully" };
    }

    public async Task<object> ResetPasswordAsync(string identifier, string otpCode, string newPassword)
    {
        using var conn = _db.CreateConnection();

        var now = DateTime.UtcNow;
        var affected = await conn.ExecuteAsync(
            @"UPDATE ""PendingRegistrations"" SET ""is_used"" = true
              WHERE ""identifier"" = @Id AND ""otp_code"" = @OtpCode AND ""is_used"" = false AND ""expires_at"" > @Now",
            new { Id = identifier, OtpCode = otpCode, Now = now });

        if (affected == 0)
        {
            var expired = await conn.QueryFirstOrDefaultAsync<PendingRegistration>(
                "SELECT * FROM \"PendingRegistrations\" WHERE \"identifier\" = @Id AND \"otp_code\" = @OtpCode AND \"is_used\" = true",
                new { Id = identifier, OtpCode = otpCode });
            throw new AppException(400, expired != null ? "OTP has expired. Please request again." : "Invalid OTP. Please try again.");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await conn.ExecuteAsync(
            "UPDATE \"Users\" SET \"password_hash\" = @Hash, \"updated_at\" = @Now WHERE \"email\" = @Email",
            new { Hash = hash, Now = DateTime.UtcNow, Email = identifier });

        return new { success = true, message = "Password updated successfully" };
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(string oldToken)
    {
        var decoded = _jwt.DecodeToken(oldToken);
        if (decoded == null)
            throw new AppException(401, "Invalid or expired token");

        using var conn = _db.CreateConnection();
        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = decoded.Value.userId });
        if (user == null)
            throw new AppException(404, "User not found");

        var newVersion = user.TokenVersion + 1;
        await conn.ExecuteAsync(
            "UPDATE \"Users\" SET \"token_version\" = @Version, \"updated_at\" = @Now WHERE \"id\" = @Id",
            new { Version = newVersion, Now = DateTime.UtcNow, Id = user.Id });
        await _cache.RemoveAsync($"user:{user.Id}");

        var newToken = _jwt.GenerateToken(user.Id, user.Role, newVersion);
        var refreshToken = _jwt.GenerateRefreshToken(user.Id, user.Role, newVersion);

        return new RefreshTokenResponse
        {
            Token = newToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<UserInfo> UpdateProfileAsync(int userId, UpdateProfileRequest updates)
    {
        using var conn = _db.CreateConnection();

        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = userId });
        if (user == null) throw new AppException(404, "User not found");

        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", userId);

        if (!string.IsNullOrEmpty(updates.Name)) { setClauses.Add("\"name\" = @Name"); parameters.Add("Name", updates.Name); }
        if (!string.IsNullOrEmpty(updates.Email)) { setClauses.Add("\"email\" = @Email"); parameters.Add("Email", updates.Email); }
        if (!string.IsNullOrEmpty(updates.Phone)) { setClauses.Add("\"phone\" = @Phone"); parameters.Add("Phone", updates.Phone); }

        if (setClauses.Count == 0)
            throw new AppException(400, "No valid fields to update");

        if (!string.IsNullOrEmpty(updates.Email) || !string.IsNullOrEmpty(updates.Phone))
        {
            var existing = await conn.QueryFirstOrDefaultAsync<User>(
                @"SELECT * FROM ""Users"" WHERE (""email"" = @Email OR ""phone"" = @Phone) AND ""id"" != @Id",
                new { Email = updates.Email ?? "", Phone = updates.Phone ?? "", Id = userId });
            if (existing != null)
                throw new AppException(409, "Email or phone already in use");
        }

        setClauses.Add("\"updated_at\" = @Now");
        parameters.Add("Now", DateTime.UtcNow);

        var sql = $"UPDATE \"Users\" SET {string.Join(", ", setClauses)} WHERE \"id\" = @Id RETURNING *";
        user = await conn.QuerySingleAsync<User>(sql, parameters);
        await _cache.RemoveAsync($"user:{userId}");

        return UserToInfo(user);
    }

    public async Task<object> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        using var conn = _db.CreateConnection();

        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = userId });
        if (user == null) throw new AppException(404, "User not found");

        if (!IsValidBcryptHash(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw new AppException(401, "Current password is incorrect");

        var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        var newVersion = user.TokenVersion + 1;
        await conn.ExecuteAsync(
            "UPDATE \"Users\" SET \"password_hash\" = @Hash, \"token_version\" = @Version, \"updated_at\" = @Now WHERE \"id\" = @Id",
            new { Hash = hash, Version = newVersion, Now = DateTime.UtcNow, Id = userId });
        await _cache.RemoveAsync($"user:{userId}");

        return new { success = true, message = "Password updated successfully. Please log in again." };
    }

    public async Task<VerifyOtpResponse> ExternalAuthSessionAsync(string email, string name, string role, string externalUserId)
    {
        using var conn = _db.CreateConnection();

        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"email\" = @Email", new { Email = email });

        if (user == null)
        {
            var jwtSecret = _config["Jwt:Secret"] ?? "";
            var hash = BCrypt.Net.BCrypt.HashPassword(externalUserId + jwtSecret);
            var now = DateTime.UtcNow;
            var isTeacher = role == "teacher";
            user = new User
            {
                Name = name,
                Role = role ?? "student",
                Email = email,
                PasswordHash = hash,
                ApprovalStatus = isTeacher ? "pending" : "approved",
                CreatedAt = now,
                UpdatedAt = now
            };
            user = await conn.QuerySingleAsync<User>(
                @"INSERT INTO ""Users"" (""name"", ""role"", ""email"", ""password_hash"", ""approval_status"", ""created_at"", ""updated_at"")
                  VALUES (@Name, @Role, @Email, @PasswordHash, @ApprovalStatus, @CreatedAt, @UpdatedAt) RETURNING *",
                user);
            await EnsureStudentIdAsync(conn, user);

            if (isTeacher)
            {
                await NotifyAdminsAsync(conn, "New teacher registration",
                    $"A new teacher ({user.Name}, {user.Email}) registered and is awaiting your approval. Review it in the Admin panel.");
                throw new AppException(403, "Your teacher account has been created and is pending admin approval. Please wait until an admin confirms your account.");
            }
        }
        else if (user.Role != role)
        {
            var now = DateTime.UtcNow;
            var isTeacher = role == "teacher";
            user = await conn.QuerySingleAsync<User>(
                @"UPDATE ""Users"" SET ""role"" = @Role, ""name"" = @Name, ""approval_status"" = @ApprovalStatus, ""token_version"" = ""token_version"" + 1, ""updated_at"" = @Now WHERE ""id"" = @Id RETURNING *",
                new { Role = role, Name = name, ApprovalStatus = isTeacher ? "pending" : "approved", Now = now, Id = user.Id });

            if (isTeacher)
            {
                await NotifyAdminsAsync(conn, "New teacher registration",
                    $"A new teacher ({user.Name}, {user.Email}) registered and is awaiting your approval. Review it in the Admin panel.");
                throw new AppException(403, "Your teacher account has been created and is pending admin approval. Please wait until an admin confirms your account.");
            }
        }

        EnsureTeacherApproved(user);
        await EnsureStudentIdAsync(conn, user);

        var token = _jwt.GenerateToken(user.Id, user.Role, user.TokenVersion);

        return new VerifyOtpResponse
        {
            Token = token,
            User = UserToInfo(user)
        };
    }

    public async Task DeleteProfileAsync(int userId)
    {
        using var conn = _db.CreateConnection();

        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = userId });
        if (user == null)
            throw new AppException(404, "User not found");

        await conn.ExecuteAsync("DELETE FROM \"Users\" WHERE \"id\" = @Id", new { Id = userId });
        await _cache.RemoveAsync($"user:{userId}");
    }

    public async Task<string?> GetLatestOtpAsync(string identifier, string type)
    {
        if (!_env.IsDevelopment())
            return null;

        using var conn = _db.CreateConnection();

        if (type == "register" || type == "forgot")
        {
            var pending = await conn.QueryFirstOrDefaultAsync<PendingRegistration>(
                "SELECT \"otp_code\" FROM \"PendingRegistrations\" WHERE \"identifier\" = @Id AND \"is_used\" = false ORDER BY \"created_at\" DESC LIMIT 1",
                new { Id = identifier });
            return pending?.OtpCode;
        }

        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT \"id\" FROM \"Users\" WHERE \"email\" = @Email", new { Email = identifier });
        if (user == null) return null;

        var otp = await conn.QueryFirstOrDefaultAsync<OtpToken>(
            "SELECT \"otp_code\" FROM \"OtpTokens\" WHERE \"user_id\" = @UserId AND \"is_used\" = false ORDER BY \"created_at\" DESC LIMIT 1",
            new { UserId = user.Id });
        return otp?.OtpCode;
    }
}
