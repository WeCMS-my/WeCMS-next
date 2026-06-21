#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

python3 - "$repo_root" <<'PY'
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

repo = Path(sys.argv[1])
src = repo / "backend" / "src"
slnx = repo / "backend" / "WeCms.slnx"
violations: list[str] = []

platform_projects = [
    "WeCms.Data.SqlSugar",
    "WeCms.Caching",
    "WeCms.EventBus",
    "WeCms.Aop",
]

business_modules = [
    "Identity",
    "AccessControl",
    "Organization",
    "Configuration",
    "Audit",
    "Security",
    "FileCenter",
    "Platform",
]

adapter_modules = [
    "Identity",
    "AccessControl",
    "Organization",
    "Configuration",
    "Audit",
    "Security",
    "FileCenter",
]

business_directories = [
    "Contracts",
    "Endpoints",
    "Services",
    "Permissions",
    "Repositories",
    "Records",
]

adapter_directories = [
    "Entities",
    "Repositories",
    "CodeFirst",
]

expected_references: dict[str, set[str]] = {
    "WeCms.Data.SqlSugar": {"WeCms.Shared"},
    "WeCms.Caching": {"WeCms.Shared"},
    "WeCms.EventBus": {"WeCms.Shared"},
    "WeCms.Aop": {"WeCms.Shared", "WeCms.Caching", "WeCms.EventBus"},
}

for module in business_modules:
    expected_references[f"WeCms.Modules.{module}"] = {"WeCms.Shared"}

expected_references["WeCms.Modules.Identity"] = {
    "WeCms.Shared",
    "WeCms.Modules.AccessControl",
    "WeCms.Modules.Organization",
}

for module in adapter_modules:
    expected_references[f"WeCms.Modules.{module}.SqlSugar"] = {
        "WeCms.Shared",
        "WeCms.Data.SqlSugar",
        f"WeCms.Modules.{module}",
    }


def read_project_references(project_path: Path) -> set[str]:
    tree = ET.parse(project_path)
    project_dir = project_path.parent
    refs: set[str] = set()
    for item in tree.findall(".//ProjectReference"):
        include = item.attrib.get("Include")
        if not include:
            continue
        refs.add((project_dir / include.replace("\\", "/")).resolve().stem)
    return refs


def assert_file(path: Path, description: str) -> None:
    if not path.is_file():
        violations.append(f"missing {description}: {path.relative_to(repo)}")


def assert_dir(path: Path, description: str) -> None:
    if not path.is_dir():
        violations.append(f"missing {description}: {path.relative_to(repo)}")


if not slnx.is_file():
    violations.append("missing backend/WeCms.slnx")
    slnx_text = ""
else:
    slnx_text = slnx.read_text(encoding="utf-8")

for project_name, allowed_references in expected_references.items():
    project_dir = src / project_name
    project_file = project_dir / f"{project_name}.csproj"
    assert_file(project_file, f"project file for {project_name}")
    assert_file(project_dir / "AssemblyMarker.cs", f"AssemblyMarker for {project_name}")

    slnx_path = f"src/{project_name}/{project_name}.csproj"
    if slnx_path not in slnx_text:
        violations.append(f"backend/WeCms.slnx missing project {slnx_path}")

    if project_file.is_file():
        actual_references = read_project_references(project_file)
        if actual_references != allowed_references:
            violations.append(
                f"{project_name} references {sorted(actual_references)}; expected {sorted(allowed_references)}"
            )

        project_text = project_file.read_text(encoding="utf-8")
        for token in [
            "<TargetFramework>net10.0</TargetFramework>",
            "<ImplicitUsings>enable</ImplicitUsings>",
            "<Nullable>enable</Nullable>",
            "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>",
        ]:
            if token not in project_text:
                violations.append(f"{project_name}.csproj missing {token}")

for module in business_modules:
    project_name = f"WeCms.Modules.{module}"
    project_dir = src / project_name
    for directory in business_directories:
        assert_dir(project_dir / directory, f"{directory} directory for {project_name}")
    assert_file(project_dir / f"{module}ServiceCollectionExtensions.cs", f"DI extension for {project_name}")
    assert_file(project_dir / f"{module}EndpointRouteBuilderExtensions.cs", f"Endpoint extension for {project_name}")

for module in adapter_modules:
    project_name = f"WeCms.Modules.{module}.SqlSugar"
    project_dir = src / project_name
    for directory in adapter_directories:
        assert_dir(project_dir / directory, f"{directory} directory for {project_name}")
    assert_file(project_dir / f"{module}SqlSugarServiceCollectionExtensions.cs", f"SqlSugar DI extension for {project_name}")

platform_adapter = src / "WeCms.Modules.Platform.SqlSugar"
if platform_adapter.exists():
    violations.append("Platform SqlSugar adapter should not exist during S1 unless a later task introduces platform repositories")

layer_tests = repo / "backend" / "tests" / "WeCms.Tests.Architecture" / "LayerDependencyTests.cs"
assert_file(layer_tests, "LayerDependencyTests")
if layer_tests.is_file():
    layer_source = layer_tests.read_text(encoding="utf-8")
    for token in [
        "ProductionProjects_UseOnlyAllowedProjectReferences",
        "SharedProject_HasNoProductionProjectReferences",
        "BusinessModules_DoNotReferenceSqlSugarAdapterProjects",
        "SqlSugarAdapterProjects_DoNotReferenceOtherSqlSugarAdapterProjects",
    ]:
        if token not in layer_source:
            violations.append(f"LayerDependencyTests missing {token}")

if violations:
    raise SystemExit("check-system-foundation-s1-skeleton: " + "; ".join(violations))

print("check-system-foundation-s1-skeleton: ok")
PY
