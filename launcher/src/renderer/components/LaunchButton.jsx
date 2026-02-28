import { useState } from "react";

export default function LaunchButton() {
  const [status, setStatus] = useState(null);

  const onLaunch = async () => {
    setStatus(null);

    const res = await window.game.launch();

    if (res.ok) {
      setStatus({ type: "success", text: "Launching CoOpShooter..." });
      return;
    }

    // Handle known error codes cleanly
    if (res.code === "ALREADY_RUNNING") {
      setStatus({ type: "info", text: "Game is already running." });
    } else if (res.code === "GAME_NOT_FOUND") {
      setStatus({ type: "error", text: res.message });
    } else {
      setStatus({ type: "error", text: res.message || "Launch failed." });
    }
  };

  return (
    <div style={{ display: "grid", gap: 8, maxWidth: 420 }}>
      <button onClick={onLaunch}>Play CoOpShooter</button>

      {status && (
        <div
          role="status"
          style={{
            padding: 10,
            borderRadius: 8,
            border: "1px solid rgba(255,255,255,0.15)",
          }}
        >
          <strong style={{ textTransform: "capitalize" }}>{status.type}</strong>
          <div style={{ marginTop: 4, whiteSpace: "pre-wrap" }}>{status.text}</div>
        </div>
      )}
    </div>
  );
}