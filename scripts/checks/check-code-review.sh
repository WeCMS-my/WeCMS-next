#!/usr/bin/env bash

# WeCMS M0-BE lightweight code review checklist checks.
# Rule-driven version backed by scripts/checks/code-review-rules.conf.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
RULE_FILE="$SCRIPT_DIR/code-review-rules.conf"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-root)
      REPO_ROOT="${2:?missing value for --repo-root}"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      echo "Usage: $0 [--repo-root <path>]" >&2
      exit 1
      ;;
  esac
done

if [[ ! -f "$REPO_ROOT/code_review.md" ]]; then
  echo "code_review.md missing: required baseline for review rules." >&2
  exit 1
fi

if [[ ! -f "$RULE_FILE" ]]; then
  echo "Rule file missing: $RULE_FILE" >&2
  exit 1
fi

p0_failures=0
p1_failures=0

trim() {
  local value="$1"
  value="${value#"${value%%[![:space:]]*}"}"
  value="${value%"${value##*[![:space:]]}"}"
  printf '%s' "$value"
}

is_whitelisted() {
  local file_path="$1"
  local line="$2"
  local whitelist_raw="$3"

  if [[ -z "$whitelist_raw" ]]; then
    return 1
  fi

  IFS=';' read -r -a wh <<< "$whitelist_raw"
  for raw_w in "${wh[@]}"; do
    local w
    w="$(trim "$raw_w")"
    [[ -z "$w" ]] && continue

    if [[ "$line" == *"$w"* ]]; then
      return 0
    fi

    if [[ "$file_path" == "$w" ]]; then
      return 0
    fi

    case "$file_path" in
      $w)
        return 0
        ;;
    esac
  done

  return 1
}

run_pattern_scan() {
  local rule_id="$1"
  local severity="$2"
  local desc="$3"
  local pattern="$4"
  local paths_csv="$5"
  local whitelist="$6"
  local review_ref="$7"
  local header_ref=""

  if [[ -n "$review_ref" ]]; then
    header_ref=" [ref: ${review_ref}]"
  fi

  printf "    [%s] (%s) %s%s\n" "$rule_id" "$severity" "$desc" "$header_ref"

  local found=0
  local hit_count=0
  IFS=';' read -r -a paths <<< "$paths_csv"

  for raw_path in "${paths[@]}"; do
    [[ -z "$raw_path" ]] && continue
    local path="$REPO_ROOT/$raw_path"
    while IFS= read -r match; do
      if [[ -z "$match" ]]; then
        continue
      fi
      local matched_file="${match%%:*}"
      if ! is_whitelisted "$matched_file" "$match" "$whitelist"; then
        if [[ -n "$review_ref" ]]; then
          printf "      ❌ [%s] [ref: %s] %s\n" "$rule_id" "$review_ref" "$match"
        else
          printf "      ❌ [%s] %s\n" "$rule_id" "$match"
        fi
        found=1
        hit_count=$((hit_count + 1))
      fi
    done < <(rg -n --color=never -g "*.cs" --glob '!**/backend/tests/**' "$pattern" "$path" || true)
  done

  if (( hit_count > 0 )); then
    printf "      💥 hits: %d\n" "$hit_count"
  fi

  if (( found == 1 )); then
    if [[ "$severity" == "P0" ]]; then
      ((p0_failures += 1))
    else
      ((p1_failures += 1))
    fi
  fi
}

