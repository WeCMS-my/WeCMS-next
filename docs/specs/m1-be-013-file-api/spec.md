# M1-BE-013 File API Spec

## Scope

Implement backend-only file upload APIs:

- `GET /api/v1/system/files`
- `GET /api/v1/system/files/{id}`
- `POST /api/v1/system/files` (multipart/form-data)
- `GET /api/v1/system/files/{id}/download`
- `GET /api/v1/system/files/{id}/preview`
- `DELETE /api/v1/system/files/{id}`

## Rules

- Real uploaded file bytes are persisted to local file storage, and file metadata is persisted in `sys_file`.
- Download and preview require authenticated access + permission check.
- Delete is soft delete.
- Responses must not expose physical paths or object keys.
- Maximum file size is 10 MiB.
- Allowed mime types: `image/jpeg`, `image/png`, `image/webp`, `application/pdf`, `text/plain`.
- Allowed extensions: `.jpg`, `.jpeg`, `.png`, `.webp`, `.pdf`, `.txt`.
- Extension is derived from `originalName`; request `fileExt` is not trusted.
- Server must recalculate SHA-256 and MIME type from uploaded bytes and verify against request values.
- POST request is multipart form-data and includes:
  - `file` (binary),
  - `originalName`,
  - `mimeType`,
  - `sizeBytes`,
  - `sha256`.
- Mutations write audit log rows.
- SQL and persistence mapping stay in `WeCms.Persistence`.

## Out of Scope
- Byte upload is now in scope.
- Object storage abstraction can be local-first and may evolve to cloud providers later.
- Frontend implementation.
