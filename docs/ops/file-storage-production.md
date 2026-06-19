# File Storage Production

PH-4 keeps `local` as the only implemented provider and defines the boundary for future object storage providers.

## Configuration

| Key | Production rule |
| --- | --- |
| `FileStorage:Provider` | Must be `local` until an object storage adapter is implemented. |
| `FileStorage:Local:BasePath` | Required absolute path. Directory must exist, be writable by the API process, and must not be under `wwwroot`. |
| `FileStorage:PublicBaseUrl` | Optional for local deployments; object storage adapters will own public URL rules later. |
| `FileStorage:MaxUploadBytes` | Documents the global deploy cap. Current per-policy caps remain authoritative in code. |
| `FileStorage:AllowedMimeTypes` | Documents allowed deploy MIME families. Current per-policy allowlists remain authoritative in code. |
| `FileStorage:VirusScanEnabled` | Must remain `false` while only `NoopFileScanService` is registered. `true` fails Production startup. |

Development may use `storage/files`. Production must use an operator-created absolute directory such as `/var/lib/wecms/files`.

## Local Storage Rules

- The storage directory is private application data, not web root content.
- Do not expose the storage directory directly through Nginx, Kestrel static files, or CDN origin rules.
- API download and preview endpoints enforce permissions, audit records, content disposition, and `nosniff`.
- Object keys reject path traversal before disk access.

## Upload Security

- File policy validation still checks size, extension, MIME type, and image signatures.
- Stored content SHA256, size, and detected MIME type must match declared metadata.
- Rejected uploads write `file_upload_rejected` security events.
- `IFileScanService` is called for system file uploads and account avatar uploads.
- The current scanner is `NoopFileScanService`; it exists only to keep the interface stable until a real scanner is added.

## Operations Checklist

- Create `FileStorage:Local:BasePath` before deployment.
- Ensure the API process user can write to the directory.
- Verify the path is not inside `wwwroot`.
- Back up this directory together with the database when local storage is used.
- Keep `FileStorage:VirusScanEnabled=false` until a real scanner implementation is registered and validated.
