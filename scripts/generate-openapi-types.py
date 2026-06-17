#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from typing import Any


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: generate-openapi-types.py <openapi-json> <output-ts>", file=sys.stderr)
        return 2

    openapi_path = Path(sys.argv[1])
    output_path = Path(sys.argv[2])
    document = json.loads(openapi_path.read_text(encoding="utf-8"))

    lines: list[str] = [
        "// Generated from artifacts/openapi/wecms-api-v1.json.",
        "// Do not edit by hand. Run: python3 scripts/generate-openapi-types.py artifacts/openapi/wecms-api-v1.json frontend/soybean-admin/src/api/types/generated.ts",
        "",
        "export type JsonObject = Record<string, unknown>;",
        "",
    ]

    schemas: dict[str, Any] = document.get("components", {}).get("schemas", {})
    for name, schema in schemas.items():
        emit_schema(lines, name, schema)
        lines.append("")

    emit_operation_types(lines, document.get("paths", {}))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")
    return 0


def emit_schema(lines: list[str], name: str, schema: dict[str, Any]) -> None:
    if name == "ApiResult":
        lines.extend(
            [
                "export interface ApiResult<TData = unknown> {",
                "  code: number;",
                "  msg: string;",
                "  data: TData;",
                "  traceId?: string | null;",
                "  fieldErrors?: Record<string, string[]> | null;",
                "}",
            ]
        )
        return

    schema_type = schema.get("type")
    if schema_type == "object" or "properties" in schema:
        properties = schema.get("properties", {})
        required = set(schema.get("required", []))
        if not properties:
            additional = schema.get("additionalProperties")
            value_type = "unknown"
            if isinstance(additional, dict):
                value_type = ts_type(additional)
            lines.append(f"export type {name} = Record<string, {value_type}>;")
            return

        lines.append(f"export interface {name} {{")
        for prop_name, prop_schema in properties.items():
            optional = "?" if prop_name not in required else ""
            lines.append(f"  {quote_property(prop_name)}{optional}: {ts_type(prop_schema)};")
        lines.append("}")
        return

    lines.append(f"export type {name} = {ts_type(schema)};")


def emit_operation_types(lines: list[str], paths: dict[str, Any]) -> None:
    lines.append("export interface ApiOperations {")
    for path in sorted(paths):
        path_item = paths[path]
        lines.append(f"  {json.dumps(path)}: {{")
        for method in ["get", "post", "put", "delete", "patch"]:
            if method not in path_item:
                continue
            operation = path_item[method]
            lines.append(f"    {method}: {{")
            lines.append(f"      response: {operation_response_type(operation)};")
            request_body = operation_request_body_type(operation)
            if request_body is not None:
                lines.append(f"      requestBody: {request_body};")
            parameters = collect_parameters(operation.get("parameters", []))
            if parameters:
                lines.append("      parameters: {")
                for location in ["path", "query", "header"]:
                    if location in parameters:
                        lines.append(f"        {location}: {{")
                        for name, type_name, required in parameters[location]:
                            optional = "" if required else "?"
                            lines.append(f"          {quote_property(name)}{optional}: {type_name};")
                        lines.append("        };")
                lines.append("      };")
            lines.append("    };")
        lines.append("  };")
    lines.append("}")


def operation_response_type(operation: dict[str, Any]) -> str:
    response = operation.get("responses", {}).get("200", {})
    schema = response.get("content", {}).get("application/json", {}).get("schema")
    if not schema:
        return "unknown"

    all_of = schema.get("allOf") if isinstance(schema, dict) else None
    if isinstance(all_of, list):
        for entry in all_of:
            data_schema = entry.get("properties", {}).get("data") if isinstance(entry, dict) else None
            if data_schema:
                return f"ApiResult<{ts_type(data_schema)}>"

    return ts_type(schema)


def operation_request_body_type(operation: dict[str, Any]) -> str | None:
    request_body = operation.get("requestBody")
    if not request_body:
        return None
    schema = request_body.get("content", {}).get("application/json", {}).get("schema")
    return ts_type(schema) if schema else "unknown"


def collect_parameters(parameters: list[dict[str, Any]]) -> dict[str, list[tuple[str, str, bool]]]:
    grouped: dict[str, list[tuple[str, str, bool]]] = {}
    for parameter in parameters:
        location = parameter.get("in")
        name = parameter.get("name")
        if not location or not name:
            continue
        grouped.setdefault(location, []).append((name, ts_type(parameter.get("schema", {})), bool(parameter.get("required"))))
    return grouped


def ts_type(schema: Any) -> str:
    if not isinstance(schema, dict):
        return "unknown"

    ref = schema.get("$ref")
    if ref:
        return ref.rsplit("/", 1)[-1]

    if "oneOf" in schema:
        return " | ".join(ts_type(item) for item in schema["oneOf"])

    if "anyOf" in schema:
        return " | ".join(ts_type(item) for item in schema["anyOf"])

    if "allOf" in schema:
        return " & ".join(ts_type(item) for item in schema["allOf"])

    schema_type = schema.get("type")
    nullable = schema.get("nullable") is True
    if isinstance(schema_type, list):
        nullable = nullable or "null" in schema_type
        non_null_types = [item for item in schema_type if item != "null"]
        rendered = " | ".join(ts_type({**schema, "type": item, "nullable": False}) for item in non_null_types) or "unknown"
        return with_null(rendered, nullable)

    if schema_type == "string":
        return with_null("string", nullable)
    if schema_type in {"integer", "number"}:
        return with_null("number", nullable)
    if schema_type == "boolean":
        return with_null("boolean", nullable)
    if schema_type == "array":
        return with_null(f"{wrap_array_item(ts_type(schema.get('items', {})))}[]", nullable)
    if schema_type == "object" or "properties" in schema:
        additional = schema.get("additionalProperties")
        if isinstance(additional, dict):
            return with_null(f"Record<string, {ts_type(additional)}>", nullable)
        if additional is True and not schema.get("properties"):
            return with_null("Record<string, unknown>", nullable)
        if schema.get("properties"):
            parts = []
            required = set(schema.get("required", []))
            for name, prop_schema in schema["properties"].items():
                optional = "?" if name not in required else ""
                parts.append(f"{quote_property(name)}{optional}: {ts_type(prop_schema)}")
            return with_null("{ " + "; ".join(parts) + " }", nullable)
        return with_null("Record<string, unknown>", nullable)

    return "unknown"


def with_null(type_name: str, nullable: bool) -> str:
    return f"{type_name} | null" if nullable else type_name


def wrap_array_item(type_name: str) -> str:
    return f"({type_name})" if " | " in type_name or " & " in type_name else type_name


def quote_property(name: str) -> str:
    if re.match(r"^[A-Za-z_$][A-Za-z0-9_$]*$", name):
        return name
    return json.dumps(name)


if __name__ == "__main__":
    raise SystemExit(main())
