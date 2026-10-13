# TOTP Demo — Time-based One-Time Passwords

![C#](https://img.shields.io/badge/C%23-512BD4?logo=dotnet&logoColor=white)
![.NET](https://img.shields.io/badge/.NET_10-512BD4?logo=dotnet&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-7952B3?logo=bootstrap&logoColor=white)
![Font Awesome](https://img.shields.io/badge/Font_Awesome-528DD7?logo=fontawesome&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)

TOTP Demo is a deliberately tiny demo of **Time-based One-Time Passwords** (TOTP, [RFC 6238](https://datatracker.ietf.org/doc/html/rfc6238)): the 6-digit codes from apps like Bitwarden, Google Authenticator, Microsoft Authenticator, Authy and 1Password.

> ⚠️ **Demo only.** There is no database, no login and no user state. The secret is sent to the browser so the page can show what the server calculates. Never do this in a real application.

---

## Features

- **Secret generation**: Creates a random 160-bit secret and an `otpauth://` URI rendered as a QR code.
- **Works with any authenticator app**: Scan the QR code or type the Base32 key in manually.
- **Side-by-side comparison**: Shows the code the server calculates so the audience can match it against their phone.
- **Code verification**: Accepts the current 30-second window plus one either side to tolerate clock drift.
- **Hand-written algorithm**: TOTP and Base32 implemented in about 100 lines with no library, so the whole thing fits on screen.
- **Docker ready**: Multi-stage Alpine build, runs on port 8080.

---

## Tech Stack

- Runtime: .NET 10 (ASP.NET Core minimal API).
- Language: C#.
- Frontend: Single static HTML page with Bootstrap 5 and Font Awesome 6 (via CDN).
- QR codes: [QRCoder](https://github.com/codebude/QRCoder) (the only NuGet dependency).
- Container: Docker (`sdk:10.0-alpine` → `aspnet:10.0-alpine`).

---

## Quick Start

### Prerequisites

- Docker, **or** the .NET 10 SDK
- An authenticator app on a phone

### Run with Docker

```bash
docker build -t totp-demo .
docker run --rm -p 8080:8080 totp-demo
```

Visit: http://localhost:8080

To let people scan from their phones, open the page from another device using your machine's LAN IP, e.g. `http://192.168.1.50:8080`.

### Run without Docker

```bash
cd src/TotpDemo
dotnet run
```

## Demo Script

1. **Create a secret**: type an account name (optional) and click **Generate**.
2. **Scan the QR code** with any authenticator app (or type the key in manually).
3. **Compare**: the *"What the server calculates"* panel shows the same code as the phone, refreshing every 30 seconds. Point out that the phone never talks to the server.
4. **Verify**: type the code from the phone and click **Verify** → ✅. Try a wrong code or an old one (wait ~1 minute) → ❌.

## Repository Structure

```bash
.
├── dockerfile                   # Multi-stage build (sdk:10.0-alpine → aspnet:10.0-alpine)
├── README.md
└── src/TotpDemo
    ├── Program.cs               # API endpoints
    ├── Totp.cs                  # TOTP + Base32 implementation (~100 lines, no library)
    ├── TotpDemo.csproj          # Only dependency: QRCoder (for the QR image)
    └── wwwroot/index.html       # Single page UI (Bootstrap + Font Awesome via CDN)
```

## Architecture Overview

**Frontend**
- One static page served from `wwwroot/`
- Calls the API to create a secret, poll the current code and verify input

**Backend**
- ASP.NET Core minimal API with three endpoints
- Stateless: nothing is stored server-side

### How TOTP Works

| Step | What happens |
|---|---|
| Setup | Server generates a random 160-bit secret, Base32 encodes it, and builds an `otpauth://totp/...` URI rendered as a QR code. |
| Counter | Both sides compute `counter = floor(unix_time / 30)`. |
| Hash | `HMAC-SHA1(secret, counter)` produces a 20-byte hash. |
| Truncate | The last nibble picks an offset; 4 bytes from there → 31-bit integer → `mod 1,000,000` → 6 digits. |
| Verify | The server accepts the current window plus one either side (±30s) to tolerate clock drift. |

Because the code depends only on the shared secret and the current time, the phone works offline. Both clocks just need to be roughly correct.

The algorithm is implemented by hand in [Totp.cs](src/TotpDemo/Totp.cs) for teaching purposes. In production, use a vetted library such as [Otp.NET](https://github.com/kspearrin/Otp.NET).

## API Endpoints

| Endpoint | Method | Description |
|---|---|---|
| /api/setup | POST | Generate a new secret, `otpauth://` URI and QR code |
| /api/code?secret=... | GET | Current code and seconds remaining in the window |
| /api/verify | POST | Check a code against a secret |

**Example Request:**
```json
POST /api/setup
{
  "account": "bob@example.com"
}
```
**Example Response:**
```json
{
  "account": "bob@example.com",
  "issuer": "TOTP Demo",
  "secret": "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP",
  "uri": "otpauth://totp/TOTP%20Demo:bob%40example.com?secret=...&issuer=TOTP%20Demo&algorithm=SHA1&digits=6&period=30",
  "qrCode": "data:image/png;base64,..."
}
```

**Example Request:**
```json
POST /api/verify
{
  "secret": "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP",
  "code": "123456"
}
```
**Example Response:**
```json
{
  "valid": true
}
```

## Troubleshooting

- Bootstrap and Font Awesome load from CDNs, so the browser needs internet access.
- If codes never match, check the clock on the phone and on the Docker host.
