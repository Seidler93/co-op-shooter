import { useMemo, useState } from "react";

function AuthForm({ mode, busy, onSubmit }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const title = mode === "login" ? "Welcome back" : "Create your pilot profile";
  const subtitle =
    mode === "login"
      ? "Sign in to access your beta entitlement, updates, and install state."
      : "Create an account once and keep your launcher access synced across updates.";
  const buttonLabel = mode === "login" ? "Login" : "Create Account";

  return (
    <form
      className="auth-form"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit(email, password);
      }}
    >
      <div className="auth-copy">
        <p className="eyebrow">Secure Access</p>
        <h1>{title}</h1>
        <p className="muted">{subtitle}</p>
      </div>

      <label className="field">
        <span>Email</span>
        <input
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          type="email"
          placeholder="pilot@coopshooter.dev"
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

      <button className="primary auth-submit" disabled={busy}>
        {buttonLabel}
      </button>
    </form>
  );
}

export default function AuthScreen({ authState }) {
  const [mode, setMode] = useState("login");

  const modeCopy = useMemo(
    () => ({
      login: {
        label: "Login",
        hint: "Need an account?",
        switchLabel: "Create one",
      },
      signup: {
        label: "Create Account",
        hint: "Already have an account?",
        switchLabel: "Login",
      },
    }),
    []
  );

  return (
    <section className="auth-screen">
      <div className="auth-stage">
        <aside className="auth-hero panel">
          <p className="eyebrow">Co-op Shooter</p>
          <h2>Private beta launcher</h2>
          <p className="muted">
            Authenticate first, then manage updates, redeem beta access, and launch the game from a single tactical
            dashboard.
          </p>

          <div className="auth-feature-list">
            <div className="auth-feature">
              <strong>Automatic checks</strong>
              <span>Launcher and game updates are checked on startup.</span>
            </div>
            <div className="auth-feature">
              <strong>Access aware</strong>
              <span>Beta keys unlock install rights without breaking the launcher flow.</span>
            </div>
            <div className="auth-feature">
              <strong>Ready for backend growth</strong>
              <span>Auth, profile, and beta entitlement logic stay modular for future integrations.</span>
            </div>
          </div>
        </aside>

        <section className="auth-card panel">
          <div className="auth-toggle">
            <button
              className={mode === "login" ? "toggle-chip active" : "toggle-chip"}
              onClick={() => setMode("login")}
              type="button"
            >
              Login
            </button>
            <button
              className={mode === "signup" ? "toggle-chip active" : "toggle-chip"}
              onClick={() => setMode("signup")}
              type="button"
            >
              Create Account
            </button>
          </div>

          <AuthForm
            mode={mode}
            busy={authState.busy}
            onSubmit={mode === "login" ? authState.signIn : authState.signUp}
          />

          <p className="auth-switch">
            {modeCopy[mode].hint}{" "}
            <button
              className="text-button"
              onClick={() => setMode(mode === "login" ? "signup" : "login")}
              type="button"
            >
              {modeCopy[mode].switchLabel}
            </button>
          </p>

          {authState.message ? <div className="notice">{authState.message}</div> : null}
        </section>
      </div>
    </section>
  );
}
