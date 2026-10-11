import { copyFile, mkdir, readFile, rm } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const webDir = path.dirname(fileURLToPath(import.meta.url));
const repoDir = path.resolve(webDir, "..");
const outDir = path.join(webDir, "dist");
const mascotSourceDir = path.join(repoDir, "Mimo", "Assets", "Mascots");
const catalog = JSON.parse(await readFile(path.join(mascotSourceDir, "catalog.json"), "utf8"));

await rm(outDir, { recursive: true, force: true });
await mkdir(path.join(outDir, "assets", "mascots"), { recursive: true });

await Promise.all([
  ...["index.html", "styles.css", "main.js", "robots.txt", "sitemap.xml"].map((name) =>
    copyFile(path.join(webDir, name), path.join(outDir, name)),
  ),
  copyFile(path.join(repoDir, "Mimo", "Assets", "mimo.ico"), path.join(outDir, "assets", "mimo.ico")),
  copyFile(path.join(webDir, "assets", "mimo-mark.svg"), path.join(outDir, "assets", "mimo-mark.svg")),
  ...catalog.mascots.map(({ id }) =>
    copyFile(path.join(mascotSourceDir, `${id}.png`), path.join(outDir, "assets", "mascots", `${id}.png`)),
  ),
]);

console.log(`Mimo landing page ready: ${outDir} (${catalog.mascots.length} original mascot sheets)`);
