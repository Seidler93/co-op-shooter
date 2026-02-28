import React from "react";
import { createRoot } from "react-dom/client";

function App() {
  return (
    <div style={{ padding: 24, fontFamily: "system-ui" }}>
      <h1>Co-op Shooter Launcher</h1>
      <button onClick={() => window.location.reload()}>Reload</button>
      <p>Next: button to launch the Unity build.</p>
    </div>
  );
}

createRoot(document.getElementById("root")).render(<App />);