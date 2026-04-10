import { useEffect, useState } from "react";
import { createClient } from "@supabase/supabase-js";

function getSupabaseClient() {
  const url = import.meta.env.VITE_SUPABASE_URL;
  const anonKey = import.meta.env.VITE_SUPABASE_ANON_KEY;

  if (!url || !anonKey) {
    return null;
  }

  return createClient(url, anonKey, {
    auth: {
      persistSession: true,
      autoRefreshToken: true,
    },
  });
}

const supabase = getSupabaseClient();
const requiresBetaAccess = import.meta.env.VITE_REQUIRE_BETA_ACCESS !== "false";

function AuthCard({
  session,
  profile,
  entitlement,
  authMessage,
  onSignIn,
  onSignUp,
  onSignOut,
  onSaveProfile,
  onRedeemCode,
}) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState(profile?.display_name || "");
  const [inviteCode, setInviteCode] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setDisplayName(profile?.display_name || "");
  }, [profile?.display_name]);

  async function handleAuth(action) {
    setBusy(true);
    try {
      if (action === "signin") {
        await onSignIn(email, password);
      } else {
        await onSignUp(email, password);
      }
    } finally {
      setBusy(false);
    }
  }

  async function handleProfileSave() {
    setBusy(true);
    try {
      await onSaveProfile(displayName);
    } finally {
      setBusy(false);
    }
  }

  async function handleRedeemCode() {
    setBusy(true);
    try {
      await onRedeemCode(inviteCode.trim().toUpperCase());
      setInviteCode("");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="panel auth-panel">
      <div className="panel-header">
        <p className="eyebrow">Account</p>
        <h2>Identity and access</h2>
      </div>

      {!supabase && (
        <div className="notice warning">
          Set `VITE_SUPABASE_URL` and `VITE_SUPABASE_ANON_KEY` to enable login and profiles.
        </div>
      )}

      {!session ? (
        <div className="auth-grid">
          <label className="field">
            <span>Email</span>
            <input
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              type="email"
              placeholder="squadmate@yourgame.com"
            />
          </label>

          <label className="field">
            <span>Password</span>
            <input
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              type="password"
              placeholder="At least 6 characters"
            />
          </label>

          <div className="button-row">
            <button className="primary" disabled={busy || !supabase} onClick={() => handleAuth("signin")}>
              Sign In
            </button>
            <button className="secondary" disabled={busy || !supabase} onClick={() => handleAuth("signup")}>
              Create Account
            </button>
          </div>

          <p className="muted">
            Access-code gating can be layered on top of this later without changing the launcher flow.
          </p>
        </div>
      ) : (
        <div className="auth-grid">
          <div className="identity-card">
            <div>
              <p className="muted label">Signed in as</p>
              <strong>{session.user.email}</strong>
            </div>
            <button className="ghost" onClick={onSignOut}>
              Sign Out
            </button>
          </div>

          <label className="field">
            <span>Display Name</span>
            <input
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              placeholder="Your callsign"
            />
          </label>

          <div className="button-row">
            <button className="primary" disabled={busy || !supabase} onClick={handleProfileSave}>
              Save Profile
            </button>
          </div>

          {requiresBetaAccess && (
            <>
              <div className={`notice ${entitlement?.hasAccess ? "" : "warning"}`}>
                {entitlement?.hasAccess
                  ? `Beta access granted${entitlement.code ? ` via ${entitlement.code}` : ""}.`
                  : "No beta access on this account yet. Redeem an invite code to unlock install and play."}
              </div>

              {!entitlement?.hasAccess && (
                <>
                  <label className="field">
                    <span>Invite Code</span>
                    <input
                      value={inviteCode}
                      onChange={(event) => setInviteCode(event.target.value)}
                      placeholder="BETA-XXXX"
                    />
                  </label>

                  <div className="button-row">
                    <button className="secondary" disabled={busy || !supabase || !inviteCode.trim()} onClick={handleRedeemCode}>
                      Redeem Code
                    </button>
                  </div>
                </>
              )}
            </>
          )}

          <p className="muted">
            Profile row source: Supabase `profiles` table keyed to `auth.users`.
          </p>
        </div>
      )}

      {authMessage ? <div className="notice">{authMessage}</div> : null}
    </section>
  );
}

