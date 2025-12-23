# Dashfin Web (Next.js)

## Setup

1. `cd web`
2. `npm install`
3. Configure `NEXT_PUBLIC_API_BASE_URL` (see `.env.example`)
4. Run: `npm run dev`

## Auth model

- Access token lives only in memory (JS runtime).
- Refresh token is stored by the API as an httpOnly cookie and is used by `POST /auth/refresh` when the API returns 401.

### Local dev note (cookies)

In `src/Finance.Api/appsettings.json`, `AuthCookies:RefreshTokenSecure=true` requires HTTPS to set the refresh cookie.
For localhost over HTTP you can either:
- run the API over HTTPS, or
- set `RefreshTokenSecure=false` for development.

