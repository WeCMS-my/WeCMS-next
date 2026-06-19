# Security Alerting Baseline

## Severity Levels

| Severity | Meaning | Alert |
|---|---|---|
| info | routine security event | no |
| warning | suspicious or policy-relevant event | no |
| high | likely active abuse or operator action required | yes |
| critical | confirmed high-risk security condition | yes |

## Critical Event Classes

The production baseline treats these as critical or alert-worthy:

- refresh token reuse;
- brute force ban threshold reached;
- repeated two-factor failure or replay;
- permission denied spike;
- file upload rejection spike;
- IP or security ban hit.

PH-3 provides `ISecurityAlertSink` and `LoggingSecurityAlertSink`. External sinks such as webhook, email, SIEM, or incident tooling require a separate production decision and must not introduce secrets into repository configuration.

## Current Alert Flow

1. Security event is classified.
2. Application writes the security event to `sys_security_event`.
3. Alert service receives high or critical events.
4. `LoggingSecurityAlertSink` emits a critical log record with event type, severity, source, trace id, timestamp, and message.

The PH-3 code path routes alert decisions from auth security events, login failure bans, two-factor challenge failures/replays, security ban hits/unbans, rate-limit hits, and IP access denials. Warning and info events are evaluated but not emitted to the sink.

## Operator Checklist

- Monitor `sys_security_event` for `severity in ('high', 'critical')`.
- Monitor application logs for `Security alert emitted`.
- For auth-related critical events, review login logs and refresh-token activity.
- For IP or ban-related critical events, review reverse proxy access logs and IP rules.
- Record incident response actions in audit notes or release/incident runbooks.
