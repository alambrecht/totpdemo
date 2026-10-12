using QRCoder;
using TotpDemo;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Generate a new secret + QR code for an authenticator app to scan.
// Demo only: the secret is handed to the browser and nothing is stored server-side.
app.MapPost("/api/setup", (SetupRequest req) =>
{
    var account = string.IsNullOrWhiteSpace(req.Account) ? "demo@example.com" : req.Account.Trim();
    const string issuer = "TOTP Demo";

    var secret = Base32.Encode(Totp.NewSecret());
    var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}" +
              $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}" +
              $"&algorithm=SHA1&digits={Totp.Digits}&period={Totp.StepSeconds}";

    using var qrData = QRCodeGenerator.GenerateQrCode(uri, QRCodeGenerator.ECCLevel.M);
    var png = new PngByteQRCode(qrData).GetGraphic(8);

    return Results.Ok(new
    {
        account,
        issuer,
        secret,
        uri,
        qrCode = $"data:image/png;base64,{Convert.ToBase64String(png)}"
    });
});

// What the server thinks the current code is (so the audience can compare with their phone).
app.MapGet("/api/code", (string secret) =>
{
    var now = DateTimeOffset.UtcNow;
    return Results.Ok(new
    {
        code = Totp.Compute(Base32.Decode(secret), now),
        secondsRemaining = Totp.SecondsRemaining(now),
        period = Totp.StepSeconds
    });
});

app.MapPost("/api/verify", (VerifyRequest req) =>
{
    var code = (req.Code ?? "").Replace(" ", "");
    var valid = code.Length == Totp.Digits && code.All(char.IsDigit)
                && Totp.Verify(Base32.Decode(req.Secret), code, DateTimeOffset.UtcNow);
    return Results.Ok(new { valid });
});

app.Run();

record SetupRequest(string? Account);
record VerifyRequest(string Secret, string? Code);
