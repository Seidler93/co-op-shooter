import { createRoot } from "react-dom/client";
import LaunchButton from "./components/LaunchButton";

function App() {
  return (
    <div style={{ padding: 24, fontFamily: "system-ui" }}>
      <h1>Co-op Shooter Launcher</h1>
      <button onClick={() => window.location.reload()}>Reload</button>
      <LaunchButton/>
    </div>
  );
}

createRoot(document.getElementById("root")).render(<App />);