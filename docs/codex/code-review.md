Review checklist:

- Is the task scope minimal?
- Are unrelated files changed?
- Did any module reference Persistence?
- Did any module reference SqlSugarCore?
- Did any endpoint contain SQL?
- Did the change break AOT compatibility?
- Did the change alter API contracts unexpectedly?
- Are tests added or updated?
- Are validation commands run?