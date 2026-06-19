#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
python3 - "$repo_root/backend/src/WeCms.Persistence/Modules/System/Users/UserRepository.cs" "$repo_root/backend/src/WeCms.Persistence/Modules/System/Roles/RoleRepository.cs" <<'PY'
import re
import sys

files = sys.argv[1:]

method_metadata = {
    "ReplaceRolesAsync": "DELETE FROM sys_user_role",
    "ReplacePostsAsync": "DELETE FROM sys_user_post",
    "ReplacePermissionsAsync": "DELETE FROM sys_role_permission",
    "ReplaceMenusAsync": "DELETE FROM sys_role_menu",
}

all_ok = True


def extract_method_body(lines, method_name):
    decl_pattern = re.compile(rf"\b{method_name}\s*\(")
    method_pattern = re.compile(rf"public\s+(?:async\s+)?Task\s+{method_name}\b")

    for index, line in enumerate(lines):
        if not method_pattern.search(line):
            continue
        if not decl_pattern.search(line):
            continue

        body_lines = []
        start_line = index + 1
        brace_depth = 0
        started = False

        for sub_index in range(index, len(lines)):
            current = lines[sub_index]
            if sub_index == index and "{" not in current:
                # skip until opening brace in following lines
                continue

            for i, char in enumerate(current):
                if char == "{":
                    brace_depth += 1
                    started = True
                elif char == "}":
                    brace_depth -= 1

            if started:
                if sub_index >= start_line:
                    body_lines.append((sub_index + 1, current))

                if brace_depth == 0:
                    # remove method signature and braces by excluding first and last captured lines
                    return [entry for entry in body_lines[1:-1]], start_line
            
            if sub_index + 1 > len(lines):
                break

    return [], -1


for path in files:
    with open(path, "r", encoding="utf-8") as handle:
        lines = handle.read().splitlines()

    for method_name, delete_sql in method_metadata.items():
        if "Users/UserRepository.cs" in path and method_name not in {"ReplaceRolesAsync", "ReplacePostsAsync"}:
            continue
        if "Roles/RoleRepository.cs" in path and method_name not in {"ReplacePermissionsAsync", "ReplaceMenusAsync"}:
            continue

        body, _ = extract_method_body(lines, method_name)
        if not body:
            print(f"check-replace-write-affected-rows: method {method_name} not found in {path}", file=sys.stderr)
            all_ok = False
            continue

        flat_body = "\n".join(line for _, line in body)

        if "_db.Ado.ExecuteCommandAsync(" in flat_body:
            first_hit = next((line_no for line_no, text in body if "_db.Ado.ExecuteCommandAsync(" in text), None)
            print(
                f"check-replace-write-affected-rows: {path}:{first_hit}: direct ExecuteCommandAsync in {method_name}; use ExpectOneAsync/ExecuteOptionalAsync instead.",
                file=sys.stderr,
            )
            all_ok = False

        if delete_sql in flat_body and "ExecuteOptionalAsync(" not in flat_body:
            print(
                f"check-replace-write-affected-rows: {path}: {method_name} should use ExecuteOptionalAsync for replace delete.",
                file=sys.stderr,
            )
            all_ok = False

        if "INSERT INTO" in flat_body and "ExpectOneAsync(" not in flat_body:
            print(
                f"check-replace-write-affected-rows: {path}: {method_name} should use ExpectOneAsync for insert rows.",
                file=sys.stderr,
            )
            all_ok = False

if not all_ok:
    sys.exit(1)

print("check-replace-write-affected-rows: ok")
PY