run_file_limit_scan() {
  local rule_id="$1"
  local severity="$2"
  local desc="$3"
  local limit="$4"
  local paths_csv="$5"
  local whitelist="$6"
  local review_ref="$7"
  local header_ref=""

  if [[ -n "$review_ref" ]]; then
    header_ref=" [ref: ${review_ref}]"
  fi

  printf "    [%s] (%s) %s%s\n" "$rule_id" "$severity" "$desc" "$header_ref"

  local found=0
  local hit_count=0
  local line_limit="${limit:-600}"
  IFS=';' read -r -a paths <<< "$paths_csv"

  local path
  for raw_path in "${paths[@]}"; do
    [[ -z "$raw_path" ]] && continue
    local dir="$REPO_ROOT/$raw_path"
    while IFS= read -r -d '' file; do
      local line_count
      line_count=$(wc -l < "$file")
      if (( line_count > line_limit )); then
        local line="${file}:${line_count}: file exceeds limit ${line_limit}"
        if ! is_whitelisted "$file" "$line" "$whitelist"; then
          if [[ -n "$review_ref" ]]; then
            printf "      ❌ [%s] [ref: %s] %s has %s lines (limit: %s)\n" \
              "$rule_id" "$review_ref" "$file" "$line_count" "$line_limit"
          else
            printf "      ❌ [%s] %s has %s lines (limit: %s)\n" "$rule_id" "$file" "$line_count" "$line_limit"
          fi
          found=1
          hit_count=$((hit_count + 1))
        fi
      fi
    done < <(find "$dir" -type f -name '*.cs' -print0 ! -path '*/bin/*' ! -path '*/obj/*' ! -path '*/backend/tests/*')
  done

  if (( hit_count > 0 )); then
    printf "      💥 hits: %d\n" "$hit_count"
  fi

  if (( found == 1 )); then
    if [[ "$severity" == "P0" ]]; then
      ((p0_failures += 1))
    else
      ((p1_failures += 1))
    fi
  fi
}

printf "  Running code review checklist checks...\n\n"

# DI boundary baseline from dedicated rule set
printf "  [1] DI baseline scan (code_review: 1.5.1 / 1.5.3 / DB-BOUNDARY-004)\n"
review_di_log="$(mktemp)"
if ! bash "$SCRIPT_DIR/../review-di.sh" "$REPO_ROOT" >"$review_di_log" 2>&1; then
  p0_failures=$((p0_failures + 1))
  printf "    ❌ DI baseline scan failed\n"
fi
if [[ -s "$review_di_log" ]]; then
  while IFS= read -r line; do
    [[ -n "$line" ]] && printf "    %s\n" "$line"
  done < "$review_di_log"
fi
rm -f "$review_di_log"

printf "  [2] Rule-driven scan from code-review-rules.conf\n\n"

while IFS='|' read -r rule_id severity scope description pattern paths limit whitelist review_ref; do
  rule_id="$(trim "$rule_id")"
  severity="$(trim "$severity")"
  scope="$(trim "$scope")"
  description="$(trim "$description")"
  pattern="$(trim "$pattern")"
  paths="$(trim "$paths")"
  limit="$(trim "$limit")"
  whitelist="$(trim "$whitelist")"
  review_ref="$(trim "$review_ref")"

  [[ -z "$rule_id" || "${rule_id:0:1}" == "#" ]] && continue

  if [[ -z "$rule_id" || -z "$severity" || -z "$scope" || -z "$paths" || (-z "$pattern" && "$scope" != "file") ]]; then
    echo "    ⚠️  skip invalid rule line: $rule_id|$severity|$scope|$description"
    continue
  fi

  case "$scope" in
    module|src)
      run_pattern_scan "$rule_id" "$severity" "$description" "$pattern" "$paths" "$whitelist" "$review_ref"
      ;;
    file)
      run_file_limit_scan "$rule_id" "$severity" "$description" "$limit" "$paths" "$whitelist" "$review_ref"
      ;;
    *)
      echo "    ⚠️  unknown scope '$scope' for rule $rule_id"
      ;;
  esac
done < "$RULE_FILE"

printf "\n  Code review rule summary: P0=%s, P1=%s\n" "$p0_failures" "$p1_failures"

if (( p0_failures > 0 || p1_failures > 0 )); then
  printf "  ❌ code-review checklist failed.\n"
  exit 1
fi

echo "  ✅ code-review checklist passed."
