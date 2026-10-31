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
  .newspaper-print-pages .newspaper-footer {
    position:absolute; bottom:18mm; left:18mm; right:18mm
  }
}
`

function Sheet({ articles, number, total }: { articles: readonly DemoArticle[]; number: number; total: number }) {
  return (
    <section
      aria-label={`Tidningssida ${number}`}
      className={`newspaper-sheet${number === 1 ? ' newspaper-sheet--front' : ''}`}
    >
      <header className="newspaper-heading">
        <p>PERSONLIG MORGONTIDNING · PROVUPPLAGA</p>
        <h2>Morgonbladet</h2>
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
            <p>{article.body}</p>
            <small>
              {article.source} · {article.date}
            </small>
          </article>
        ))}
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

  const sheets = pages.map((articles, index) => (
    <Sheet articles={articles} key={index} number={index + 1} total={pages.length} />
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
