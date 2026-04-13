# Beta Website

This folder is a simple static download page intended for Cloudflare Pages.

## Current beta URL

- Website: `https://still-block-a68a.ajseidler0526.workers.dev/`
- R2 bucket: `https://pub-72a26ee483c14eb6b975bbb15ed9ba81.r2.dev`

## Recommended live URLs

- Website: `https://play.yourdomain.com`
- CDN / launcher feed: `https://cdn.yourdomain.com`
- Launcher installer direct download: `https://downloads.yourdomain.com/Co-op-Shooter-Launcher-Setup.exe`

## Deploy

Deploy this folder with Cloudflare Wrangler:

```powershell
cd website
npx wrangler deploy
```

If Wrangler asks you to log in, run:

```powershell
npx wrangler login
```

Then update:

- `Download Launcher` link in `index.html`
- support/contact text if you want a Discord or email listed

## Launcher Update Feed

After building a new launcher release, upload the update files to R2:

```powershell
cd ..\launcher
npm run publish:launcher:r2
```

This uploads `latest.yml`, the current installer, and its `.blockmap` to the `projectz` R2 bucket root used by `LAUNCHER_UPDATE_BASE_URL`.

## Beta flow

1. Tester lands on website
2. Downloads launcher
3. Signs in
4. Redeems invite code
5. Installs the latest build from the launcher CDN manifest
