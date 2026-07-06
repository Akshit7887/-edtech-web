using EdTechApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace EdTechApi.Tests;

public class AuthServiceTests
{
    [Fact]
    public void Constructor_Accepts_IWebHostEnvironment()
    {
        var dbFactory = new Mock<Data.IDbConnectionFactory>();
        var jwt = new Mock<IJwtService>();
        var otp = new Mock<IOtpService>();
        var email = new Mock<IEmailService>();
        var config = new ConfigurationBuilder().Build();
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Development");
        var logger = new Mock<ILogger<AuthService>>();

        var ex = Record.Exception(() => new AuthService(
            dbFactory.Object, jwt.Object, otp.Object, email.Object, config, env.Object, logger.Object));

        Assert.Null(ex);
    }
}
