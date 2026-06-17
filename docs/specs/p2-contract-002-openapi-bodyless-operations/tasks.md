# P2-CONTRACT-002 OpenAPI Bodyless Operations Tasks

1. Add the spec trio for the OpenAPI bodyless-operation contract fix.
2. Remove synthetic request-body schema assignments from bodyless command endpoints in `RegisteredDiscoveryEndpoints`.
3. Add OpenAPI regression tests that prove bodyless command operations omit `requestBody`.
4. Update the shell coverage gate to validate request bodies route-by-route instead of by HTTP verb alone.
5. Run focused OpenAPI checks, then rerun the backend gate and final audit for this task.
