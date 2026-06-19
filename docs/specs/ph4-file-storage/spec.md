# PH-4 File Storage Productionization

## Scope

PH-4 hardens the existing file upload and local storage path for production deployment. It does not add CMS media features and does not integrate a real cloud storage SDK.

## Requirements

- Local file storage must be configurable through `FileStorage:Local:BasePath`.
- Development may use `storage/files`.
- Production must fail fast when local storage base path is missing, relative, nonexistent, unwritable, or under `wwwroot`.
- `IFileStorage` must retain the current local implementation and expose future object-storage-friendly metadata operations.
- Uploads must pass through an `IFileScanService` abstraction.
- Production must fail fast when `FileStorage:VirusScanEnabled=true` while only `NoopFileScanService` exists.
- File upload rejection must continue to write `file_upload_rejected` security events.

## Non-Goals

- No S3, OSS, R2, MinIO, or other cloud SDK.
- No public media library or CMS media module.
- No legacy ThinkPHP upload compatibility.
