# P3 File Upload Concurrency Spec

## Scope

Close the P3 audit recommendation for large file upload governance:

- Limit concurrent uploads that cross the configured in-memory threshold.
- Expose current large-file upload concurrency and rejection counters.
- Keep the behavior inside the FileCenter upload boundary.

## Requirements

- `FileStorage:Upload:MaxConcurrentLargeFileUploads` controls the number of simultaneous large-file uploads.
- A large upload is any declared upload size greater than `FileStorage:Upload:MemoryFallbackThresholdBytes`.
- Large upload admission must be non-blocking; when capacity is exhausted, return `ApiCodes.TooManyRequests`.
- Small uploads must not consume a large-file concurrency slot.
- Metrics must be exposed through the file management API and protected by existing file list permission.
- Production configuration must fail fast when the concurrency limit is outside the supported range.

## Out of Scope

- No frontend page changes.
- No new permission code or menu seed.
- No object-storage adapter changes.
