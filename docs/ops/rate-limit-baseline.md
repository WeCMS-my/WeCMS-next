# Rate Limit Baseline

## Policies

| Policy | Development | Staging | Production | Security event |
| --- | --- | --- | --- | --- |
| `AuthLogin` | 5 / minute | 5 / minute | 5 / minute | yes |
| `AuthRefresh` | 20 / minute | 20 / minute | 20 / minute | yes |
| `AuthTwoFactor` | 5 / minute | 5 / minute | 5 / minute | yes |
| `AdminWrite` | 60 / minute | 60 / minute | 60 / minute | yes |
| `FileUpload` | 10 / minute | 10 / minute | 10 / minute | yes |
| `SecurityUnban` | 5 / minute | 5 / minute | 5 / minute | yes |

The values above match the current production template and may be lowered per deployment risk.

## Response Contract

Rejected requests return HTTP 429 with a generic API response. The response must not reveal internal limiter keys, partition values, ban thresholds, or user enumeration details.

## Event Contract

Rate limit rejection writes `rate_limit_hit` with:

- policy
- method
- path
- user id when authenticated
- username when available
- remote IP
- user agent
- trace id

## Operations

- Alert on repeated `AuthLogin`, `AuthTwoFactor`, and `SecurityUnban` spikes.
- Review `FileUpload` spikes with file rejection events.
- Do not raise limits to hide abuse; adjust policy only after triage.
