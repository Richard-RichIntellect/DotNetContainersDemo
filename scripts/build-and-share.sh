#!/usr/bin/env bash
# Run this on YOUR machine. It builds the API image, makes sure the SQL
# Server image is pulled too, then bundles both into one .tar file you can
# hand to a colleague on a USB stick, a shared drive, Slack, however —
# no registry account needed on either end.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "--- Building the API image ---"
docker compose build taskboard.api

echo "--- Pulling the SQL Server image (so it's bundled too) ---"
docker compose pull db

echo "--- Saving both images into one file: taskboard-images.tar ---"
# docker save -o writes silently fails (exits 0, no file, no error) when the
# destination is a Windows drive mounted into WSL2 via DrvFs — e.g. a repo
# checked out under /mnt/c or /mnt/d. Saving to a native-filesystem temp file
# first and moving it into place sidesteps that regardless of where this repo
# happens to live.
TMP_TAR="$(mktemp)"
docker save -o "$TMP_TAR" taskboardapi:latest mcr.microsoft.com/mssql/server:2022-latest
# cp, not mv: mv's cross-filesystem fallback tries to preserve timestamps and
# permissions, which DrvFs rejects, producing harmless but confusing warnings.
cp "$TMP_TAR" taskboard-images.tar
rm -f "$TMP_TAR"

ls -lh taskboard-images.tar
echo
echo "Done. Send taskboard-images.tar (and this repo's docker-compose*.yml files)"
echo "to your colleague, then have them run scripts/load-and-run.sh."
