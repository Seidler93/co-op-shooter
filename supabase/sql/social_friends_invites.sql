create table if not exists public.friend_links (
  owner_id uuid not null constraint friend_links_owner_id_fkey references public.profiles(id) on delete cascade,
  friend_id uuid not null constraint friend_links_friend_id_fkey references public.profiles(id) on delete cascade,
  created_at timestamptz not null default now(),
  primary key (owner_id, friend_id),
  constraint friend_links_not_self check (owner_id <> friend_id)
);

create table if not exists public.lobby_invites (
  id uuid primary key default gen_random_uuid(),
  sender_id uuid not null constraint lobby_invites_sender_id_fkey references public.profiles(id) on delete cascade,
  recipient_id uuid not null constraint lobby_invites_recipient_id_fkey references public.profiles(id) on delete cascade,
  relay_join_code text not null,
  status text not null default 'pending' check (status in ('pending', 'accepted', 'declined', 'expired')),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create index if not exists friend_links_owner_id_idx
on public.friend_links(owner_id);

create index if not exists lobby_invites_recipient_status_idx
on public.lobby_invites(recipient_id, status, created_at desc);

alter table public.profiles enable row level security;
alter table public.friend_links enable row level security;
alter table public.lobby_invites enable row level security;

revoke select on public.profiles from anon, authenticated;
grant select (id, display_name) on public.profiles to authenticated;
grant insert (id, email, display_name, updated_at) on public.profiles to authenticated;
grant update (email, display_name, updated_at) on public.profiles to authenticated;

grant select, insert, delete on public.friend_links to authenticated;
grant select, insert, update on public.lobby_invites to authenticated;

drop policy if exists "profiles_select_own" on public.profiles;
drop policy if exists "profiles_select_public_names" on public.profiles;
create policy "profiles_select_public_names"
on public.profiles
for select
to authenticated
using (true);

drop policy if exists "profiles_insert_own" on public.profiles;
create policy "profiles_insert_own"
on public.profiles
for insert
to authenticated
with check (auth.uid() = id);

drop policy if exists "profiles_update_own" on public.profiles;
create policy "profiles_update_own"
on public.profiles
for update
to authenticated
using (auth.uid() = id)
with check (auth.uid() = id);

drop policy if exists "friend_links_select_own" on public.friend_links;
create policy "friend_links_select_own"
on public.friend_links
for select
to authenticated
using (auth.uid() = owner_id);

drop policy if exists "friend_links_insert_own" on public.friend_links;
create policy "friend_links_insert_own"
on public.friend_links
for insert
to authenticated
with check (auth.uid() = owner_id);

drop policy if exists "friend_links_delete_own" on public.friend_links;
create policy "friend_links_delete_own"
on public.friend_links
for delete
to authenticated
using (auth.uid() = owner_id);

drop policy if exists "lobby_invites_select_visible" on public.lobby_invites;
create policy "lobby_invites_select_visible"
on public.lobby_invites
for select
to authenticated
using (auth.uid() = sender_id or auth.uid() = recipient_id);

drop policy if exists "lobby_invites_insert_as_sender" on public.lobby_invites;
create policy "lobby_invites_insert_as_sender"
on public.lobby_invites
for insert
to authenticated
with check (auth.uid() = sender_id);

drop policy if exists "lobby_invites_update_as_recipient" on public.lobby_invites;
create policy "lobby_invites_update_as_recipient"
on public.lobby_invites
for update
to authenticated
using (auth.uid() = recipient_id)
with check (auth.uid() = recipient_id);
