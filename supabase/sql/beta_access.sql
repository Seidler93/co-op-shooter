create table if not exists public.profiles (
  id uuid primary key references auth.users(id) on delete cascade,
  email text,
  display_name text not null default 'Player',
  updated_at timestamptz not null default now()
);

create table if not exists public.invite_codes (
  code text primary key,
  created_by uuid references auth.users(id) on delete set null,
  max_uses integer not null default 1 check (max_uses > 0),
  uses_count integer not null default 0 check (uses_count >= 0),
  is_active boolean not null default true,
  expires_at timestamptz,
  note text,
  created_at timestamptz not null default now()
);

create table if not exists public.beta_entitlements (
  user_id uuid primary key references auth.users(id) on delete cascade,
  invite_code text references public.invite_codes(code) on delete set null,
  granted_at timestamptz not null default now()
);

alter table public.profiles enable row level security;
alter table public.invite_codes enable row level security;
alter table public.beta_entitlements enable row level security;

drop policy if exists "profiles_select_own" on public.profiles;
create policy "profiles_select_own"
on public.profiles
for select
using (auth.uid() = id);

drop policy if exists "profiles_insert_own" on public.profiles;
create policy "profiles_insert_own"
on public.profiles
for insert
with check (auth.uid() = id);

drop policy if exists "profiles_update_own" on public.profiles;
create policy "profiles_update_own"
on public.profiles
for update
using (auth.uid() = id);

drop policy if exists "beta_entitlements_select_own" on public.beta_entitlements;
create policy "beta_entitlements_select_own"
on public.beta_entitlements
for select
using (auth.uid() = user_id);

drop function if exists public.redeem_invite_code(text);
create or replace function public.redeem_invite_code(input_code text)
returns table(success boolean, message text)
language plpgsql
security definer
set search_path = public
as $$
declare
  normalized_code text := upper(trim(input_code));
  current_user_id uuid := auth.uid();
  selected_code public.invite_codes%rowtype;
begin
  if current_user_id is null then
    return query select false, 'You must be signed in to redeem a code.';
    return;
  end if;

  if normalized_code is null or normalized_code = '' then
    return query select false, 'Enter an invite code.';
    return;
  end if;

  if exists (
    select 1
    from public.beta_entitlements ent
    where ent.user_id = current_user_id
  ) then
    return query select true, 'Beta access is already active on this account.';
    return;
  end if;

  select *
  into selected_code
  from public.invite_codes
  where code = normalized_code
  for update;

  if not found then
    return query select false, 'Invite code not found.';
    return;
  end if;

  if not selected_code.is_active then
    return query select false, 'That invite code is no longer active.';
    return;
  end if;

  if selected_code.expires_at is not null and selected_code.expires_at <= now() then
    return query select false, 'That invite code has expired.';
    return;
  end if;

  if selected_code.uses_count >= selected_code.max_uses then
    return query select false, 'That invite code has already been fully used.';
    return;
  end if;

  insert into public.beta_entitlements (user_id, invite_code)
  values (current_user_id, normalized_code);

  update public.invite_codes
  set uses_count = uses_count + 1
  where code = normalized_code;

  return query select true, 'Invite code redeemed successfully.';
end;
$$;

grant execute on function public.redeem_invite_code(text) to authenticated;

comment on function public.redeem_invite_code(text)
is 'Redeems a one-time or limited-use invite code for the signed-in user and grants beta entitlement.';
