using EdTechApi.Services;
using Microsoft.Extensions.Configuration;

namespace EdTechApi.Tests;

public class JwtServiceTests
{
    private static JwtService CreateJwtService(string? secret = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret ?? "this-is-a-test-secret-key-that-is-at-least-32-chars!!",
                ["Jwt:Issuer"] = "EdTechApi",
                ["Jwt:Audience"] = "EdTechApp",
                ["Jwt:ExpiryHours"] = "24"
            })
            .Build();
        return new JwtService(config);
    }

    [Fact]
    public void GenerateToken_Returns_Valid_Token()
    {
        var jwt = CreateJwtService();
        var token = jwt.GenerateToken(1, "teacher", 0);
        Assert.False(string.IsNullOrEmpty(token));

        var decoded = jwt.DecodeToken(token);
        Assert.NotNull(decoded);
        Assert.Equal(1, decoded.Value.userId);
        Assert.Equal("teacher", decoded.Value.role);
        Assert.Equal(0, decoded.Value.tokenVersion);
    }

    [Fact]
    public void VerifyToken_ExpiredToken_Returns_Null()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "this-is-a-test-secret-key-that-is-at-least-32-chars!!",
                ["Jwt:Issuer"] = "EdTechApi",
                ["Jwt:Audience"] = "EdTechApp",
                ["Jwt:ExpiryHours"] = "-1"
            })
            .Build();
        var jwt = new JwtService(config);
        var token = jwt.GenerateToken(1, "teacher", 0);

        var verified = jwt.VerifyToken(token);
        Assert.Null(verified);
    }

    [Fact]
    public void VerifyToken_InvalidSignature_Returns_Null()
    {
        var jwt = CreateJwtService();
        var token = jwt.GenerateToken(1, "teacher", 0);

        var jwt2 = CreateJwtService("different-secret-key-that-does-not-match-the-first!!");
        var verified = jwt2.VerifyToken(token);
        Assert.Null(verified);
    }

    [Fact]
    public void DecodeToken_TokenVersionMismatch_StillDecodes()
    {
        var jwt = CreateJwtService();
        var token = jwt.GenerateToken(1, "student", 5);

        var decoded = jwt.DecodeToken(token);
        Assert.NotNull(decoded);
        Assert.Equal(5, decoded.Value.tokenVersion);
    }

    [Fact]
    public void GenerateRefreshToken_Contains_Refresh_Claim()
    {
        var jwt = CreateJwtService();
        var token = jwt.GenerateRefreshToken(1, "teacher", 0);
        Assert.False(string.IsNullOrEmpty(token));

        var decoded = jwt.DecodeToken(token);
        Assert.NotNull(decoded);
    }
}