function GameCard({
  state,
  installMessage,
  downloadProgress,
  launcherInfo,
  launcherUpdate,
  busy,
  canInstall,
  canPlay,
  onRefresh,
  onInstall,
  onLaunch,
  onOpenInstallDirectory,
  onCheckLauncherUpdate,
  onDownloadLauncherUpdate,
  onQuitAndInstall,
  onOpenWebsite,
}) {
  const installLabel = state?.needsInstall ? "Download Game" : "Update Game";
  const remoteVersion = state?.remote?.version || "Not configured";
  const installedVersion = state?.installed?.installedVersion || "Not installed";
  const progressText =
    downloadProgress?.percent != null ? `${downloadProgress.percent}% downloaded` : "Waiting for size info";

  return (
    <section className="panel game-panel">
      <div className="panel-header spread">
        <div>
          <p className="eyebrow">Launcher</p>
          <h2>Patch, play, repeat</h2>
        </div>
        <div className="version-pill">Launcher v{launcherInfo?.version || "..."}</div>
      </div>

      <div className="hero-card">
        <div className="hero-copy">
          <p className="hero-kicker">Co-op Shooter Command</p>
          <h1>One place to update the launcher, update the game, and jump in.</h1>
          <p className="muted">
            Friends download the launcher from your site, sign in, install the game build, and launch from here.
          </p>
        </div>
        <div className="hero-meta">
          <div className="stat">
            <span>Installed</span>
            <strong>{installedVersion}</strong>
          </div>
          <div className="stat">
            <span>Latest</span>
            <strong>{remoteVersion}</strong>
          </div>
          <div className="stat">
            <span>Channel</span>
            <strong>{launcherInfo?.channel || "stable"}</strong>
          </div>
        </div>
      </div>

      <div className="card-grid">
        <article className="subpanel">
          <p className="eyebrow">Game Build</p>
          <h3>Install state</h3>
          <p className="muted">
            {state?.installed?.canLaunch
              ? `Ready from ${state.installed.installDir}`
              : "No installed build detected yet."}
          </p>

          {state?.updateError ? <div className="notice warning">{state.updateError}</div> : null}
          {installMessage ? <div className="notice">{installMessage}</div> : null}
          {downloadProgress ? <div className="notice">{progressText}</div> : null}

          <div className="button-row">
            <button className="primary" disabled={busy || !state?.config?.manifestConfigured || !canInstall} onClick={onInstall}>
              {installLabel}
            </button>
            <button className="secondary" disabled={busy} onClick={onRefresh}>
              Refresh
            </button>
            <button className="ghost" disabled={busy} onClick={onOpenInstallDirectory}>
              Open Folder
            </button>
          </div>
        </article>

        <article className="subpanel">
          <p className="eyebrow">Play</p>
          <h3>Launch flow</h3>
          <p className="muted">
            Require login before install/play today, then add invite-code access later.
          </p>
          <div className="button-row">
            <button className="primary large" disabled={!canPlay || busy} onClick={onLaunch}>
              Launch Game
            </button>
            <button className="ghost" disabled={busy || !state?.config?.websiteUrl} onClick={onOpenWebsite}>
              Open Website
            </button>
          </div>
        </article>

        <article className="subpanel">
          <p className="eyebrow">Launcher Updates</p>
          <h3>Desktop app</h3>
          <p className="muted">{launcherUpdate?.message || "Check for launcher patches in packaged builds."}</p>
          <div className="button-row">
            <button className="secondary" onClick={onCheckLauncherUpdate}>
              Check Launcher Update
            </button>
            <button
              className="secondary"
              disabled={launcherUpdate?.phase !== "available"}
              onClick={onDownloadLauncherUpdate}
            >
              Download Update
            </button>
            <button
              className="ghost"
              disabled={launcherUpdate?.phase !== "downloaded"}
              onClick={onQuitAndInstall}
            >
              Restart to Install
            </button>
          </div>
        </article>
      </div>
    </section>
  );
}

