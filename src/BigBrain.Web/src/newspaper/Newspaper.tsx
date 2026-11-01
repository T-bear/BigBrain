import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'

export type DemoArticle = {
  id: string
  title: string
  body: string
  source: string
  date: string
}

const topics = [
  [
    'pil',
    'Ett rum av levande pil',
    'Ett växande rum av pil kan vara både en plats för grönska och en skuggig sittplats. Detta är ett fiktivt exempel som demonstrerar hur ett odlingsintresse kan presenteras. Den tänkta sittplatsen får en enkel bänk, en liten gång och ett tak av böjda grenar. Text och illustration är endast ett exempel på hur ett odlingsintresse kan presenteras i ett personligt morgonblad.',
  ],
  [
    'spel',
    'En återblick på retrospel',
    'Äldre spel och spelkonsoler kan få en egen plats i en personlig tidning. Denna påhittade artikel används för att testa text, rubriker och utrymme. I detta tänkta kulturinslag samsas en handkontroll med en kort berättelse om pixlar, skärmar och spelminnen. Det handlar inte om en verklig spelnyhet eller recension, utan om hur spaltens form kan fungera.',
  ],
  [
    'rymd',
    'Intresse för astronomi',
    'En tidning kan innehålla artiklar om rymden. Här används enbart demotext för att prova layouten, utan att påstå att någon ny upptäckt har gjorts. En längre vetenskapsspalt skulle kunna bjuda in läsaren att upptäcka natthimlens detaljer och lära sig mer om observationer. Här används bara exempel för att testa en längre, balanserad text i en tidningsspalt, utan aktuella faktaanspråk.',
  ],
  [
    'teknik',
    'Teknik i vardagen',
    'Teknikprojekt kan beskrivas på ett lättläst sätt. I denna prototyp är texten endast demonstrationsmaterial och inte någon verklig nyhetsrapport. I en tänkt artikel beskrivs vägen från en enkel skiss till ett praktiskt projekt. Målet med denna text är att ge sidan mer rytm och innehåll. Inga tekniska specifikationer eller verkliga produktnyheter rapporteras.',
  ],
  [
    'konst',
    'Akvarell och formgivning',
    'Dämpade färger och lekfulla dekorationer ger ett personligt uttryck. Texten är påhittad för att testa den visuella formen. Här får färg och form en egen lugn kulturspalt. Ett personligt uttryck kan byggas med tunna linjer, små illustrationer och läsbara stycken. Allt är en tänkt redaktionell övning och inte en aktuell konstnyhet.',
  ],
  [
    'bocker',
    'Böcker och berättelser',
    'Böcker och ljudböcker kan också vara ett intresse. Denna sjätte demotext gör det möjligt att kontrollera utskrift på tre A4-sidor. I den här påhittade kulturspalten får böcker och ljudböcker skapa en känsla av nyfikenhet och läslust. Det finns inga verkliga bokrecensioner eller rekommendationer här. Texten används för att prova den tredje sidans spalter.',
  ],
] as const

export const demoArticles: DemoArticle[] = topics.map(([id, title, body]) => ({
  id,
  title,
  body,
  source: 'Demomaterial',
  date: '2026-10-30',
}))

const ARTICLES_PER_PAGE = 2
const WORD_BUDGET = 85
const TITLE_BUDGET = 72

export function budgetPages(articles: readonly DemoArticle[], requestedPages: number): DemoArticle[][] {
  const maximum = Number.isFinite(requestedPages) ? Math.max(1, Math.min(3, Math.floor(requestedPages))) : 1
  const selected = articles.slice(0, maximum * ARTICLES_PER_PAGE).map(article => {
    const words = article.body.trim().split(/\s+/).filter(Boolean)
    return {
      ...article,
      title: article.title.slice(0, TITLE_BUDGET),
      body: words.slice(0, WORD_BUDGET).join(' ') + (words.length > WORD_BUDGET ? ' …' : ''),
    }
  })
  const pages: DemoArticle[][] = []
  for (let start = 0; start < selected.length; start += ARTICLES_PER_PAGE)
    pages.push(selected.slice(start, start + ARTICLES_PER_PAGE))
  return pages
}

