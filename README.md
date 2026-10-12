# TOTP Demo

A deliberately tiny demo of **Time-based One-Time Passwords** (TOTP, [RFC 6238](https://datatracker.ietf.org/doc/html/rfc6238)) — the 6-digit codes from apps like Bitwarden, Google Authenticator, Microsoft Authenticator, Authy and 1Password.

Built with .NET 10 (minimal API), Bootstrap 5 and Font Awesome 6. Runs in Docker.

> ⚠️ **Demo only.** There is no database, no login and no user state. The secret is sent to the browser so the page can show what the server calculates. Never do this in a real application.

## Run it

```bash
docker build -t totp-demo .
docker run --rm -p 8080:8080 totp-demo
```

Open <http://localhost:8080>.

To let people scan from their phones, open the page from another device using your machine's LAN IP, e.g. `http://192.168.1.50:8080`.

### Without Docker

```bash
cd src/TotpDemo
dotnet run
```

## Demo script

1. **Create a secret** – type an account name (optional) and click **Generate**.
2. **Scan the QR code** with any authenticator app (or type the key in manually).
3. **Compare** – the *"What the server calculates"* panel shows the same code as the phone, refreshing every 30 seconds. Point out that the phone never talks to the server.
4. **Verify** – type the code from the phone and click **Verify** → ✅. Try a wrong code or an old one (wait ~1 minute) → ❌.

## How it works

| Step | What happens |
|------|--------------|
| Setup | Server generates a random 160-bit secret, Base32 encodes it, and builds an `otpauth://totp/...` URI rendered as a QR code. |
| Counter | Both sides compute `counter = floor(unix_time / 30)`. |
| Hash | `HMAC-SHA1(secret, counter)` produces a 20-byte hash. |
| Truncate | The last nibble picks an offset; 4 bytes from there → 31-bit integer → `mod 1,000,000` → 6 digits. |
| Verify | The server accepts the current window plus one either side (±30s) to tolerate clock drift. |

Because the code depends only on the shared secret and the current time, the phone works offline. Both clocks just need to be roughly correct.

## Project layout

```
.
├── dockerfile                   # multi-stage build (sdk:10.0-alpine → aspnet:10.0-alpine)
├── README.md
└── src/TotpDemo
    ├── Program.cs               # API endpoints
    ├── Totp.cs                  # TOTP + Base32 implementation (~100 lines, no library)
    ├── TotpDemo.csproj          # only dependency: QRCoder (for the QR image)
    └── wwwroot/index.html       # single page UI (Bootstrap + Font Awesome via CDN)
```

The TOTP algorithm is implemented by hand in [Totp.cs](src/TotpDemo/Totp.cs) so you can show the whole thing on screen. In production, use a vetted library such as [Otp.NET](https://github.com/kspearrin/Otp.NET).

## API

| Method | Path | Body / Query | Returns |
|--------|------|--------------|---------|
| `POST` | `/api/setup` | `{ "account": "bob@example.com" }` | `secret`, `uri`, `qrCode` (PNG data URI) |
| `GET`  | `/api/code?secret=...` | — | current `code`, `secondsRemaining` |
| `POST` | `/api/verify` | `{ "secret": "...", "code": "123456" }` | `{ "valid": true }` |

## Notes

- Bootstrap and Font Awesome load from CDNs, so the browser needs internet access.
- If codes never match, check the clock on the phone and on the Docker host.
