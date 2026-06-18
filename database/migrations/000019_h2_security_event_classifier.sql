ALTER TABLE sys_security_event
  ADD COLUMN source VARCHAR(80) NOT NULL DEFAULT 'system' AFTER severity,
  ADD COLUMN trace_id VARCHAR(64) NOT NULL DEFAULT 'unknown' AFTER message,
  ADD KEY ix_sys_security_event_source (source),
  ADD KEY ix_sys_security_event_trace_id (trace_id);
