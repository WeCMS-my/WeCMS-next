#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
frontend_root="$repo_root/frontend"
base_ref="${WECMS_FRONTEND_BASE_REF:-${GITHUB_BASE_REF:-}}"

if [[ -d "$frontend_root" ]]; then
  tracked_changes="$(git -C "$repo_root" status --short -- frontend || true)"
  if [[ -n "$tracked_changes" ]]; then
    printf 'check-no-frontend-change: frontend changes are not allowed in M0-BE\n%s\n' "$tracked_changes" >&2
    exit 1
  fi
fi

if [[ -z "$base_ref" ]]; then
  printf 'check-no-frontend-change: ok\n'
  exit 0
fi

if git -C "$repo_root" rev-parse --verify "$base_ref" >/dev/null 2>&1; then
  diff_base="$base_ref"
elif git -C "$repo_root" rev-parse --verify "origin/$base_ref" >/dev/null 2>&1; then
  diff_base="origin/$base_ref"
else
  printf 'check-no-frontend-change: base ref %s is not available for frontend diff check\n' "$base_ref" >&2
  exit 1
fi

diff_changes="$(git -C "$repo_root" diff --name-only "$diff_base"...HEAD -- frontend)"
if [[ -n "$diff_changes" ]]; then
  printf 'check-no-frontend-change: frontend changes are not allowed in M0-BE diff\n%s\n' "$diff_changes" >&2
  exit 1
fi

printf 'check-no-frontend-change: ok\n'