function WillowArtwork() {
  return (
    <figure className="newspaper-art newspaper-art--willow">
      <img src="/newspaper/willow-dome.jpg" alt="Fotografi av ett rum av levande pil" loading="eager" />
      <figcaption>Exempelfoto: Stan Wray · CC BY-SA 2.0 · Demomaterial</figcaption>
    </figure>
  )
}

function RetroArtwork() {
  return (
    <figure className="newspaper-art newspaper-art--retro">
      <img src="/newspaper/retro-games.jpg" alt="Fotografi av en klassisk spelkontroll" loading="eager" />
      <figcaption>Exempelfoto: Digital Game Museum · CC BY 2.0 · Demomaterial</figcaption>
    </figure>
  )
}

const css = `
.newspaper-prototype { display:grid; gap:16px; min-width:0 }
.newspaper-controls { display:flex; align-items:center; flex-wrap:wrap; gap:10px }
.newspaper-controls button, .newspaper-controls select { min-height:44px }
.newspaper-preview { display:grid; gap:16px; overflow-x:auto; padding-bottom:12px }
.newspaper-sheet {
  box-sizing:border-box; position:relative; display:flex; flex-direction:column;
  width:210mm; height:297mm; padding:18mm;
  overflow:hidden; color:#34342f; font-family:Georgia,serif;
  background:
    radial-gradient(ellipse at 95% 5%,#b9d2c4 0%,transparent 31%),
    radial-gradient(ellipse at 3% 94%,#e3ccd4 0%,transparent 30%),#fffdf6;
  border:1px solid #c9c8bd;
}
.newspaper-sheet::before {
  content:'❦'; position:absolute; right:16mm; top:9mm;
  font-size:24mm; color:#8ca99b; opacity:.24
}
.newspaper-heading { border-bottom:2px solid #718a7d; padding-bottom:8mm; text-align:center }
.newspaper-heading h2 { margin:2mm 0; font-size:32pt; line-height:1.1; color:#415a4c }
.newspaper-heading p { margin:0; font-size:9pt; letter-spacing:.08em }
.newspaper-sheet--front::before { display:none }
.newspaper-sheet--front .newspaper-heading {
  text-align:left; padding-bottom:3mm; border-bottom:3px double #435b4c
}
.newspaper-sheet--front .newspaper-heading h2 {
  margin:1mm 0 2mm;
  font-family:Palatino,"Palatino Linotype",Georgia,serif;
  font-size:47pt; font-weight:700;
  letter-spacing:-.055em; line-height:1.06
}
.newspaper-edition-line {
  display:flex; justify-content:space-between; gap:5mm; padding-top:2mm;
  border-top:1px solid #a8b3a6; font-size:9pt; letter-spacing:.04em
}
.newspaper-sheet--front .newspaper-stories {
  display:grid; grid-template-columns:minmax(0,1.7fr) minmax(0,1fr);
  gap:7mm; padding-top:4mm; align-content:start
}

.newspaper-front-ribbon {
  display:grid;
  grid-template-columns:repeat(3,minmax(0,1fr));
  margin-top:2mm;
  padding:2mm 0;
  border-top:1px solid #b2bdb0;
  border-bottom:2px double #7d927e;
  break-inside:avoid;
  page-break-inside:avoid
}
.newspaper-front-ribbon > div {
  min-width:0;
  padding:0 2.5mm
}
.newspaper-front-ribbon > div + div {
  border-left:1px solid #becabc
}
.newspaper-front-ribbon span {
  display:block;
  margin-bottom:1mm;
  font-size:7pt;
  color:#536d58;
  font-weight:700;
  letter-spacing:.07em
}
.newspaper-front-ribbon strong {
  display:block;
  font-family:Georgia,serif;
  font-size:11pt;
  line-height:1.15
}
.newspaper-front-ribbon p {
  margin:1mm 0 0;
  font-size:8pt;
  line-height:1.25
}
.newspaper-story-kicker {
  display:block; margin-bottom:3mm; color:#48624f;
  font-size:8pt; font-weight:700; letter-spacing:.13em
}
.newspaper-sheet--front .newspaper-story--lead h3 {
  font-size:31pt; line-height:1.07; letter-spacing:-.025em
}
.newspaper-sheet--front .newspaper-story:not(.newspaper-story--lead) {
  border-left:1px solid #a9b7aa; padding-left:5mm
}
.newspaper-sheet--front .newspaper-story:not(.newspaper-story--lead) h3 {
  font-size:19pt
}
.newspaper-art { margin:3mm 0 4mm; break-inside:avoid; page-break-inside:avoid }
.newspaper-art svg { display:block; width:100%; height:auto; max-height:62mm }
.newspaper-art figcaption {
  margin-top:1mm; color:#626b61; font-size:8pt; line-height:1.3
}
.newspaper-art--retro svg { max-height:40mm }
.newspaper-art img {
  display:block;
  width:100%;
  object-fit:cover
}
.newspaper-art--willow img {
  height:54mm;
  object-position:center 45%
}
.newspaper-art--retro img {
  height:33mm;
  object-position:center
}

.newspaper-mast-mark {
  float:right; display:block; width:24mm; height:15mm;
  margin-left:2mm; line-height:1
}
.newspaper-mast-mark svg {
  display:block; width:100%; height:100%
}
.newspaper-front-index {
  grid-column:1 / -1; border-top:1px solid #99a89a;
  border-bottom:1px solid #c5ccc1; padding:4mm 0;
  margin-top:3mm; font-size:9pt
}
.newspaper-front-index > strong {
  display:block; margin-bottom:3mm; color:#425d49;
  font-size:10pt; text-transform:uppercase; letter-spacing:.11em
}
.newspaper-front-index ul {
  display:grid; grid-template-columns:1fr 1fr; column-gap:10mm;
  row-gap:2mm; margin:0; padding:0; list-style:none
}
.newspaper-front-index li {
  display:flex; justify-content:space-between; gap:3mm;
  border-bottom:1px dotted #c5cec5; padding-bottom:1mm
}
.newspaper-front-index li span:last-child {
  flex-shrink:0; font-variant-numeric:tabular-nums
}
.newspaper-stories { flex:1; display:grid; align-content:start; gap:12mm; padding-top:12mm }
.newspaper-sheet:not(.newspaper-sheet--front) .newspaper-heading {
  text-align:left; padding-bottom:4mm
}
.newspaper-sheet:not(.newspaper-sheet--front) .newspaper-heading h2 {
  margin:0 0 2mm;
  font-family:Palatino,"Palatino Linotype",Georgia,serif;
  font-size:27pt; font-weight:700
}
.newspaper-sheet:not(.newspaper-sheet--front) .newspaper-stories {
  grid-template-columns:repeat(2,minmax(0,1fr));
  align-content:start; gap:9mm; padding-top:9mm
}
.newspaper-sheet:not(.newspaper-sheet--front) .newspaper-story + .newspaper-story {
  border-left:1px solid #b9c4b9; padding-left:5mm
}
.newspaper-story h3 { margin:0 0 4mm; font-size:20pt; line-height:1.2; color:#405a4b }
.newspaper-story p { margin:0 0 4mm; font-size:11pt; line-height:1.5 }
.newspaper-sheet--front .newspaper-story--lead p {
  font-size:11.5pt; line-height:1.52
}
.newspaper-story small { font-size:9pt }
.newspaper-footer {
  display:flex; justify-content:space-between; border-top:1px solid #a9b7aa;
  padding-top:4mm; font-size:9pt
}

/* N1: redaktionell förstasida. Efterföljande sidor är orörda. */
.newspaper-sheet--front .newspaper-stories {
  display:grid;
  grid-template-columns:minmax(0,1.08fr) minmax(0,1.08fr) minmax(0,.82fr);
  grid-template-areas:
    "lead lead rail"
    "retro feature rail"
    "index index index";
  gap:3mm 3.5mm;
  padding-top:3mm;
  align-content:start
}
.newspaper-sheet--front .newspaper-story--lead {
  grid-area:lead;
  display:grid;
  grid-template-columns:minmax(0,.86fr) minmax(0,1.14fr);
  gap:1.5mm 3mm;
  align-content:start
}
.newspaper-sheet--front .newspaper-story--lead .newspaper-story-kicker,
.newspaper-sheet--front .newspaper-story--lead h3 {
  grid-column:1 / -1
}
.newspaper-sheet--front .newspaper-story--lead h3 {
  margin:0 0 1mm;
  font-size:27pt;
  line-height:1.06;
  letter-spacing:-.035em;
  color:#222820
}
.newspaper-sheet--front .newspaper-story--lead .newspaper-art {
  grid-column:2;
  grid-row:3 / span 2;
  margin:0;
  align-self:start
}
.newspaper-sheet--front .newspaper-story--lead p {
  grid-column:1;
  grid-row:3;
  margin:0;
  font-size:9.2pt;
  line-height:1.33
}
.newspaper-sheet--front .newspaper-story--lead small {
  grid-column:1;
  grid-row:4
}
.newspaper-sheet--front .newspaper-story--lead .newspaper-art img {
  height:53mm
}
.newspaper-sheet--front .newspaper-story:not(.newspaper-story--lead) {
  grid-area:retro;
  padding-left:0;
  border-left:0;
  min-width:0
}
.newspaper-sheet--front .newspaper-story:not(.newspaper-story--lead) h3 {
  margin-bottom:1.5mm;
  font-size:18pt;
  color:#222820
}
.newspaper-sheet--front .newspaper-story:not(.newspaper-story--lead) p {
  font-size:8.5pt;
  line-height:1.32
}
.newspaper-sheet--front .newspaper-art--retro img {
  height:25mm
}
.newspaper-sheet--front .newspaper-front-rail {
  grid-area:rail;
  border-left:1px solid #aab5a7;
  padding-left:3mm;
  min-width:0
}
.newspaper-front-rail span,
.newspaper-front-feature span {
  display:block;
  margin-bottom:1.5mm;
  font-size:7pt;
  font-weight:700;
  letter-spacing:.07em;
  color:#55725b
}
.newspaper-front-rail h3 {
  margin:0 0 2mm;
  font-size:18pt;
  line-height:1.14;
  color:#252b23
}
.newspaper-front-rail h4 {
  margin:0 0 1.5mm;
  font-size:12pt;
  line-height:1.15
}
.newspaper-front-rail p,
.newspaper-front-feature p {
  margin:0 0 2mm;
  font-size:8.8pt;
  line-height:1.34
}
.newspaper-front-rail small,
.newspaper-front-feature small {
  font-size:8pt
}
.newspaper-front-rail-note {
  margin-top:6mm;
  padding-top:3mm;
  border-top:1px solid #b4bfb0
}
.newspaper-sheet--front .newspaper-front-feature {
  grid-area:feature;
  border-top:1px solid #a2aea0;
  padding-top:3mm;
  min-width:0
}
.newspaper-front-feature h3 {
  margin:0 0 2mm;
  font-size:18pt;
  line-height:1.16;
  color:#252b23
}
.newspaper-sheet--front .newspaper-front-index {
  grid-area:index;
  grid-column:1 / -1
}
#bigbrain-newspaper-print { display:none }
@media print {
  @page { size:A4; margin:10mm 0 }
  body:has(> #bigbrain-newspaper-print) { margin:0!important; overflow:visible!important }
  body:has(> #bigbrain-newspaper-print) > #root { display:none!important }
  body > #bigbrain-newspaper-print { display:block!important }
  .newspaper-print-pages { margin:0; padding:0 }
  .newspaper-print-pages > .newspaper-sheet {
    display:block; width:210mm; height:260mm; margin:0; padding:18mm;
    overflow:visible; background-color:#fff!important; background-image:none!important;
    color:#34342f!important; border:0; box-shadow:none;
    break-inside:auto; page-break-inside:auto;
    break-after:auto; page-break-after:auto;
    print-color-adjust:economy; -webkit-print-color-adjust:economy
  }
  .newspaper-print-pages > .newspaper-sheet + .newspaper-sheet {
    break-before:page; page-break-before:always
  }
  .newspaper-print-pages .newspaper-stories { display:block }
  .newspaper-print-pages .newspaper-story + .newspaper-story { margin-top:12mm }
  .newspaper-print-pages .newspaper-sheet:not(.newspaper-sheet--front) .newspaper-stories {
    display:grid; grid-template-columns:repeat(2,minmax(0,1fr));
    gap:9mm; align-content:start
  }
  .newspaper-print-pages .newspaper-sheet:not(.newspaper-sheet--front) .newspaper-story + .newspaper-story {
    margin-top:0
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-stories {
    display:block; padding-top:8mm
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-story--lead {
    float:left; width:59%
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-story:not(.newspaper-story--lead) {
    float:right; width:34%; margin-top:0; padding-left:4mm
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-stories::after {
    content:''; display:block; clear:both
  }
  .newspaper-print-pages .newspaper-front-index {
    clear:both; margin-top:2mm; padding:2mm 0;
    break-inside:avoid; page-break-inside:avoid
  }
  .newspaper-print-pages .newspaper-front-index ul {
    row-gap:1mm
  }
  .newspaper-print-pages .newspaper-front-index li {
    padding-bottom:.5mm
  }
  .newspaper-print-pages .newspaper-art {
    break-inside:avoid; page-break-inside:avoid
  }
  .newspaper-print-pages .newspaper-footer {
    position:absolute; bottom:18mm; left:18mm; right:18mm
  }
  /* Fast 260 mm-ram med marginal för iPhones A4-sidbrytning. */
  .newspaper-print-pages > .newspaper-sheet--front {
    height:260mm
  }
  .newspaper-print-pages .newspaper-front-ribbon {
    margin-top:1mm;
    padding:1mm 0
  }
  .newspaper-print-pages .newspaper-front-ribbon p {
    margin-top:0;
    font-size:7.5pt;
    line-height:1.15
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-stories {
    padding-top:3mm
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-story--lead p {
    font-size:11pt;
    line-height:1.4
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-front-index {
    margin-top:1mm;
    padding:.5mm 0
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-front-index > strong {
    margin-bottom:1mm
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-footer {
    position:absolute;
    bottom:2mm;
    left:18mm;
    right:18mm;
    padding-top:2mm;
    margin-top:0
  }

  /* Redaktionellt rutnät ersätter den äldre float-layouten. */
  .newspaper-print-pages .newspaper-sheet--front .newspaper-stories {
    display:grid;
    grid-template-columns:minmax(0,1.08fr) minmax(0,1.08fr) minmax(0,.82fr);
    grid-template-areas:
      "lead lead rail"
      "retro feature rail"
      "index index index";
    gap:3mm 3.5mm;
    padding-top:2mm
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-story--lead {
    float:none;
    width:auto;
    margin:0
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-story:not(.newspaper-story--lead) {
    float:none;
    width:auto;
    margin:0;
    padding-left:0
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-stories::after {
    display:none;
    content:none
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-front-index {
    clear:none;
    margin-top:0;
    padding:.5mm 0
  }
  .newspaper-print-pages .newspaper-sheet--front .newspaper-story--lead p {
    font-size:9.2pt;
    line-height:1.33
  }
  .newspaper-print-pages > .newspaper-sheet {
    print-color-adjust:exact;
    -webkit-print-color-adjust:exact
  }

}
`

