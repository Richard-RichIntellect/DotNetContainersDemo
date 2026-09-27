#!/usr/bin/env bash
# Run this on your COLLEAGUE'S machine, once they have Docker running and
# both taskboard-images.tar and this repo (for the compose files) copied
# over. No internet access needed from this point on — both images,
# including SQL Server, already live inside the tar file.
set -euo pipefail
cd "$(dirname "$0")/.."

if [ ! -f taskboard-images.tar ]; then
  echo "taskboard-images.tar not found next to this script — copy it in first." >&2
  exit 1
fi

echo "--- Loading both images from taskboard-images.tar ---"
docker load -i taskboard-images.tar

echo "--- Starting the stack (compose sees the images already exist, so it"
echo "    won't try to build or pull anything) ---"
docker compose up -d

echo
echo "Waiting for the API to report healthy..."
for _ in $(seq 1 20); do
  if curl -sf http://localhost:8080/health > /dev/null 2>&1; then
    echo "API is up: http://localhost:8080"
    exit 0
  fi
  sleep 2
done

echo "Still starting — check 'docker compose logs -f' if this takes longer than expected." >&2