export default function App() {
  const [launcherInfo, setLauncherInfo] = useState(null);
  const [launcherUpdate, setLauncherUpdate] = useState(null);
  const [gameState, setGameState] = useState(null);
  const [downloadProgress, setDownloadProgress] = useState(null);
  const [installMessage, setInstallMessage] = useState("");
  const [authMessage, setAuthMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [session, setSession] = useState(null);
  const [profile, setProfile] = useState(null);
  const [entitlement, setEntitlement] = useState({ hasAccess: !requiresBetaAccess, code: null });

  useEffect(() => {
    let releaseLauncherEvents = () => {};
    let releaseInstallEvents = () => {};
    let releaseDownloadEvents = () => {};
    let authSubscription = null;

    async function bootstrap() {
      const info = await window.desktop.launcher.getInfo();
      setLauncherInfo(info);

      const state = await window.desktop.game.getState();
      setGameState(state);

      releaseLauncherEvents = window.desktop.launcher.onUpdateStatus((payload) => {
        setLauncherUpdate(payload);
      });

      releaseInstallEvents = window.desktop.game.onInstallStatus((payload) => {
        setInstallMessage(payload.message);
      });

      releaseDownloadEvents = window.desktop.game.onDownloadProgress((payload) => {
        setDownloadProgress(payload);
      });

      if (supabase) {
        const currentSession = await supabase.auth.getSession();
        setSession(currentSession.data.session);

        if (currentSession.data.session?.user) {
          await ensureProfile(currentSession.data.session.user);
          await refreshEntitlement(currentSession.data.session.user.id);
        }

        const authListener = supabase.auth.onAuthStateChange(async (_event, nextSession) => {
          setSession(nextSession);
          if (nextSession?.user) {
            await ensureProfile(nextSession.user);
            await refreshEntitlement(nextSession.user.id);
          } else {
            setProfile(null);
            setEntitlement({ hasAccess: !requiresBetaAccess, code: null });
          }
        });

        authSubscription = authListener.data.subscription;
      }
    }

    bootstrap();

    return () => {
      releaseLauncherEvents();
      releaseInstallEvents();
      releaseDownloadEvents();
      authSubscription?.unsubscribe();
    };
  }, []);

  async function ensureProfile(user) {
    if (!supabase || !user) {
      return null;
    }

    const profilePayload = {
      id: user.id,
      email: user.email,
      display_name:
        user.user_metadata?.display_name ||
        user.email?.split("@")[0] ||
        "Player",
      updated_at: new Date().toISOString(),
    };

    const { data: upserted, error: upsertError } = await supabase
      .from("profiles")
      .upsert(profilePayload, { onConflict: "id" })
      .select()
      .single();

    if (upsertError) {
      setAuthMessage(upsertError.message);
      return null;
    }

    setProfile(upserted);
    return upserted;
  }

  async function refreshEntitlement(userId) {
    if (!supabase || !requiresBetaAccess) {
      setEntitlement({ hasAccess: true, code: null });
      return { hasAccess: true, code: null };
    }

    const { data, error } = await supabase
      .from("beta_entitlements")
      .select("invite_code, granted_at")
      .eq("user_id", userId)
      .maybeSingle();

    if (error) {
      setAuthMessage(error.message);
      return { hasAccess: false, code: null };
    }

    const nextEntitlement = {
      hasAccess: !!data,
      code: data?.invite_code || null,
      grantedAt: data?.granted_at || null,
    };

    setEntitlement(nextEntitlement);
    return nextEntitlement;
  }

  async function refreshGameState() {
    const state = await window.desktop.game.checkForUpdates();
    setGameState(state);
    return state;
  }

  async function handleInstall() {
    if (!session) {
      setInstallMessage("Sign in before installing the game.");
      return;
    }

    if (requiresBetaAccess && !entitlement.hasAccess) {
      setInstallMessage("Redeem a valid beta invite code before installing the game.");
      return;
    }

    setBusy(true);
    setDownloadProgress(null);

    try {
      const result = await window.desktop.game.installOrUpdate();
      if (!result.ok) {
        setInstallMessage(result.message || "Install failed.");
      } else {
        setGameState(result.state);
      }
    } finally {
      setBusy(false);
    }
  }

  async function handleLaunch() {
    if (!session) {
      setInstallMessage("Sign in before launching the game.");
      return;
    }

    if (requiresBetaAccess && !entitlement.hasAccess) {
      setInstallMessage("Redeem a valid beta invite code before launching the game.");
      return;
    }

    const result = await window.desktop.game.launch();
    if (!result.ok) {
      setInstallMessage(result.message || "Launch failed.");
    } else {
      setInstallMessage("Launching game...");
    }
  }

  async function handleSignIn(email, password) {
    setAuthMessage("");
    const { error } = await supabase.auth.signInWithPassword({ email, password });
    if (error) {
      setAuthMessage(error.message);
      return;
    }

    setAuthMessage("Signed in.");
  }

  async function handleSignUp(email, password) {
    setAuthMessage("");
    const { error } = await supabase.auth.signUp({ email, password });
    if (error) {
      setAuthMessage(error.message);
      return;
    }

    setAuthMessage("Account created. Check your email if confirmation is enabled.");
  }

  async function handleSignOut() {
    await supabase.auth.signOut();
    setAuthMessage("Signed out.");
  }

  async function handleSaveProfile(displayName) {
    if (!session?.user) {
      return;
    }

    setAuthMessage("");

    const { data, error } = await supabase
      .from("profiles")
      .upsert(
        {
          id: session.user.id,
          email: session.user.email,
          display_name: displayName || "Player",
          updated_at: new Date().toISOString(),
        },
        { onConflict: "id" }
      )
      .select()
      .single();

    if (error) {
      setAuthMessage(error.message);
      return;
    }

    setProfile(data);
    setAuthMessage("Profile updated.");
  }

  async function handleRedeemCode(code) {
    if (!code) {
      setAuthMessage("Enter an invite code first.");
      return;
    }

    setAuthMessage("");

    const { data, error } = await supabase.rpc("redeem_invite_code", {
      input_code: code,
    });

    if (error) {
      setAuthMessage(error.message);
      return;
    }

    const result = Array.isArray(data) ? data[0] : data;
    if (!result?.success) {
      setAuthMessage(result?.message || "That invite code could not be redeemed.");
      return;
    }

    await refreshEntitlement(session.user.id);
    setAuthMessage(result.message || "Invite code redeemed.");
  }

  const canInstallOrPlay =
    !!session &&
    (!!entitlement.hasAccess || !requiresBetaAccess) &&
    !busy;
  const canPlay = canInstallOrPlay && !!gameState?.installed?.canLaunch;

  return (
    <main className="app-shell">
      <div className="background-glow background-glow-left" />
      <div className="background-glow background-glow-right" />

      <GameCard
        state={gameState}
        installMessage={installMessage}
        downloadProgress={downloadProgress}
        launcherInfo={launcherInfo}
        launcherUpdate={launcherUpdate}
        busy={busy}
        canInstall={canInstallOrPlay}
        canPlay={canPlay}
        onRefresh={refreshGameState}
        onInstall={handleInstall}
        onLaunch={handleLaunch}
        onOpenInstallDirectory={() => window.desktop.game.openInstallDirectory()}
        onCheckLauncherUpdate={() => window.desktop.launcher.checkForUpdates()}
        onDownloadLauncherUpdate={() => window.desktop.launcher.downloadUpdate()}
        onQuitAndInstall={() => window.desktop.launcher.quitAndInstall()}
        onOpenWebsite={() => window.desktop.launcher.openWebsite()}
      />

      <AuthCard
        session={session}
        profile={profile}
        entitlement={entitlement}
        authMessage={authMessage}
        onSignIn={handleSignIn}
        onSignUp={handleSignUp}
        onSignOut={handleSignOut}
        onSaveProfile={handleSaveProfile}
        onRedeemCode={handleRedeemCode}
      />
    </main>
  );
}
