using System.Security.Cryptography;

namespace TotpDemo;

/// <summary>
/// Minimal RFC 6238 TOTP (HMAC-SHA1, 6 digits, 30 second step) — the defaults
/// every common authenticator app (Google, Microsoft, Authy, 1Password...) uses.
/// </summary>
public static class Totp
{
    public const int Digits = 6;
    public const int StepSeconds = 30;

    public static byte[] NewSecret() => RandomNumberGenerator.GetBytes(20); // 160 bits

    public static string Compute(byte[] secret, DateTimeOffset time)
        => ComputeForCounter(secret, time.ToUnixTimeSeconds() / StepSeconds);

    public static int SecondsRemaining(DateTimeOffset time)
        => StepSeconds - (int)(time.ToUnixTimeSeconds() % StepSeconds);

    /// <summary>Checks the code against the current step, plus one step either side for clock drift.</summary>
    public static bool Verify(byte[] secret, string code, DateTimeOffset time, int window = 1)
    {
        var counter = time.ToUnixTimeSeconds() / StepSeconds;
        for (var i = -window; i <= window; i++)
        {
            var expected = ComputeForCounter(secret, counter + i);
            if (CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.ASCII.GetBytes(expected),
                    System.Text.Encoding.ASCII.GetBytes(code)))
                return true;
        }
        return false;
    }

    // RFC 4226 HOTP: HMAC the 8-byte big-endian counter, then "dynamic truncation".
    private static string ComputeForCounter(byte[] secret, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);

        var hash = HMACSHA1.HashData(secret, counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                   | (hash[offset + 1] << 16)
                   | (hash[offset + 2] << 8)
                   | hash[offset + 3];

        return (binary % (int)Math.Pow(10, Digits)).ToString().PadLeft(Digits, '0');
    }
}

/// <summary>RFC 4648 Base32 — the format authenticator apps expect secrets in.</summary>
public static class Base32
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Encode(byte[] data)
    {
        var sb = new System.Text.StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }
        if (bits > 0) sb.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        return sb.ToString();
    }

    public static byte[] Decode(string input)
    {
        input = input.Trim().TrimEnd('=').Replace(" ", "").ToUpperInvariant();
        var output = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in input)
        {
            var val = Alphabet.IndexOf(c);
            if (val < 0) throw new FormatException($"Invalid Base32 character '{c}'.");
            buffer = (buffer << 5) | val;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)(buffer >> (bits - 8)));
                bits -= 8;
            }
        }
        return output.ToArray();
    }
}
