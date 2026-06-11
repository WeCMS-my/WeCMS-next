#!/usr/bin/env bash

# WeCMS architecture layer dependency check.
# Verifies the project-level dependency matrix across production layers.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
json_mode=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --json)
      json_mode=true
      shift
      ;;
    --repo-root)
      REPO_ROOT="${2:?missing value for --repo-root}"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      echo "Usage: $0 [--json] [--repo-root <path>]" >&2
      exit 1
      ;;
  esac
done

json_quote() {
  local value="$1"
  value="${value//\\/\\\\}"
  value="${value//\"/\\\"}"
  value="${value//$'\n'/\\n}"
  value="${value//$'\r'/\\r}"
  value="${value//$'\t'/\\t}"
  printf '"%s"' "$value"
}

to_json_array() {
  local list="$1"
  local output="["
  local first=true item

  while IFS= read -r item; do
    [[ -z "$item" ]] && continue
    if [[ "$first" == true ]]; then
      first=false
    else
      output+=","
    fi
    output+="$(json_quote "$item")"
  done <<< "$list"

  output+="]"
  printf '%s' "$output"
}

has_dep() {
  local target="$1"
  local list="$2"
  local item

  while IFS= read -r item; do
    [[ -z "$item" ]] && continue
    if [[ "$item" == "$target" ]]; then
      return 0
    fi
  done <<< "$list"

  return 1
}

collect_project_refs() {
  local csproj="$1"
  local refs=""
  local line ref

  while IFS= read -r line; do
    [[ -z "$line" ]] && continue
    [[ "$line" == *'<ProjectReference'* ]] || continue
    ref="$(echo "$line" | sed -n 's/.*<ProjectReference[^>]*Include="\([^"]*\)".*/\1/p')"
    [[ -z "$ref" ]] && continue
    # Normalize both Windows and POSIX separators before extracting project name.
    ref="${ref##*\\}"
    ref="${ref##*/}"
    ref="${ref%.csproj}"
    [[ "$ref" == WeCms.* ]] || continue
    if ! has_dep "$ref" "$refs"; then
      if [[ -z "$refs" ]]; then
        refs="$ref"
      else
        refs="$refs"$'\n'"$ref"
      fi
    fi
  done < "$csproj"

  printf '%s\n' "$refs"
}

project_refs() {
  case "$1" in
    WeCms.Api)
      echo "WeCms.Modules.System
WeCms.Modules.Cms
WeCms.Infrastructure
WeCms.Persistence
WeCms.Shared"
      ;;
    WeCms.Modules.System)
      echo "WeCms.Shared"
      ;;
    WeCms.Modules.Cms)
      echo "WeCms.Shared"
      ;;
    WeCms.Persistence)
      echo "WeCms.Shared
WeCms.Modules.System
WeCms.Modules.Cms"
      ;;
    WeCms.Infrastructure)
      echo "WeCms.Shared"
      ;;
    WeCms.Shared)
      echo ""
      ;;
  esac
}

project_path() {
  case "$1" in
    WeCms.Api)
      echo "backend/src/WeCms.Api/WeCms.Api.csproj"
      ;;
    WeCms.Modules.System)
      echo "backend/src/WeCms.Modules.System/WeCms.Modules.System.csproj"
      ;;
    WeCms.Modules.Cms)
      echo "backend/src/WeCms.Modules.Cms/WeCms.Modules.Cms.csproj"
      ;;
    WeCms.Persistence)
      echo "backend/src/WeCms.Persistence/WeCms.Persistence.csproj"
      ;;
    WeCms.Infrastructure)
      echo "backend/src/WeCms.Infrastructure/WeCms.Infrastructure.csproj"
      ;;
    WeCms.Shared)
      echo "backend/src/WeCms.Shared/WeCms.Shared.csproj"
      ;;
  esac
}

project_order="WeCms.Api
WeCms.Modules.System
WeCms.Modules.Cms
WeCms.Persistence
WeCms.Infrastructure
WeCms.Shared"

violations=0
report_status="passed"
report_lines=""

if [[ "$json_mode" != "true" ]]; then
  echo "  Running layer dependency matrix checks..."
fi

for project in $project_order; do
  csproj_path="$REPO_ROOT/$(project_path "$project")"
  if [[ ! -f "$csproj_path" ]]; then
    [[ "$json_mode" != "true" ]] && echo "    ❌ Missing project file: $csproj_path"
    violations=$((violations + 1))
    report_status="failed"
    line="{\"project\":$(json_quote "$project"),\"status\":\"missing_project\",\"path\":$(json_quote "$csproj_path"),\"expected\":[],\"actual\":[],\"missing\":[],\"unexpected\":[]}"
    if [[ -z "$report_lines" ]]; then
      report_lines="$line"
    else
      report_lines="$report_lines,$line"
    fi
    continue
  fi

  actual_refs="$(collect_project_refs "$csproj_path")"
  expected_refs="$(project_refs "$project")"
  missing_refs=""
  unexpected_refs=""

  while IFS= read -r dep; do
    [[ -z "$dep" ]] && continue
    if ! has_dep "$dep" "$actual_refs"; then
      if [[ -z "$missing_refs" ]]; then
        missing_refs="$dep"
      else
        missing_refs="$missing_refs"$'\n'"$dep"
      fi
    fi
  done <<< "$expected_refs"

  while IFS= read -r dep; do
    [[ -z "$dep" ]] && continue
    if ! has_dep "$dep" "$expected_refs"; then
      if [[ -z "$unexpected_refs" ]]; then
        unexpected_refs="$dep"
      else
        unexpected_refs="$unexpected_refs"$'\n'"$dep"
      fi
    fi
  done <<< "$actual_refs"

  if [[ -n "$missing_refs" || -n "$unexpected_refs" ]]; then
    violations=$((violations + 1))
    report_status="failed"
    [[ "$json_mode" != "true" ]] && echo "    ❌ $project"
    [[ "$json_mode" != "true" ]] && [[ -n "$missing_refs" ]] && echo "      - Missing refs: $(echo "$missing_refs" | tr '\n' ' ')"
    [[ "$json_mode" != "true" ]] && [[ -n "$unexpected_refs" ]] && echo "      - Unexpected refs: $(echo "$unexpected_refs" | tr '\n' ' ')"
    line="{\"project\":$(json_quote "$project"),\"status\":\"invalid\",\"path\":$(json_quote "$csproj_path"),\"expected\":$(to_json_array "$expected_refs"),\"actual\":$(to_json_array "$actual_refs"),\"missing\":$(to_json_array "$missing_refs"),\"unexpected\":$(to_json_array "$unexpected_refs")}"
  else
    [[ "$json_mode" != "true" ]] && echo "    ✅ $project"
    line="{\"project\":$(json_quote "$project"),\"status\":\"valid\",\"path\":$(json_quote "$csproj_path"),\"expected\":$(to_json_array "$expected_refs"),\"actual\":$(to_json_array "$actual_refs"),\"missing\":[],\"unexpected\":[]}"
  fi

  if [[ -z "$report_lines" ]]; then
    report_lines="$line"
  else
    report_lines="$report_lines,$line"
  fi
done

projects_json="[$report_lines]"

if [[ "$json_mode" == "true" ]]; then
  printf '%s\n' "{\"status\":\"$report_status\",\"violations\":$violations,\"projects\":$projects_json}"
  if [[ "$report_status" == "failed" ]]; then
    exit 1
  fi
  exit 0
fi

if (( violations > 0 )); then
  echo "  Layer dependency matrix checks failed."
  exit 1
fi

echo "  Layer dependency matrix checks passed."
