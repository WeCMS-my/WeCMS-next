#!/usr/bin/env bash
set -euo pipefail

PACKAGE_NAME="${PACKAGE_NAME:-SqlSugarCoreNoDrive.Aot}"
PACKAGE_VERSION="${PACKAGE_VERSION:-5.1.4.186}"
RID="${RID:-}"
PUBLISH_TIMEOUT_SECONDS="${PUBLISH_TIMEOUT_SECONDS:-600}"
ROOT="${SQLSUGAR_PROBE_ROOT:-/private/tmp}"
if [[ ! -d "$ROOT" ]]; then
  ROOT="${TMPDIR:-/tmp}"
fi
WORK_DIR="$(mktemp -d "${ROOT%/}/wecms-sqlsugar-aot-probe.XXXXXX")"
LOG_FILE="$WORK_DIR/publish.log"

if [[ -z "$RID" ]]; then
  os_name="$(uname -s)"
  arch_name="$(uname -m)"
  case "$os_name:$arch_name" in
    Darwin:arm64) RID="osx-arm64" ;;
    Darwin:x86_64) RID="osx-x64" ;;
    Linux:x86_64) RID="linux-x64" ;;
    Linux:aarch64|Linux:arm64) RID="linux-arm64" ;;
    *)
      echo "Unsupported host for automatic RID detection: $os_name $arch_name" >&2
      echo "Set RID explicitly, for example: RID=linux-x64 bash $0" >&2
      exit 64
      ;;
  esac
fi

echo "SqlSugar AOT probe"
echo "Package: $PACKAGE_NAME $PACKAGE_VERSION"
echo "RID: $RID"
echo "Work dir: $WORK_DIR"
echo "Publish timeout: ${PUBLISH_TIMEOUT_SECONDS}s"

cat > "$WORK_DIR/rd.xml" <<'XML'
<Directives>
  <Application>
    <Assembly Name="SqlSugar" Dynamic="Required All" />
    <Assembly Name="sqlsugar-aot-probe" Dynamic="Required All" />
  </Application>
</Directives>
XML

cat > "$WORK_DIR/sqlsugar-aot-probe.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>sqlsugar_aot_probe</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PublishAot>true</PublishAot>
    <IsAotCompatible>true</IsAotCompatible>
    <InvariantGlobalization>false</InvariantGlobalization>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <EnableAotAnalyzer>true</EnableAotAnalyzer>
    <WarningsAsErrors>\$(WarningsAsErrors);IL2026;IL3050;IL2070;IL2072</WarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="$PACKAGE_NAME" Version="$PACKAGE_VERSION" />
  </ItemGroup>

  <ItemGroup>
    <RdXmlFile Include="rd.xml" />
  </ItemGroup>
</Project>
XML

cat > "$WORK_DIR/Program.cs" <<'CS'
using SqlSugar;

StaticConfig.EnableAot = true;

using var db = new SqlSugarClient(
    new ConnectionConfig
    {
        ConnectionString = "Server=127.0.0.1;Port=3306;Database=wecms_aot_probe;Uid=wecms;Pwd=wecms;",
        DbType = DbType.MySql,
        IsAutoCloseConnection = true
    },
    config =>
    {
        config.Aop.OnLogExecuting = (sql, parameters) =>
            Console.WriteLine(UtilMethods.GetNativeSql(sql, parameters));
    });

Console.WriteLine($"{nameof(SqlSugarClient)} initialized: {db.GetType().FullName}");
CS

set +e
dotnet publish "$WORK_DIR/sqlsugar-aot-probe.csproj" -c Release -r "$RID" /p:PublishAot=true >"$LOG_FILE" 2>&1 &
publish_pid=$!
publish_timed_out=0
publish_deadline=$((SECONDS + PUBLISH_TIMEOUT_SECONDS))

while kill -0 "$publish_pid" 2>/dev/null; do
  if (( SECONDS >= publish_deadline )); then
    publish_timed_out=1
    kill "$publish_pid" 2>/dev/null || true
    sleep 2
    kill -9 "$publish_pid" 2>/dev/null || true
    break
  fi

  sleep 2
done

wait "$publish_pid"
publish_status=$?
set -e

cat "$LOG_FILE"

if [[ "$publish_timed_out" -eq 1 ]]; then
  echo "Native AOT publish exceeded ${PUBLISH_TIMEOUT_SECONDS}s and was stopped."
  echo "This is an environment/tooling failure, not a SqlSugar compatibility result."
  echo "Log: $LOG_FILE"
  exit 124
fi

if [[ "$publish_status" -ne 0 ]]; then
  echo "Native AOT publish failed with exit code $publish_status."
  echo "Log: $LOG_FILE"
  exit "$publish_status"
fi

binary="$WORK_DIR/bin/Release/net10.0/$RID/publish/sqlsugar-aot-probe"
if [[ -x "$binary" ]]; then
  "$binary"
else
  echo "Published binary not found or not executable: $binary" >&2
  exit 1
fi

warning_pattern='(^|[[:space:]:])(warning|error)[[:space:]]+(IL[0-9]+|SYSLIB[0-9]+|CS[0-9]+|NETSDK[0-9]+|MSB[0-9]+)|:[[:space:]]*(warning|error)[[:space:]]+(IL[0-9]+|SYSLIB[0-9]+|CS[0-9]+|NETSDK[0-9]+|MSB[0-9]+)|警告[[:space:]]+(IL[0-9]+|SYSLIB[0-9]+|CS[0-9]+|NETSDK[0-9]+|MSB[0-9]+)|错误[[:space:]]+(IL[0-9]+|SYSLIB[0-9]+|CS[0-9]+|NETSDK[0-9]+|MSB[0-9]+)'

if grep -Eiq "$warning_pattern" "$LOG_FILE"; then
  echo "Probe generated a native binary, but publish output contains warnings or errors."
  echo "WeCMS production admission remains BLOCKED until the warning inventory is cleared or explicitly approved in ADR-0006."
  echo "Log: $LOG_FILE"
  exit 2
fi

echo "Probe passed with 0 detected warnings and 0 detected errors."
