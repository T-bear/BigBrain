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
// Structural/presence guard, not a substitute for independent semantic rule review.
// Normalize wrapping so ordinary Markdown line reflow does not change the contract.
const agentSections = [
  ["1. Roller och auktoritet", [
    "Produktägaren (användaren) beslutar om prioritering, scope, UX och acceptans.",
    "Vid arkitekturell oklarhet eller konflikt: stoppa och fråga före implementation.",
    "Owner UX approval registreras endast efter uttryckligt ägargodkännande.",
  ]],
  ["2. KISS och uppgiftsstyrd kontext", [
    "Läs alltid AGENTS.md och verifiera Git-baslinje och working tree.",
    "Förenkling får inte försämra säkerhet, korrekthet, tillförlitlighet eller underhållbarhet.",
  ]],
  ["3. Riskbaserad arbetsinsats", [
    "| LOW |", "| MEDIUM |", "| HIGH |",
    "Återanvänd giltig evidens när relevant tree/kod, beroenden och antaganden är oförändrade.",
    "Compute Economy tar bort redundant arbete, aldrig nödvändig verifiering, säkerhetsreview, fail-closed-beteende, dokumentation, vetenskaplig integritet eller migrationssäkerhet.",
  ]],
  ["4. Scope och Definition of Done", [
    "Saknas DoD får agenten undersöka och föreslå den, men implementation väntar på ägarens eller arkitektens godkännande inom ägarens auktoriserade scope.",
    "nytt scope kräver ägarbeslut.",
    "Uppfylld DoD avslutar uppdraget: ingen automatisk nästa sprint, checkpoint eller förbättringsloop.",
  ]],
  ["5. Säkerhet och arkitektur", [
    "Lägg aldrig hemligheter, lösenord, tokens eller API-nycklar i kod, loggar eller frontend.",
    "Montera aldrig Docker-socketen i API eller Web.",
    "Använd minsta behörighet för processer, containers, API:er och användare.",
    "Destruktiva funktioner kräver tydlig auktorisering, uttrycklig bekräftelse och audit.",
    "Radera eller skriv aldrig över användardata.",
    "Ändra inte andra servercontainers, tjänster eller konfigurationer utan uttryckligt uppdrag;",
    "ändra aldrig andra containers eller deras data som bieffekt av utveckling eller test.",
    "Moduler får inte läsa eller skriva andra modulers datalager direkt.",
    "Externa tjänster kapslas bakom adapters.",
    "Alla publika API:er versionssätts; alla API-fel använder Problem Details.",
    "`BigBrain.Brain` innehåller endast AI-orkestrering enligt `ARCHITECTURE.md`, inte generell kod.",
    "AI får endast använda deklarerade capabilities och strukturerade verktygskontrakt genom det vanliga auktoriserade API:t.",
    "Godtycklig tredjepartskod får inte laddas i API, Web eller Brain.",
    "Finance-skydd, vetenskapliga krav, risk-, provider-, rättighets-, lineage- och kapitalgränser gäller oförändrat.",
    "Dokumentationspublicering och RESEARCH ger ingen tradingbehörighet.",
  ]],
  ["6. Dokumentation – minsta beständiga sanning", [
    "NO UNDOCUMENTED SIGNIFICANT WORK:",
    "Chatt och terminalhistorik är inte bestående source of truth. Dokumentation ingår i DoD.",
    "Påstå aldrig färdigstatus utan evidens.",
  ]],
  ["7. Review och blockerare", [
    "| P0 |", "| P1 |", "| P2 |", "| P3 |",
    "Verifierade P0/P1 får aldrig kringgås för att nå en rundgräns;",
    "P2/P3 öppnar inte automatiskt nya Codex-iterationer.",
    "När ägaren skriver **”Codex är klar”** ska ChatGPT själv hämta exakt checkpoint-SHA och tree från GitHub, jämföra med accepterad main-baseline",
  ]],
  ["8. Git och Review Checkpoints", [
    "Ingen reset, rebase, amend, squash, force push eller historieskrivning av reviewhistorik.",
    "Skriv inte över eller återställ befintliga ändringar.",
    "**REVIEW CHECKPOINT:**", "**MERGE CANDIDATE:**",
    "**ACCEPTED SHA:** exakt kandidat-SHA som arkitekten granskat mot rätt main-baseline, ägaren uttryckligen godkänt och som verifierats oförändrad före merge. Ingen annan SHA får mergas.",
    "Före merge: verifiera exakt godkänd SHA, oförändrad förväntad main och relevanta required checks.",
    "Avvikelse kräver STOP/re-review.",
    "Deployment kräver separat uttrycklig auktorisering.",
  ]],
  ["9. Avbrott och blockerad implementation", [
    "Vid avbrott: bevara giltigt working-tree-arbete; kasta eller gör inte om det.",
    "INTERRUPTED — SAFE TO RESUME", "INTERRUPTED — MANUAL REVIEW REQUIRED",
    "BLOCKER HANDOFF — NOT MERGEABLE", "Blocker-branchen får inte mergas;",
    "Korrigering kräver separat uttryckligen auktoriserad checkpoint från verifierad main.",
  ]],
  ["10. Långvariga processer", [
    "Verifiera livslängd, ägarskap, resursgränser och beständig resultatlagring innan processen lämnas;",
    "En kritisk process får aldrig lämnas utan säker fel-/avbrottshantering.",
  ]],
  ["11. Kort sluthandoff", [
    "exakt verifierad remote-SHA/tree", "PASS/FAIL/PENDING/SKIP",
    "Vid beslut: ange exakt godkännande som behövs; begär inte redan givet godkännande.",
    "Misslyckade/ej körda kontroller får aldrig beskrivas som gröna.",
  ]],
  ["12. Befintliga frysningar och beslut", [
    "[Frysningsregistret](docs/operations/freezes.md)",
    "Historiska fortsättningsinstruktioner upphäver inte senare ägarbeslut.",
    "Upptining kräver nytt uttryckligt ägarbeslut och ny auktoriserad scope/DoD;",
    "Befintlig drift lämnas orörd utan separat beslut.",
  ]],
];
const sections = [...agents.matchAll(/^## (.+)\r?\n([\s\S]*?)(?=^## |$(?![\s\S]))/gm)];
if (JSON.stringify(sections.map((section) => section[1].trim())) !==
    JSON.stringify(agentSections.map(([heading]) => heading))) {
  failures.push("AGENTS must contain the 12 required sections in order, without duplicates or extra sections");
}
for (const [heading, rules] of agentSections) {
  const content = sections.find((section) => section[1].trim() === heading)?.[2].replace(/\s+/g, " ") ?? "";
  for (const rule of rules) {
    if (!content.includes(rule)) failures.push(`AGENTS missing required rule in ${heading}: ${rule}`);
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
