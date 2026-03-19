import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      entry: "src/index.ts", // Bundle registers one or more manifests
      formats: ["es"],
      fileName: "ai-log-analyser",
    },
    outDir: "../wwwroot/App_Plugins/AILogAnalyser", // your web component will be saved in this location
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
});
