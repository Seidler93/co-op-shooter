import { useState } from "react";
import AuthScreen from "./components/AuthScreen";
import BetaKeyModal from "./components/BetaKeyModal";
import Dashboard from "./components/Dashboard";
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

  const isAuthenticated = !!authState.session;

  async function handlePrimaryAction() {
    if (!isAuthenticated) {
      return;
    }

    if (gameRuntime.primaryAction === "download" && !authState.hasBetaAccess) {
      setBetaModalOpen(true);
      return;
    }

    if (gameRuntime.primaryAction === "play") {
      await gameRuntime.launchGame();
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
            onPrimaryAction={handlePrimaryAction}
            onOpenBetaModal={() => setBetaModalOpen(true)}
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
        </>
      )}
    </main>
  );
}
