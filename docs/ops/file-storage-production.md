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
| `FileStorage:VirusScanEnabled` | `false` uses `NoopFileScanService`; `true` requires `FileStorage:VirusScan:Provider=clamav-tcp`. |
| `FileStorage:VirusScan:Provider` | `clamav-tcp` is the implemented real scanner provider. `none` is allowed only when scanning is disabled. |
| `FileStorage:VirusScan:Host` | Required scanner host when scan is enabled. Use an internal service name such as `scanner.internal`. |
| `FileStorage:VirusScan:Port` | Optional scanner port when scan is enabled. Defaults to ClamAV port `3310`; Production validation allows 1-65535. |
| `FileStorage:VirusScan:TimeoutSeconds` | Optional timeout when scan is enabled. Defaults to `10`; Production validation allows 1-300 seconds. |
| `FileStorage:VirusScan:ChunkSizeBytes` | Optional scanner streaming chunk size. Defaults to `8192`; Production validation allows 1024 bytes through 1 MiB. |

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
- `NoopFileScanService` is used only when virus scanning is disabled.
- `ClamAvFileScanService` implements the ClamAV TCP `INSTREAM` protocol for Production scanning.

## Operations Checklist

- Create `FileStorage:Local:BasePath` before deployment.
  - Example:
    ```bash
    sudo mkdir -p /var/lib/wecms/files
    sudo chown -R wecms:wecms /var/lib/wecms/files
    sudo chmod 0755 /var/lib/wecms/files
    ```
- Ensure the API process user can write to the directory.
- Verify the path is not inside `wwwroot`.
- Back up this directory together with the database when local storage is used.
- Keep `FileStorage:VirusScanEnabled=false` unless a reachable ClamAV TCP scanner is deployed and validated.
- When enabling scanning, set `FileStorage:VirusScan:Provider=clamav-tcp` and `Host`. Override `Port`, `TimeoutSeconds`, and `ChunkSizeBytes` only when the defaults do not match the deployed scanner.
