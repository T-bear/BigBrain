import { execFileSync } from "node:child_process";
import { existsSync, readFileSync, statSync } from "node:fs";
import { dirname, extname, resolve } from "node:path";

const root = process.cwd();
const tracked = execFileSync("git", ["ls-files", "--cached", "--others", "--exclude-standard"], { cwd: root, encoding: "utf8" })
  .trim().split("\n").filter(Boolean)
  .filter((path) => !/^docs\/adr\/000[6-9]-/.test(path));
const markdown = tracked.filter((path) => path.endsWith(".md"));
const failures = [];

for (const path of markdown) {
  const text = readFileSync(resolve(root, path), "utf8");
  const links = text.matchAll(/\[[^\]]*\]\(([^)]+)\)/g);
  for (const match of links) {
    const target = match[1].trim().replace(/^<|>$/g, "").split("#")[0];
    if (!target || /^(https?:|mailto:)/i.test(target)) continue;
    const destination = resolve(root, dirname(path), decodeURIComponent(target));
    if (!existsSync(destination)) failures.push(`${path}: broken relative link ${match[1]}`);
  }
}

const backlog = readFileSync(resolve(root, "docs/BACKLOG.md"), "utf8");
const ids = [...backlog.matchAll(/^### (BB-\d{3})\b/gm)].map((match) => match[1]);
const duplicates = ids.filter((id, index) => ids.indexOf(id) !== index);
if (duplicates.length) failures.push(`duplicate backlog IDs: ${[...new Set(duplicates)].join(", ")}`);

const status = readFileSync(resolve(root, "docs/STATUS.md"), "utf8");
for (const field of ["Senast uppdaterad:", "Verifierad mot commit:", "Runtime senast verifierad:"]) {
  if (!status.includes(field)) failures.push(`STATUS missing metadata: ${field}`);
}

const agents = readFileSync(resolve(root, "AGENTS.md"), "utf8");
for (const marker of [
  "## Documentation and publication completion rule",
  "DOCUMENTATION STATUS",
  "Dokumenten är uppdaterade:",
  "Dokumenten är granskade och inga uppdateringar behövdes.",
]) {
  if (!agents.includes(marker)) failures.push(`AGENTS missing completion rule marker: ${marker}`);
}

const forbiddenClaim = "No BigBrain POST, PUT, PATCH or DELETE Media route exists";
for (const path of markdown) {
  if (readFileSync(resolve(root, path), "utf8").includes(forbiddenClaim)) {
    failures.push(`${path}: obsolete Media read-only claim`);
  }
}

for (const path of markdown.filter((path) =>
  path.startsWith("docs/reports/features/") && !path.endsWith("README.md"))) {
  const text = readFileSync(resolve(root, path), "utf8");
  for (const heading of ["## Metadata", "## Status", "## Evidence", "## Changes", "## Security", "## Remaining work", "## Resumption"]) {
    if (!text.includes(heading)) failures.push(`${path}: missing report section ${heading}`);
  }
  if (!text.includes("Detta är en sanerad GitHub-version")) {
    failures.push(`${path}: missing sanitization notice`);
  }
}

const prohibitedTracked = tracked.filter((path) =>
  /(^|\/)(node_modules|bin|obj)(\/|$)/.test(path) ||
  path === ".env" || /\.(db|sqlite|sqlite3|mp4|mkv|avi|mov)$/i.test(path) ||
  (/(^|\/)dist(\/|$)/.test(path) && !path.startsWith("docs/")));
if (prohibitedTracked.length) failures.push(`prohibited tracked artifacts: ${prohibitedTracked.join(", ")}`);

for (const path of tracked.filter((path) => path.startsWith("docs/reports/") && extname(path) === ".md")) {
  const text = readFileSync(resolve(root, path), "utf8");
  if (/magnet:\?|Authorization\s*:|(?:10\.|192\.168\.|172\.(?:1[6-9]|2\d|3[01])\.)\d{1,3}\.\d{1,3}/i.test(text)) {
    failures.push(`${path}: prohibited sensitive pattern`);
  }
  if (statSync(resolve(root, path)).size === 0) failures.push(`${path}: empty report file`);
}

if (failures.length) {
  console.error(failures.join("\n"));
  process.exit(1);
}
console.log(`Documentation verification passed: ${markdown.length} Markdown files, ${ids.length} unique backlog IDs.`);
