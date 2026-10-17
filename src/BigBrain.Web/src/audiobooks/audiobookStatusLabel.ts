export function audiobookStatusLabel(status: string) {
  return (
    (
      {
        requested: 'Begärd',
        searching: 'Söker',
        candidateFound: 'Träff hittad',
        awaitingSelection: 'Väntar på val',
        queued: 'Köad',
        downloading: 'Hämtas',
        importing: 'Importeras',
        indexing: 'Indexeras',
        completed: 'Klar',
        failed: 'Misslyckades',
        cancelled: 'Avbruten',
      } as Record<string, string>
    )[status] ?? 'Okänt tillstånd'
  )
}
