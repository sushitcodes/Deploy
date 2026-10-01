import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  build: {
    rollupOptions: {
      output: {
        // Treat all font files as pure binary assets — never run them
        // through the JS/text pipeline, which corrupts the binary data
        // and causes "OTS parsing error: invalid sfntVersion" in Chrome.
        assetFileNames: (assetInfo) => {
          const name = assetInfo.names?.[0] ?? "";
          if (/\.(woff2?|ttf|eot)$/.test(name)) {
            return "assets/fonts/[name][extname]";
          }
          return "assets/[name]-[hash][extname]";
        },
      },
    },
  },
});
