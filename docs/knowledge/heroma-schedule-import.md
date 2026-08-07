# Heroma schedule import knowledge

## Sanitized observed format

The locally supplied sample is an OOXML `.xlsx` workbook with one month sheet named as Swedish month plus four-digit year. The used range is a compact calendar grid: weekday headings in B–H, Monday first, and week rows below. Each day cell starts with day-of-month and may contain a label, one or more `HH:mm-HH:mm` ranges, or an all-day status. One sanitized observation confirmed two intervals on the same date.

The workbook has a merged title row, but schedule cells are not merged. Styles do not carry required meaning. Macro parts and external-link parts were absent. OOXML document properties exist and are deliberately ignored because they can contain private author metadata.

## Parser mapping

- Month/year: validated Swedish sheet name.
- Date: sheet month/year plus leading day number, cross-checked against weekday column.
- Time: 24-hour ranges in the day cell.
- Education: normalized explicit education/course/study label.
- Collaboration: normalized explicit collaboration label.
- Vacation: normalized explicit vacation label and all-day semantics when no time exists.
- Work: a time range without a recognized special label.
- Unknown: non-free text without known meaning becomes `other`; it is never guessed as a special type.
- Free days: recognized free-day labels produce no event.

No explicit safe day/evening code was found. The verified fallback classifies work starting before 12:00 local time as day and work starting at or after 12:00 as evening. This boundary matches the sample's morning and afternoon clusters and is parser-versioned. Times are local Europe/Stockholm wall-clock values; timezone conversion is not applied.

## Privacy boundary

The original sample is local-only and Git-ignored. Parser fixtures are generated from synthetic names and times. Reports contain dimensions, counts and classifications only—not workbook cells, personal identifiers, internal paths or document-property values.
