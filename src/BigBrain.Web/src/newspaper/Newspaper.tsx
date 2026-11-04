const master = '/newspaper/Morgonbladet_designmaster_A4.png'
const pdf = '/newspaper/Morgonbladet_designmaster_A4.pdf'
const description = 'Morgonbladets designmaster – en A4-sida'

export function Newspaper() {
  return (
    <section aria-label="Morgonbladet – designmaster">
      <p>Designmaster · 1 A4-sida</p>
      <a
        className="primary-button"
        style={{ display: 'inline-block', marginBottom: 12 }}
        href={pdf}
        target="_blank"
        rel="noopener noreferrer"
      >
        Öppna A4-PDF för utskrift
      </a>
      <img
        src={master}
        alt={description}
        width={2480}
        height={3508}
        style={{ display: 'block', width: '100%', maxWidth: '210mm', height: 'auto', background: 'white' }}
      />
    </section>
  )
}
