#!/usr/bin/env bash
# Exercises the API once the stack is up (docker compose up, or
# scripts/load-and-run.sh). Every call here is going through the
# containerized API to the containerized SQL Server — nothing in-memory,
# nothing mocked.
set -euo pipefail

BASE="http://localhost:8080"

echo "--- Health check (API -> SQL Server round trip) ---"
curl -sS "$BASE/health" -w "\nSTATUS:%{http_code}\n"
echo

echo "--- Create a task ---"
RESPONSE=$(curl -sS -X POST "$BASE/tasks" -H "Content-Type: application/json" -d '{"title":"Write the Docker demo README"}')
echo "$RESPONSE"
TASK_ID=$(echo "$RESPONSE" | python3 -c "import sys,json; print(json.load(sys.stdin)['id'])")
echo "Task ID: $TASK_ID"
echo

echo "--- List tasks ---"
curl -sS "$BASE/tasks" -w "\nSTATUS:%{http_code}\n"
echo

echo "--- Mark it complete ---"
curl -sS -X POST "$BASE/tasks/$TASK_ID/complete" -w "\nSTATUS:%{http_code}\n"
echo

echo "--- Fetch it back, IsComplete should now be true ---"
curl -sS "$BASE/tasks/$TASK_ID" -w "\nSTATUS:%{http_code}\n"
echo

echo "--- Delete it ---"
curl -sS -X DELETE "$BASE/tasks/$TASK_ID" -w "\nSTATUS:%{http_code}\n"
echo

echo "--- Confirm it's gone ---"
curl -sS "$BASE/tasks/$TASK_ID" -w "\nSTATUS:%{http_code}\n"
