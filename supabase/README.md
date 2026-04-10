# Supabase Beta Access

Use [`beta_access.sql`](C:\Users\ajsei\Desktop\Projects\co-op-shooter\supabase\sql\beta_access.sql) in the Supabase SQL editor.

It creates:

- `profiles`
- `invite_codes`
- `beta_entitlements`
- `redeem_invite_code(input_code text)` RPC

## Intended launcher flow

1. User signs in
2. Launcher creates or updates `profiles`
3. Launcher checks `beta_entitlements`
4. If missing, user redeems a code through `redeem_invite_code`
5. Launcher enables install and play

## Creating invite codes

After running the schema, seed codes like:

```sql
insert into public.invite_codes (code, max_uses, note)
values
  ('BETA-ALPHA-001', 1, 'Private friend test'),
  ('BETA-SQUAD-005', 5, 'Small group weekend playtest');
```

## Suggested domains

- Website: `https://play.yourdomain.com`
- CDN/feed: `https://cdn.yourdomain.com`
- Launcher installer download: `https://downloads.yourdomain.com/Co-op-Shooter-Launcher-Setup.exe`
