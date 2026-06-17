# P2-CONTRACT-002 OpenAPI Bodyless Operations Checklist

- [ ] Bodyless command-style `POST` operations no longer export `requestBody`.
- [ ] DTO-backed JSON operations still export `requestBody`.
- [ ] Multipart file upload still exports `multipart/form-data` requestBody.
- [ ] Optional DTO request properties remain present and non-required in exported schemas.
- [ ] OpenAPI shell coverage checks validate request-body presence by real contract shape, not by verb alone.
