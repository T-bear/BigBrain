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
    'Ett växande rum av pil kan vara både en plats för grönska och en skuggig sittplats. Detta är ett fiktivt exempel som demonstrerar hur ett odlingsintresse kan presenteras.',
  ],
  [
    'spel',
    'En återblick på retrospel',
    'Äldre spel och spelkonsoler kan få en egen plats i en personlig tidning. Denna påhittade artikel används för att testa text, rubriker och utrymme.',
  ],
  [
    'rymd',
    'Intresse för astronomi',
    'En tidning kan innehålla artiklar om rymden. Här används enbart demotext för att prova layouten, utan att påstå att någon ny upptäckt har gjorts.',
  ],
  [
    'teknik',
    'Teknik i vardagen',
    'Teknikprojekt kan beskrivas på ett lättläst sätt. I denna prototyp är texten endast demonstrationsmaterial och inte någon verklig nyhetsrapport.',
  ],
  [
    'konst',
    'Akvarell och formgivning',
    'Dämpade färger och lekfulla dekorationer ger ett personligt uttryck. Texten är påhittad för att testa den visuella formen.',
  ],
  [
    'bocker',
    'Böcker och berättelser',
    'Böcker och ljudböcker kan också vara ett intresse. Denna sjätte demotext gör det möjligt att kontrollera utskrift på tre A4-sidor.',
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
      <svg viewBox="0 0 520 290" role="img" aria-label="Tecknad sittplats under en båge av levande pil">
        <path d="M18 256 Q262 278 502 255" fill="none" stroke="#b5bcab" strokeWidth="3" />
        <path
          d="M79 252 C84 135 147 32 263 28 C374 29 447 132 452 253"
          fill="none"
          stroke="#687e6a"
          strokeWidth="9"
          strokeLinecap="round"
        />
        <path
          d="M114 248 C116 140 169 61 262 60 C352 60 410 141 417 249"
          fill="none"
          stroke="#9bab92"
          strokeWidth="5"
        />
        <path
          d="M150 246 C154 147 195 93 263 93 C329 94 369 149 378 245"
          fill="none"
          stroke="#738b78"
          strokeWidth="4"
        />
        <path d="M92 174 Q250 115 433 182 M130 103 Q258 155 396 107" fill="none" stroke="#a4b394" strokeWidth="3" />
        <g fill="#b3c7a8" stroke="#718e73" strokeWidth="1.5">
          <ellipse cx="128" cy="102" rx="19" ry="7" transform="rotate(-37 128 102)" />
          <ellipse cx="189" cy="49" rx="17" ry="7" transform="rotate(27 189 49)" />
          <ellipse cx="285" cy="32" rx="17" ry="7" transform="rotate(-24 285 32)" />
          <ellipse cx="370" cy="75" rx="18" ry="7" transform="rotate(36 370 75)" />
          <ellipse cx="419" cy="133" rx="18" ry="7" transform="rotate(-24 419 133)" />
          <ellipse cx="100" cy="185" rx="18" ry="7" transform="rotate(32 100 185)" />
          <ellipse cx="407" cy="199" rx="18" ry="7" transform="rotate(-29 407 199)" />
        </g>
        <path
          d="M184 223 H338 M197 223 L183 260 M326 223 L342 260"
          fill="none"
          stroke="#90765d"
          strokeWidth="7"
          strokeLinecap="round"
        />
        <path d="M221 208 H309 L325 219 H206 Z" fill="#d5c2a0" stroke="#90765d" strokeWidth="3" />
        <path
          d="M119 222 H171 M131 219 L122 254 M163 219 L173 254"
          stroke="#998568"
          strokeWidth="6"
          fill="none"
          strokeLinecap="round"
        />
        <path
          d="M352 222 H406 M360 219 L351 254 M397 219 L408 254"
          stroke="#998568"
          strokeWidth="6"
          fill="none"
          strokeLinecap="round"
        />
        <path d="M251 205 V196 M242 196 Q251 189 260 196" stroke="#6b826b" strokeWidth="3" fill="none" />
        <path d="M32 258 Q51 230 70 257 M447 259 Q468 223 490 258" stroke="#8eaa84" strokeWidth="4" fill="none" />
      </svg>
      <figcaption>Illustration av ett pilrum · Demomaterial</figcaption>
    </figure>
  )
}

function RetroArtwork() {
  return (
    <figure className="newspaper-art newspaper-art--retro">
      <svg viewBox="0 0 320 185" role="img" aria-label="Tecknad retrospelskärm och handkontroll">
        <rect x="64" y="10" width="204" height="138" rx="12" fill="none" stroke="#656d60" strokeWidth="7" />
        <rect x="82" y="27" width="166" height="94" rx="4" fill="#e9efeb" stroke="#9aaea2" strokeWidth="3" />
        <path d="M82 95 L119 66 L157 93 L197 51 L248 93" fill="none" stroke="#799b85" strokeWidth="5" />
        <path d="M82 110 H248" stroke="#9aab8a" strokeWidth="4" />
        <path d="M145 149 V168 M190 149 V168 M128 168 H210" fill="none" stroke="#656d60" strokeWidth="6" />
        <rect x="26" y="116" width="133" height="54" rx="25" fill="#e5e1d7" stroke="#777a6c" strokeWidth="4" />
        <path d="M53 144 H87 M70 127 V161" stroke="#646a64" strokeWidth="7" strokeLinecap="round" />
        <circle cx="115" cy="137" r="6" fill="#789784" />
        <circle cx="132" cy="151" r="6" fill="#b1a07b" />
      </svg>
      <figcaption>Retrospel · Demomaterial</figcaption>
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
  margin:1mm 0 2mm; font-size:47pt; letter-spacing:-.045em; line-height:1.06
}
.newspaper-edition-line {
  display:flex; justify-content:space-between; gap:5mm; padding-top:2mm;
  border-top:1px solid #a8b3a6; font-size:9pt; letter-spacing:.04em
}
.newspaper-sheet--front .newspaper-stories {
  display:grid; grid-template-columns:minmax(0,1.7fr) minmax(0,1fr);
  gap:7mm; padding-top:8mm; align-content:start
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
.newspaper-mast-mark {
  float:right; color:#78917c; font-size:21pt; line-height:1.6;
  font-weight:normal; letter-spacing:0
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
.newspaper-story h3 { margin:0 0 4mm; font-size:20pt; line-height:1.2; color:#405a4b }
.newspaper-story p { margin:0 0 4mm; font-size:12pt; line-height:1.55 }
.newspaper-story small { font-size:9pt }
.newspaper-footer {
  display:flex; justify-content:space-between; border-top:1px solid #a9b7aa;
  padding-top:4mm; font-size:9pt
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
    clear:both; margin-top:5mm; break-inside:avoid; page-break-inside:avoid
  }
  .newspaper-print-pages .newspaper-art {
    break-inside:avoid; page-break-inside:avoid
  }
  .newspaper-print-pages .newspaper-footer {
    position:absolute; bottom:18mm; left:18mm; right:18mm
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
              ❦
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
            <p>{article.body}</p>
            <small>
              {article.source} · {article.date}
            </small>
          </article>
        ))}
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
