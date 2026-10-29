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
// Structural guard plus short, section-scoped core phrases. Ordinary prose is not
// wording-locked. These checks detect omission, not arbitrary semantic contradiction;
// review of governing rules remains mandatory.
const agentSections = [
  "1. Roller och auktoritet",
  "2. KISS och uppgiftsstyrd kontext",
  "3. Riskbaserad arbetsinsats",
  "4. Scope och Definition of Done",
  "5. Säkerhet och arkitektur",
  "6. Dokumentation – minsta beständiga sanning",
  "7. Review och blockerare",
  "8. Git och Review Checkpoints",
  "9. Avbrott och blockerad implementation",
  "10. Långvariga processer",
  "11. Kort sluthandoff",
  "12. Befintliga frysningar och beslut",
];
const sections = [...agents.matchAll(/^## (.+)\r?\n([\s\S]*?)(?=^## |$(?![\s\S]))/gm)];
if (JSON.stringify(sections.map((section) => section[1].trim())) !== JSON.stringify(agentSections)) {
  failures.push("AGENTS must contain the 12 required sections in order, without duplicates or extra sections");
}
const agentLines = agents.split(/\r?\n/).length - (agents.endsWith("\n") ? 1 : 0);
if (agentLines > 160) failures.push(`AGENTS exceeds 160 lines: ${agentLines}`);
const coreRules = [
  [3, "Finance fail-closed", [/Finance/, /fail-closed-beteende/, /aldrig nödvändig verifiering/]],
  [5, "secrets", [/Lägg aldrig hemligheter/, /lösenord/, /tokens/, /API-nycklar/]],
  [5, "Docker socket", [/Montera aldrig Docker-socketen/, /API eller Web/]],
  [7, "P0–P3", [/\| P0 \| Kritisk brist/, /\| P1 \| Blockerande avvikelse/, /\| P2 \| Icke-blockerande brist/, /\| P3 \| Framtida förbättring/]],
  [7, "P1 definition", [/konkret, evidensbaserad avvikelse/, /obligatoriskt krav/, /acceptanskriterium/, /säkerhetsvillkor/, /auktoritativa källa/, /berörd komponent/, /riskscenario och konsekvens/]],
  [7, "P1 evidence and authority", [/reproduktion när den är möjlig/, /tekniskt motiverat scenario stegvis/, /utan identifierad auktoritativ kravkälla är normalt P2/, /Misstänkta kritiska säkerhetsbrister/, /Riskacceptans upphäver inte obligatoriska krav/, /uttryckligt ändringsbeslut/]],
  [7, "two review rounds", [/Högst två ordinarie reviewvarv/, /ägaren beslutar/, /Verifierade P0\/P1 får aldrig kringgås/, /tidigare blockerare och tillkommande diff/, /får inte nedklassificeras/, /Nya P2\/P3 i oförändrad kod/, /inte obligatorisk säkerhetsverifiering/]],
  [8, "no rewritten history", [/Ingen[^.!?]*force push[^.!?]*historieskrivning/, /Skriv inte över eller återställ befintliga ändringar/]],
  [8, "verified merge identity by risk", [/Ingen merge utan verifierad kandidat-SHA\/tree/, /main-baslinje/, /required checks enligt risknivå/, /nivå 3 kräver faktisk oberoende kandidatgranskning/]],
  [12, "freezes and thaw", [/Frysningsregistret/, /Upptining kräver nytt uttryckligt ägarbeslut/, /ny auktoriserad scope\/DoD/, /Befintlig drift lämnas orörd/]],
];
for (const [number, rule, patterns] of coreRules) {
  const content = sections.find((section) => section[1].trim() === agentSections[number - 1])?.[2].replace(/\s+/g, " ") ?? "";
  if (!patterns.every((pattern) => pattern.test(content))) failures.push(`AGENTS missing core rule in section ${number}: ${rule}`);
}

const policyDocuments = new Map([
  ["docs/policy/risk-policy.md", ["# Riskpolicy och arbetsinsats", "## 3. Riskbaserad arbetsinsats", "### Risknivå för uppdraget", "### Proportionell arbetsinsats och verifiering"]],
  ["docs/operations/review-guide.md", ["# Granskarbeteende", "## 7. Granskarbeteende"]],
  ["docs/operations/checkpoint-workflow.md", ["# Git, checkpoints och återhämtning", "## 8. Git och Review Checkpoints", "## 9. Avbrott och blockerad implementation", "## 10. Långvariga processer", "## Historisk väg"]],
]);
for (const [path, headings] of policyDocuments) {
  const text = existsSync(resolve(root, path)) ? readFileSync(resolve(root, path), "utf8") : "";
  const lines = text.split(/\r?\n/);
  if (!headings.every((heading) => lines.filter((line) => line === heading).length === 1)) {
    failures.push(`${path}: missing or duplicate policy structure`);
  }
  if (!agents.includes(`](${path})`)) failures.push(`AGENTS missing policy link: ${path}`);
}

// Check fragments for all new policy links and inbound links to their retained AGENTS
// anchors. Do not turn unrelated historical anchor debt into a new global requirement.
const policyPaths = new Set(["AGENTS.md", ...policyDocuments.keys()].map((path) => resolve(root, path)));
function anchors(text) {
  const result = new Set([...text.matchAll(/<a\s+id=["']([^"']+)["']/g)].map((match) => match[1]));
  const counts = new Map();
  let fenced = false;
  for (const line of text.split(/\r?\n/)) {
    if (/^\s*(`{3,}|~{3,})/.test(line)) { fenced = !fenced; continue; }
    if (fenced) continue;
    const heading = line.match(/^#{1,6}\s+(.+?)(?:\s+#+)?$/);
    if (!heading) continue;
    const slug = heading[1].toLowerCase().replace(/[^\p{L}\p{M}\p{N}_ -]/gu, "").replace(/ /g, "-");
    const count = counts.get(slug) ?? 0;
    result.add(slug + (count ? `-${count}` : "")); counts.set(slug, count + 1);
  }
  return result;
}
for (const path of markdown) {
  const source = resolve(root, path);
  for (const match of readFileSync(source, "utf8").matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
    const link = match[1].trim().replace(/^<|>$/g, "");
    if (/^(https?:|mailto:)/i.test(link)) continue;
    const [target, fragment] = link.split("#");
    const destination = target ? resolve(dirname(source), decodeURIComponent(target)) : source;
    if (!fragment || !(policyPaths.has(source) || policyPaths.has(destination)) || !existsSync(destination)) continue;
    if (!anchors(readFileSync(destination, "utf8")).has(decodeURIComponent(fragment))) {
      failures.push(`${path}: broken policy anchor ${link}`);
    }
  }
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
