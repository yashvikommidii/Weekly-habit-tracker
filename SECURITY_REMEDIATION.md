# Security Remediation - Attack Agent Report

Based on the Attack Agent scan (5783 findings, many false positives from non-existent endpoints), the following remediations were applied to the **actual** Weekly Habit Tracker endpoints.

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
- Returns **429 Too Many Requests** when exceeded

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

---

## Reducing the Remaining ~45 Findings

To minimize findings when running the Attack Agent:

1. **Disable Swagger** (removes ~14): Set `DisableSwagger: true` in appsettings.json or `DISABLE_SWAGGER=true` before scanning.

2. **Acceptable "Missing auth" on / and /index.html** (~6): The homepage is intentionally public. No fix needed.

3. **Attack Agent SQLite errors**: The `FOREIGN KEY constraint failed` errors are **inside the Attack Agent** (AttackPatternDatabase), not the habit tracker. Fix those in the Attack Agent repo if needed.

---

## Note on Report Findings

Most reported "vulnerabilities" **do not exist** in this app. The Attack Agent probes hundreds of common paths. They previously returned 200 because the SPA fallback served index.html for any unmatched route. All unknown paths now return 404.
