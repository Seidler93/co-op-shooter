# Local Distribution Feed

This folder is a local stand-in for the hosted files your launcher will eventually download.

## Structure

- `distribution/game-feed/downloads`
- `distribution/game-feed/manifests`

## Workflow

1. Build the Unity game into `builds/windows`
2. Package that build into the feed:

```powershell
cd C:\Users\ajsei\Desktop\Projects\co-op-shooter
powershell -ExecutionPolicy Bypass -File .\launcher\scripts\package-game-build.ps1 -Version 0.1.0 -BaseUrl http://localhost:8080
```

3. Serve `distribution/game-feed` with any static file host
4. Point `LAUNCHER_GAME_MANIFEST_URL` at:

```text
http://localhost:8080/manifests/windows-stable.json
```

## Notes

- The launcher expects the downloaded zip to unpack directly into the install root.
- When you move to real hosting, upload the contents of `distribution/game-feed` to Supabase Storage, Cloudflare R2, Backblaze, or a simple web host/CDN.

## Automated Beta Release

From the launcher folder, run:

```powershell
cd C:\Users\ajsei\Desktop\Projects\co-op-shooter\launcher
npm.cmd run release:beta -- -GameVersion 0.1.2 -LauncherVersion 0.1.14
```

This release path:

- Updates Unity `bundleVersion`
- Builds the Windows game
- Packages `distribution/game-feed/downloads/coop-shooter-<version>-win64.zip`
- Updates `distribution/game-feed/manifests/windows-stable.json`
- Uploads the game zip and manifest to the remote `projectz` R2 bucket
- Builds and uploads the launcher update feed
- Updates and deploys the beta website version labels

If you only want to ship a game update and do not want a launcher rebuild, add `-SkipLauncherBuild`.
If you want a local dry-run without R2 or website changes, add `-SkipGameUpload -SkipLauncherBuild -SkipWebsiteDeploy`.
