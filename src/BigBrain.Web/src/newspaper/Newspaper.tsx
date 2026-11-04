import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'

const master = '/newspaper/Morgonbladet_designmaster_A4.png'
const description = 'Morgonbladets designmaster – en A4-sida'

const css = `
.newspaper-master-preview { display:block; width:100%; max-width:210mm; height:auto; background:white }
#bigbrain-newspaper-print { display:none }
@media print {
  @page { size:A4 portrait; margin:0 }
  body:has(> #bigbrain-newspaper-print) { margin:0 !important; padding:0 !important; overflow:visible !important; background:white !important }
  body:has(> #bigbrain-newspaper-print) > :not(#bigbrain-newspaper-print) { display:none !important }
  #bigbrain-newspaper-print { display:block !important; margin:0; padding:0; background:white }
  #bigbrain-newspaper-print > img {
    display:block; box-sizing:border-box; width:210mm; height:297mm;
    max-width:none; max-height:none; margin:0; padding:0; border:0;
    object-fit:contain; background:white; break-inside:avoid;
    print-color-adjust:exact; -webkit-print-color-adjust:exact;
  }
}
`

export function Newspaper() {
  const [printRoot, setPrintRoot] = useState<HTMLElement | null>(null)
  const [ready, setReady] = useState(false)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    const element = document.createElement('div')
    element.id = 'bigbrain-newspaper-print'
    document.body.appendChild(element)
    setPrintRoot(element)
    return () => element.remove()
  }, [])

  return (
    <section aria-label="Morgonbladet – designmaster">
      <style>{css}</style>
      <p>Designmaster · 1 A4-sida</p>
      <button type="button" disabled={!ready || failed} onClick={() => window.print()}>
        Skriv ut A4-sidan
      </button>
      {failed && <p role="alert">Designmastern kunde inte laddas. Ladda om sidan för att försöka igen.</p>}
      <img className="newspaper-master-preview" src={master} alt={description} width={2480} height={3508} />
      {printRoot &&
        createPortal(
          <img
            src={master}
            alt={description}
            width={2480}
            height={3508}
            onLoad={() => setReady(true)}
            onError={() => setFailed(true)}
          />,
          printRoot,
        )}
    </section>
  )
}