type ContentsEntry = { id: string; title: string; page: number }

function Sheet({
  articles,
  number,
  total,
  contents,
}: {
  articles: readonly DemoArticle[]
  number: number
  total: number
  contents: readonly ContentsEntry[]
}) {
  const astronomy = contents.find(entry => entry.id === 'rymd')
  const technology = contents.find(entry => entry.id === 'teknik')

  return (
    <section
      aria-label={`Tidningssida ${number}`}
      className={`newspaper-sheet${number === 1 ? ' newspaper-sheet--front' : ''}`}
    >
      <header className="newspaper-heading">
        <p>PERSONLIG MORGONTIDNING · PROVUPPLAGA</p>
        <h2>
          Morgonbladet
          {number === 1 && (
            <span className="newspaper-mast-mark" aria-hidden="true">
              <svg viewBox="0 0 140 86" aria-hidden="true" focusable="false">
                <g fill="none" stroke="#897e6d" strokeWidth="2">
                  <path d="M6 76 Q70 71 136 43" />
                  <path d="M35 72 Q28 54 19 45" />
                  <path d="M117 52 Q130 32 132 19" />
                </g>
                <g fill="#8faf8b" opacity=".8">
                  <ellipse cx="23" cy="47" rx="11" ry="4" transform="rotate(48 23 47)" />
                  <ellipse cx="38" cy="61" rx="12" ry="5" transform="rotate(-38 38 61)" />
                  <ellipse cx="53" cy="70" rx="13" ry="5" transform="rotate(25 53 70)" />
                  <ellipse cx="127" cy="24" rx="10" ry="4" transform="rotate(-40 127 24)" />
                  <ellipse cx="119" cy="42" rx="11" ry="5" transform="rotate(32 119 42)" />
                </g>
                <g fill="#b58d77" opacity=".8">
                  <circle cx="16" cy="43" r="3" />
                  <circle cx="22" cy="39" r="3" />
                  <circle cx="29" cy="44" r="3" />
                  <circle cx="129" cy="19" r="3" />
                </g>
                <path d="M77 50 L53 62 L63 47 L80 40 Z" fill="#708b99" opacity=".82" />
                <ellipse cx="91" cy="43" rx="19" ry="12" transform="rotate(-16 91 43)" fill="#d8bc83" />
                <path d="M77 41 Q92 29 104 40 Q96 51 83 51 Z" fill="#688696" opacity=".93" />
                <circle cx="105" cy="31" r="9" fill="#e6e1d1" />
                <path d="M98 27 Q105 17 113 27 L111 31 L99 31 Z" fill="#526e80" />
                <path d="M112 32 L122 36 L112 38 Z" fill="#927966" />
                <circle cx="108" cy="31" r="1.7" fill="#29332f" />
                <path d="M90 54 L87 65 M98 53 L101 61" fill="none" stroke="#7a6b55" strokeWidth="1.8" />
              </svg>
            </span>
          )}
        </h2>
        {number === 1 ? (
          <div className="newspaper-edition-line">
            <span>30 oktober 2026</span>
            <span>Endast demomaterial</span>
          </div>
        ) : (
          <p>30 oktober 2026 · Endast demomaterial</p>
        )}
      </header>
      {number === 1 && (
        <section className="newspaper-front-ribbon" aria-label="Redaktionellt demoband">
          <div>
            <span>NATUR · DEMO</span>
            <strong>Gröna rum</strong>
            <p>En tänkt plats för grönska och vila.</p>
          </div>
          <div>
            <span>KULTUR · DEMO</span>
            <strong>Spelminnen</strong>
            <p>En liten återblick på äldre spel.</p>
          </div>
          <div>
            <span>PERSONLIGT · DEMO</span>
            <strong>Morgonstund</strong>
            <p>En lugn stund med en egen tidning.</p>
          </div>
        </section>
      )}
      <div className="newspaper-stories">
        {articles.map((article, index) => (
          <article
            className={`newspaper-story${number === 1 && index === 0 ? ' newspaper-story--lead' : ''}`}
            key={article.id}
          >
            {number === 1 && (
              <span className="newspaper-story-kicker">{index === 0 ? 'HUVUDARTIKEL · DEMO' : 'NOTIS · DEMO'}</span>
            )}
            <h3>{article.title}</h3>
            {number === 1 && index === 0 && <WillowArtwork />}
            {number === 1 && index === 1 && <RetroArtwork />}
            <p>{number === 1 && index === 1 ? `${article.body.split(' ').slice(0, 30).join(' ')} …` : article.body}</p>
            <small>
              {article.source} · {article.date}
            </small>
          </article>
        ))}
        {number === 1 && (
          <aside className="newspaper-front-rail" aria-label="Redaktionell högerspalt">
            {astronomy ? (
              <section>
                <span>ASTRONOMI · DEMO</span>
                <h3>{astronomy.title}</h3>
                <p>En personlig tidning kan väcka nyfikenhet inför stjärnhimlen och rymdens många frågor.</p>
                <small>Fortsätter på sida {astronomy.page} · Demo</small>
              </section>
            ) : (
              <section>
                <span>OM UTGÅVAN · DEMO</span>
                <h3>En personlig morgonstund</h3>
                <p>Ett litet layoutprov med natur, spelminnen och läsglädje.</p>
              </section>
            )}
            <section className="newspaper-front-rail-note">
              <span>SMÅ BERÄTTELSER · DEMO</span>
              <h4>En tidning att återvända till</h4>
              <p>Detta är ett redaktionellt layoutprov. Inga verkliga nyheter hämtas.</p>
            </section>
          </aside>
        )}
        {number === 1 && (
          <aside className="newspaper-front-feature" aria-label="Redaktionell teknikruta">
            <span>VARDAG & TEKNIK · DEMO</span>
            <h3>{technology ? technology.title : 'Idéer i vardagen'}</h3>
            <p>Små projekt och smarta lösningar får också en plats i en personlig tidning.</p>
            {technology && <small>Fortsätter på sida {technology.page} · Demo</small>}
          </aside>
        )}
        {number === 1 && (
          <aside className="newspaper-front-index" aria-label="Innehåll i detta nummer">
            <strong>I detta nummer</strong>
            <ul>
              {contents.map(entry => (
                <li key={entry.id}>
                  <span>{entry.title}</span>
                  <span>{entry.page}</span>
                </li>
              ))}
            </ul>
          </aside>
        )}
      </div>
      <footer className="newspaper-footer">
        <span>BigBrain · N1</span>
        <span>
          Sida {number} av {total}
        </span>
      </footer>
    </section>
  )
}

