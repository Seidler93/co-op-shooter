import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  root: path.resolve(__dirname, "src/renderer"),
  envDir: __dirname,
  plugins: [react()],
  base: "./",
  build: {
    outDir: path.resolve(__dirname, "dist-renderer"),
    emptyOutDir: true
  }
});
