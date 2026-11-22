import { defineConfig } from "vite";

// Builds the backoffice extensions into wwwroot/App_Plugins/Stoolball.Umbraco.Backoffice.
// The output is not committed. Push-UmbracoCloud.ps1 builds it and copies it to the Umbraco Cloud repository.
export default defineConfig({
  build: {
    lib: {
      entry: "src/manifests.ts",
      formats: ["es"],
      fileName: "manifest",
    },
    outDir: "../wwwroot/App_Plugins/Stoolball.Umbraco.Backoffice",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
});
