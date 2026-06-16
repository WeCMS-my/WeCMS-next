# M1-BE-013 File API Spec

## Scope

Implement backend-only file metadata APIs:

- `GET /api/v1/system/files`
- `GET /api/v1/system/files/{id}`
- `POST /api/v1/system/files`
- `DELETE /api/v1/system/files/{id}`

M1 stores metadata only. It does not write file bytes to local disk or object storage.

## Rules

- File metadata is persisted in `sys_file`.
- Delete is soft delete.
- Responses must not expose physical paths or object keys.
- Maximum file size is 10 MiB.
- Allowed mime types: `image/jpeg`, `image/png`, `image/webp`, `application/pdf`, `text/plain`.
- Allowed extensions: `.jpg`, `.jpeg`, `.png`, `.webp`, `.pdf`, `.txt`.
- Extension is derived from `originalName`; request `fileExt` is not trusted.
- Mutations write audit log rows.
- SQL and persistence mapping stay in `WeCms.Persistence`.

## Out of Scope

- Byte upload.
- Object storage integration.
- Image processing.
- Frontend implementation.
