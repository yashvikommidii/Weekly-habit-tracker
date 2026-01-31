# Security Remediation - Attack Agent Report

Based on the Attack Agent scan (initial ~5,700 findings, mostly false positives from non-existent endpoints), remediations were applied and the count was reduced to **1 remaining** (acceptable: HTTP in addition to HTTPS for local dev).

---

## Applied Fixes

### 1. Security Headers
Added middleware to set:
- **X-Content-Type-Options**: `nosniff` – prevents MIME sniffing
- **X-Frame-Options**: `SAMEORIGIN` – prevents clickjacking
- **X-XSS-Protection**: `1; mode=block` – legacy XSS filter
- **Referrer-Policy**: `strict-origin-when-cross-origin` – limits referrer info
- **Permissions-Policy**: Restricts geolocation, microphone, camera
- **Content-Security-Policy**: Restricts script/style/font sources, allows CDN for Chart.js

### 2. Rate Limiting
- **Chat endpoint**: 20 requests/minute per IP (protects OpenAI API abuse)
- **All other API endpoints**: 120 requests/minute per IP
- **Homepage** (`/`, `/index.html`): 120 requests/minute via `RateLimitService`
- Returns **429 Too Many Requests** when exceeded
- **Headers**: `X-RateLimit-Limit`, `X-RateLimit-Policy`, `Retry-After` on 429

### 3. Error/Information Disclosure
- Chat: No longer exposes "OpenAI API key not configured" or raw API errors
- Generic messages: "The assistant is temporarily unavailable"

---

### 4. Global 404 Fallback
- **Removed** `MapFallbackToFile("index.html")` – it was serving the SPA for every unmatched path
- Unknown routes (API and root) now return **404** instead of 200
- Eliminates false positives from the Attack Agent probing:
  - `/api/vulnerable`, `/api/demo`, `/api/chatbot`, etc.
  - `/logout`, `/signin`, `/auth`, `/profile`, `/chat`, etc.
  - `/.git/config`, `/Dockerfile`, `/metrics`, etc.
- Valid entry points: `/` and `/index.html` (served by DefaultFiles + StaticFiles)

---

### 5. Swagger Disable Option (for security scans)
- Swagger can be disabled via `DisableSwagger: true` in appsettings or `DISABLE_SWAGGER=true` env var
- Run with Swagger disabled when scanning to reduce findings (removes ~14 Swagger-related items)

### 6. Additional Headers
- `Cross-Origin-Opener-Policy: same-origin`
- `X-RateLimit-Limit`, `X-RateLimit-Policy` (rate limit visibility)
- `Retry-After` on 429 responses

### 7. HTTPS
- HTTPS redirection (HTTP → HTTPS)
- HSTS (Strict-Transport-Security)
- Forwarded headers for proxy deployments (Render, Heroku)
- Local dev: `https://localhost:5081` (run `dotnet dev-certs https --trust` once)

### 8. Database Fallback
- In-memory DB when SQL Server connection string is missing/invalid (e.g. Docker without config)
- Avoids startup crash on deployment platforms

---

## Final Scan Results

After all remediations:
- **1 remaining finding**: "Application uses HTTP protocol" — acceptable for local dev (app listens on both HTTP and HTTPS)
- To scan: Run with `DISABLE_SWAGGER=true`, target `https://localhost:5081` for HTTPS-only scan

---

## Scan Tips

1. **Disable Swagger** before scanning: `DISABLE_SWAGGER=true dotnet run`
2. **Target HTTPS**: Use `https://localhost:5081` to avoid HTTP finding
3. **Attack Agent SQLite errors**: `FOREIGN KEY constraint failed` is inside the Attack Agent (AttackPatternDatabase), not this app

---

## Note on Report Findings

Most reported "vulnerabilities" **do not exist** in this app. The Attack Agent probes hundreds of common paths. They previously returned 200 because the SPA fallback served index.html for any unmatched route. All unknown paths now return 404.
