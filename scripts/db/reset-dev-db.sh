#!/usr/bin/env bash

# WeCMS M0-BE Reset Dev DB
# Drops and recreates the development database.
# Requires: docker compose, mysql client

set -euo pipefail

FORCE=false
if [ "${1:-}" = "--force" ] || [ "${1:-}" = "-f" ]; then
  FORCE=true
fi

echo "=== WeCMS Reset Dev DB ==="

if [ "$FORCE" = false ]; then
  echo "This will DROP and recreate the wecms_dev database. All data will be lost!"
  read -r confirm
  if [ "$confirm" != "yes" ]; then
    echo "Aborted."
    exit 0
  fi
fi

echo "Ensuring MySQL container is running..."
docker compose up -d mysql

echo "Waiting for MySQL to be healthy..."
max_retries=30
retry=0

while [ "$retry" -lt "$max_retries" ]; do
  healthy=$(docker inspect --format='{{.State.Health.Status}}' wecms-mysql 2>/dev/null || true)
  if [ "$healthy" = "healthy" ]; then
    echo "MySQL is healthy."
    break
  fi
  retry=$((retry + 1))
  echo "Waiting... ($retry/$max_retries)"
  sleep 2
done

if [ "$retry" -ge "$max_retries" ]; then
  echo "MySQL failed to become healthy." >&2
  exit 1
fi

echo "Dropping and recreating database..."
docker compose exec -T mysql mysql -u root -pwecms-root-123 -e "DROP DATABASE IF EXISTS wecms_dev; CREATE DATABASE wecms_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

echo "=== Reset Complete ==="

