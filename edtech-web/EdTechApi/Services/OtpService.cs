namespace EdTechApi.Services;

public interface IOtpService
{
    string GenerateOtp(int length = 6);
    bool ValidateOtpFormat(string otp, int expectedLength = 6);
}

public class OtpService : IOtpService
{
    private static readonly Random _random = new();

    public string GenerateOtp(int length = 6)
    {
        var chars = "0123456789";
        var result = new char[length];
        lock (_random)
        {
            for (int i = 0; i < length; i++)
                result[i] = chars[_random.Next(chars.Length)];
        }
        return new string(result);
    }

    public bool ValidateOtpFormat(string otp, int expectedLength = 6)
    {
        if (string.IsNullOrEmpty(otp)) return false;
        return otp.Length == expectedLength && otp.All(char.IsDigit);
    }
}
