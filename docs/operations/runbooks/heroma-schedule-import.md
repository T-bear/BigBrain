# Heroma schedule import runbook

## Preconditions

- Use only `.xlsx` exports matching the verified Swedish Heroma month-grid format.
- Never place originals in the repository or attach them to issues.
- Confirm the API and Web health checks before import.

## User-controlled import

1. Open Kalender on Hem and choose **Öppna kalender**.
2. Select **Importera Heroma-schema**.
3. Select up to six monthly files, each no larger than 5 MiB.
4. Review month, event/category counts, warnings, duplicate status and conflicts per file.
5. For a new month choose **Importera**. For an existing month choose **Ersätt månad**, **Slå ihop** or **Avbryt**.
6. Verify dates and times in the month view and then on Hem after reload.

Replace affects only Heroma events in that month. Merge rejects different work shifts on the same date. Exact files are reported as already imported. Preview expires after ten minutes.

## Failure handling

Do not retry blindly after a persistence error. Confirm API health and free storage, then retry preview. Invalid structure, unsupported file, empty file and no-events errors make no data change. Raw files are never retained server-side.

## Rollback

Stop new imports, redeploy the prior API/Web images and preserve the `calendar-data` volume for investigation. Do not delete the database volume. The MVP intentionally exposes no DELETE import endpoint.
