# M2-FE-009 Files

## Scope

Implement the M2-FE file management frontend page against the accepted M1-BE file API.

## Contract

- `GET /api/v1/system/files`
- `GET /api/v1/system/files/{id:long}`
- `POST /api/v1/system/files`
- `GET /api/v1/system/files/{id:long}/download`
- `GET /api/v1/system/files/{id:long}/preview`
- `DELETE /api/v1/system/files/{id:long}`

## Requirements

- List files with keyword, MIME type, and status filters.
- Upload through `multipart/form-data`.
- Compute SHA-256 in the browser before upload.
- Send `originalName`, `mimeType`, `sizeBytes`, and `sha256` with the file field.
- Enforce 10 MB max file size before upload.
- Allow only jpg, jpeg, png, webp, pdf, and txt files.
- Preview image and PDF/text files through authenticated blob requests.
- Download files through authenticated blob requests.
- Hide upload, preview/download, and delete controls when permissions are missing.

## Non-Goals

- No backend changes.
- No CMS file API.
- No direct unauthenticated file URLs.
