# Reverse Proxy Deployment

## Recommended Production Topology

```text
Client HTTPS
  -> Cloudflare / Load Balancer / Nginx TLS termination
  -> internal HTTP
  -> Kestrel / WeCMS API
```

Kestrel should not be exposed directly to the public internet.

## Application Behavior

- `UseForwardedHeaders` runs only when `Security:ForwardedHeaders:Enabled=true`.
- `UseHsts` runs outside Development.
- `UseHttpsRedirection` is not enabled by default.
- Scheme-sensitive logic must run after forwarded headers are applied.

## Required Configuration

```json
{
  "Security": {
    "ForwardedHeaders": {
      "Enabled": true,
      "KnownProxies": [
        "10.0.0.10"
      ],
      "KnownNetworks": [
        "10.0.0.0/24"
      ]
    }
  }
}
```

Production fail-fast rules:

- If forwarded headers are disabled, the app does not trust `X-Forwarded-*`.
- If forwarded headers are enabled, at least one known proxy or network must be configured.
- Proxy and network values must parse as IP addresses or CIDR networks.

## Nginx Sketch

```nginx
location / {
  proxy_pass http://wecms-api:5261;
  proxy_set_header Host $host;
  proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
  proxy_set_header X-Forwarded-Proto $scheme;
}
```

Do not copy this as a full production Nginx config. Add your platform TLS, logging, body-size, timeout, and allowlist controls.
