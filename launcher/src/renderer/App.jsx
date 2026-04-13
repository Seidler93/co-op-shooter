import { useState } from "react";
import AuthScreen from "./components/AuthScreen";
import BetaKeyModal from "./components/BetaKeyModal";
import ConfirmModal from "./components/ConfirmModal";
import Dashboard from "./components/Dashboard";
import SettingsModal from "./components/SettingsModal";
import { useAuthState } from "./hooks/useAuthState";
import { useGameRuntime } from "./hooks/useGameRuntime";
import { useLauncherRuntime } from "./hooks/useLauncherRuntime";

export default function App() {
  const launcherRuntime = useLauncherRuntime();
  const authState = useAuthState();
  const gameRuntime = useGameRuntime({
    isAuthenticated: !!authState.session,
    hasBetaAccess: authState.hasBetaAccess,
  });

  const [isBetaModalOpen, setBetaModalOpen] = useState(false);
  const [isUninstallModalOpen, setUninstallModalOpen] = useState(false);
  const [isSettingsModalOpen, setSettingsModalOpen] = useState(false);

  const isAuthenticated = !!authState.session;
  const launcherPhase = launcherRuntime.launcherUpdate?.phase || "idle";
  const launcherPlayBlockMessage = {
    checking: "Finishing launcher update check before play.",
    available: "Update the launcher before playing.",
    downloading: "Launcher update is downloading. Install it before playing.",
    downloaded: "Restart to install the launcher update before playing.",
    error: "Launcher update status unknown. Recheck in Settings before playing.",
  }[launcherPhase] || "";

  async function handlePrimaryAction() {
    if (!isAuthenticated) {
      return;
    }

    if (gameRuntime.primaryAction === "download" && !authState.hasBetaAccess) {
      setBetaModalOpen(true);
      return;
    }

    if (gameRuntime.primaryAction === "play") {
      if (launcherPlayBlockMessage) {
        return;
      }

      await gameRuntime.launchGame({
        userId: authState.session?.user?.id,
        email: authState.session?.user?.email,
        username:
          authState.profile?.display_name ||
          authState.session?.user?.email?.split("@")[0],
        displayName: authState.profile?.display_name,
        supabaseUrl: import.meta.env.VITE_SUPABASE_URL,
        supabaseAnonKey: import.meta.env.VITE_SUPABASE_ANON_KEY,
        supabaseAccessToken: authState.session?.access_token,
      });
      return;
    }

    await gameRuntime.installOrUpdateGame();
  }

  return (
    <main className="app-shell">
      <div className="background-grid" />
      <div className="background-glow background-glow-left" />
      <div className="background-glow background-glow-right" />

      {!isAuthenticated ? (
        <AuthScreen authState={authState} />
      ) : (
        <>
          <Dashboard
            authState={authState}
            launcherRuntime={launcherRuntime}
            gameRuntime={gameRuntime}
            launcherPlayBlockMessage={launcherPlayBlockMessage}
            onPrimaryAction={handlePrimaryAction}
            onOpenBetaModal={() => setBetaModalOpen(true)}
            onRequestUninstall={() => setUninstallModalOpen(true)}
            onOpenSettings={() => setSettingsModalOpen(true)}
          />

          <BetaKeyModal
            isOpen={isBetaModalOpen}
            busy={authState.busy}
            errorMessage={authState.message}
            onClose={() => setBetaModalOpen(false)}
            onRedeem={async (code) => {
              const result = await authState.redeemInviteCode(code);
              if (result?.success) {
                setBetaModalOpen(false);
              }
            }}
          />

          <ConfirmModal
            isOpen={isUninstallModalOpen}
            busy={gameRuntime.busy}
            danger
            eyebrow="Uninstall Game"
            title="Remove local game files?"
            message="This removes the installed game build from your selected install folder. Your account, beta access, and preferred install folder will stay intact."
            confirmLabel="Uninstall"
            onClose={() => setUninstallModalOpen(false)}
            onConfirm={async () => {
              const result = await gameRuntime.uninstallGame();
              if (result?.ok) {
                setUninstallModalOpen(false);
              }
            }}
          />

          <SettingsModal
            isOpen={isSettingsModalOpen}
            authState={authState}
            launcherRuntime={launcherRuntime}
            gameRuntime={gameRuntime}
            onClose={() => setSettingsModalOpen(false)}
            onRequestUninstall={() => {
              setSettingsModalOpen(false);
              setUninstallModalOpen(true);
            }}
          />
        </>
      )}
    </main>
  );
}
