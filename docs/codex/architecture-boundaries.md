WeCms.Core:
- No dependency on business modules
- No dependency on Infrastructure
- No dependency on Persistence

WeCms.Contracts:
- DTO only
- No database entities
- No SqlSugar attributes

WeCms.Abstractions:
- Interfaces only
- No implementation

WeCms.Infrastructure:
- Non-database infrastructure only
- Must not reference Persistence

WeCms.Persistence:
- Database implementation only
- SqlSugar allowed only here

WeCms.Modules.System:
- Business module
- Must not reference Persistence
- Must not reference SqlSugarCore

WeCms.Modules.Cms:
- Business module
- Must not reference Persistence
- Must not reference SqlSugarCore