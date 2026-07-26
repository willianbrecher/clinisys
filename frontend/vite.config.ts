import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const backendUrl = env.VITE_BACKEND_URL ?? "http://localhost:5110";
  return {
    plugins: [react()],
    resolve: {
      alias: { "@": path.resolve(__dirname, "./src") },
    },
    optimizeDeps: {
      include: [
        "@fullcalendar/core",
        "@fullcalendar/core/internal",
        "@fullcalendar/core/preact",
        "@fullcalendar/react",
        "@fullcalendar/daygrid",
        "@fullcalendar/timegrid",
        "@fullcalendar/interaction",
      ],
    },
    server: {
      proxy: {
        "/api": { target: backendUrl, changeOrigin: true },
        "/connect": { target: backendUrl, changeOrigin: true },
      },
    },
  };
});
