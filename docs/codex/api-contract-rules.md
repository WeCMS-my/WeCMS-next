Backend API is the single source of truth.
OpenAPI is the frontend contract.
Frontend must not add, remove, rename, or change API fields.
Frontend must not create final data structures from mock data.
Frontend must not hardcode final menus or permissions.
Frontend TypeScript types must be generated from OpenAPI or strictly match backend DTOs.
Any API contract change must start from WeCms.Contracts project.
