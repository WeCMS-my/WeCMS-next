#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
CHECK_SCRIPT="${REPO_ROOT}/scripts/checks/check-no-self-aot-suppression.sh"

TEMP_ROOT="$(mktemp -d)"
trap 'rm -rf "${TEMP_ROOT}"' EXIT

create_repo_root() {
  local repo_root="$1"
  mkdir -p "${repo_root}/backend/src/TestProject" "${repo_root}/backend/tests/TestProject.Tests"
}

write_file() {
  local path="$1"
  local content="$2"
  mkdir -p "$(dirname "${path}")"
  printf '%s\n' "${content}" > "${path}"
}

assert_check_fails() {
  local name="$1"
  local repo_root="$2"

  if bash "${CHECK_SCRIPT}" --repo-root "${repo_root}" >/tmp/"${name}".out 2>/tmp/"${name}".err; then
    echo "${name}: expected suppression check to fail" >&2
    cat /tmp/"${name}".out >&2 || true
    cat /tmp/"${name}".err >&2 || true
    exit 1
  fi
}

assert_check_passes() {
  local name="$1"
  local repo_root="$2"

  if ! bash "${CHECK_SCRIPT}" --repo-root "${repo_root}" >/tmp/"${name}".out 2>/tmp/"${name}".err; then
    echo "${name}: expected suppression check to pass" >&2
    cat /tmp/"${name}".out >&2 || true
    cat /tmp/"${name}".err >&2 || true
    exit 1
  fi
}

repo_with_unconditional="${TEMP_ROOT}/repo-unconditional"
create_repo_root "${repo_with_unconditional}"
write_file "${repo_with_unconditional}/backend/src/TestProject/Endpoints.cs" 'using System.Diagnostics.CodeAnalysis;

public static class Endpoints
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "test")]
    public static void Map() {}
}'
assert_check_fails "unconditional" "${repo_with_unconditional}"

repo_with_suppress="${TEMP_ROOT}/repo-suppress"
create_repo_root "${repo_with_suppress}"
write_file "${repo_with_suppress}/backend/src/TestProject/Endpoints.cs" 'using System.Diagnostics.CodeAnalysis;

public static class Endpoints
{
    [SuppressMessage("AOT", "IL3050", Justification = "test")]
    public static void Map() {}
}'
assert_check_fails "suppress" "${repo_with_suppress}"

repo_with_dynamic_dependency="${TEMP_ROOT}/repo-dynamic-dependency"
create_repo_root "${repo_with_dynamic_dependency}"
write_file "${repo_with_dynamic_dependency}/backend/src/TestProject/Endpoints.cs" 'using System.Diagnostics.CodeAnalysis;

public static class Endpoints
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(Endpoints))]
    public static void Map() {}
}'
assert_check_fails "dynamic-dependency" "${repo_with_dynamic_dependency}"

repo_without_suppression="${TEMP_ROOT}/repo-clean"
create_repo_root "${repo_without_suppression}"
write_file "${repo_without_suppression}/backend/src/TestProject/Endpoints.cs" 'public static class Endpoints
{
    public static void Map() {}
}'
write_file "${repo_without_suppression}/backend/src/TestProject/TestProject.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>$(NoWarn);IL2104;IL3053</NoWarn>
  </PropertyGroup>
</Project>'
assert_check_passes "clean" "${repo_without_suppression}"

echo "check-no-self-aot-suppression tests passed."
