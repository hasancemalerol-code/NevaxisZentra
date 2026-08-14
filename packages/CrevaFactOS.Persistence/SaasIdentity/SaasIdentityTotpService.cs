using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CrevaFactOS.Persistence.SaasIdentity;

public static class SaasIdentityTotpService
{
    private const int SecretByteLength = 20;
    private const int RecoveryCodeByteLength = 8;
    private const int DefaultTimeStepSeconds = 30;
    private const int DefaultDigits = 6;
    private const int VerificationWindowSteps = 1;

    public static string GenerateSecret()
    {
        var secretBytes = RandomNumberGenerator.GetBytes(SecretByteLength);
        return Base32Encode(secretBytes);
    }

    public static bool VerifyCode(string secret, string code, DateTimeOffset? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalizedCode = code.Trim();
        if (normalizedCode.Length != DefaultDigits || !normalizedCode.All(char.IsDigit))
        {
            return false;
        }

        var counterBase = GetCounter(utcNow ?? DateTimeOffset.UtcNow);
        for (var offset = -VerificationWindowSteps; offset <= VerificationWindowSteps; offset++)
        {
            var expected = ComputeTotp(secret, counterBase + offset);
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expected),
                    Encoding.UTF8.GetBytes(normalizedCode)))
            {
                return true;
            }
        }

        return false;
    }

    public static string BuildProvisioningUri(string secret, string accountEmail, string issuer)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedAccount = Uri.EscapeDataString(accountEmail);
        return $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={secret}&issuer={encodedIssuer}&digits={DefaultDigits}&period={DefaultTimeStepSeconds}";
    }

    public static IReadOnlyList<string> GenerateRecoveryCodes(int count = 10)
    {
        var codes = new List<string>(count);
        for (var index = 0; index < count; index++)
        {
            var bytes = RandomNumberGenerator.GetBytes(RecoveryCodeByteLength);
            codes.Add(Convert.ToHexString(bytes).ToLowerInvariant());
        }

        return codes;
    }

    private static long GetCounter(DateTimeOffset utcNow) =>
        utcNow.ToUnixTimeSeconds() / DefaultTimeStepSeconds;

#pragma warning disable CA5350 // RFC 6238 TOTP requires HMAC-SHA1.
    private static string ComputeTotp(string secret, long counter)
    {
        var secretBytes = Base32Decode(secret);
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        var hash = HMACSHA1.HashData(secretBytes, counterBytes);
        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        var otp = binaryCode % (int)Math.Pow(10, DefaultDigits);
        return otp.ToString(new string('0', DefaultDigits), CultureInfo.InvariantCulture);
    }
#pragma warning restore CA5350

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;

            while (bitsLeft >= 5)
            {
                var index = (buffer >> (bitsLeft - 5)) & 0x1F;
                bitsLeft -= 5;
                output.Append(alphabet[index]);
            }
        }

        if (bitsLeft > 0)
        {
            var index = (buffer << (5 - bitsLeft)) & 0x1F;
            output.Append(alphabet[index]);
        }

        return output.ToString();
    }

    private static byte[] Base32Decode(string encoded)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var normalized = encoded.Trim().TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>(normalized.Length * 5 / 8);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var character in normalized)
        {
            var value = alphabet.IndexOf(character);
            if (value < 0)
            {
                throw new FormatException("Invalid Base32 character.");
            }

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output.Add((byte)((buffer >> (bitsLeft - 8)) & 0xFF));
                bitsLeft -= 8;
            }
        }

        return output.ToArray();
    }
}
