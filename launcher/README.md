# Co-op Shooter Launcher

Fresh launcher foundation for:

- launcher self-updates via `electron-updater`
- game install/update via a remote manifest and downloadable zip
- Supabase auth and `profiles` table integration
- website-driven distribution for friends

## Environment

Create `launcher/.env` from `launcher/.env.example`.

Required for auth:

- `VITE_SUPABASE_URL`
- `VITE_SUPABASE_ANON_KEY`
- `VITE_REQUIRE_BETA_ACCESS`

Required for game install/update:

- `LAUNCHER_GAME_MANIFEST_URL`
- `LAUNCHER_GAME_EXECUTABLE`

Optional:

- `LAUNCHER_WEBSITE_URL`

## Remote Game Manifest

The launcher expects a manifest like this:

```json
{
  "version": "0.1.0",
  "publishedAt": "2026-04-09T00:00:00Z",
  "notes": "Initial playtest build",
  "platforms": {
    "win32": {
      "downloadUrl": "https://your-domain.com/downloads/coop-shooter-0.1.0-win64.zip",
      "launchExecutable": "CoopShooter.exe",
      "fileName": "coop-shooter-0.1.0-win64.zip"
    }
  }
}
```

The zip should extract into the final game folder layout so that `CoopShooter.exe` exists at the install root.

## Supabase Setup

1. Create a Supabase project.
2. Enable email/password auth.
3. Create the `profiles` table:

```sql
create table public.profiles (
  id uuid primary key references auth.users(id) on delete cascade,
  email text,
  display_name text not null default 'Player',
  updated_at timestamptz not null default now()
);

alter table public.profiles enable row level security;

create policy "users can view own profile"
on public.profiles
for select
using (auth.uid() = id);

create policy "users can insert own profile"
on public.profiles
for insert
with check (auth.uid() = id);

create policy "users can update own profile"
on public.profiles
for update
using (auth.uid() = id);
```

## Suggested Hosting Path

- Website download page: host on Vercel, Netlify, or Cloudflare Pages
- Launcher installer: GitHub Releases first, then your own downloads domain later if you want
- Game zip + manifest: Cloudflare R2 behind `cdn.yourdomain.com`
- Access codes: add an `entitlements` or `invites` table later and check it after login before enabling install/play

## Local Feed During Development

You now have a local feed folder at `distribution/game-feed`.

After generating a Windows build into `builds/windows`, package it with:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-game-build.ps1 -Version 0.1.0 -BaseUrl http://localhost:8080
```

That produces:

- `distribution/game-feed/downloads/coop-shooter-<version>-win64.zip`
- `distribution/game-feed/manifests/windows-stable.json`

Point `LAUNCHER_GAME_MANIFEST_URL` to the hosted manifest URL and the launcher will install/update from there.

## Production-Shaped Beta URLs

Recommended split:

- Website: `https://play.yourdomain.com`
- Game manifest/feed: `https://cdn.yourdomain.com/manifests/windows-stable.json`
- Launcher installer: `https://downloads.yourdomain.com/Co-op-Shooter-Launcher-Setup.exe`

The launcher `.env` example now follows that layout.

## Beta Access

The renderer now supports invite-code gating through Supabase:

- sign in
- auto-create/update `profiles`
- check `beta_entitlements`
- redeem an invite code if access is missing
- unlock install and launch once access is granted

Run the SQL in [`supabase/sql/beta_access.sql`](C:\Users\ajsei\Desktop\Projects\co-op-shooter\supabase\sql\beta_access.sql) to create the tables and RPC.

## Notes

- Launcher self-updates only work in packaged builds.
- The current game installer flow is zip-based and Windows-first.
- The launcher already keeps auth separate from entitlement checks so invite-code gating can be added later without rewriting the app shell.