export function Newspaper() {
  const [maximum, setMaximum] = useState(2)
  const [printRoot, setPrintRoot] = useState<HTMLElement | null>(null)
  const pages = budgetPages(demoArticles, maximum)

  useEffect(() => {
    const element = document.createElement('div')
    element.id = 'bigbrain-newspaper-print'
    document.body.appendChild(element)
    setPrintRoot(element)
    return () => element.remove()
  }, [])

  const contents: ContentsEntry[] = pages.flatMap((articles, pageIndex) =>
    articles.map(article => ({ id: article.id, title: article.title, page: pageIndex + 1 })),
  )

  const sheets = pages.map((articles, index) => (
    <Sheet articles={articles} contents={contents} key={index} number={index + 1} total={pages.length} />
  ))

  return (
    <section aria-label="Morgonbladet – prototyp" className="newspaper-prototype">
      <style>{css}</style>
      <p>Endast layouttest – inga verkliga nyheter hämtas.</p>
      <div className="newspaper-controls">
        <label htmlFor="newspaper-max">Maximalt antal sidor</label>
        <select id="newspaper-max" onChange={event => setMaximum(Number(event.target.value))} value={maximum}>
          <option value={1}>1 sida</option>
          <option value={2}>2 sidor</option>
          <option value={3}>3 sidor</option>
        </select>
        <button onClick={() => window.print()} type="button">
          Skriv ut provtidning
        </button>
      </div>
      <div className="newspaper-preview">{sheets}</div>
      {printRoot && createPortal(<div className="newspaper-print-pages">{sheets}</div>, printRoot)}
    </section>
  )
}
