# Tasks

- [x] Add failing tests for duplicate SQL sources, metadata table ownership, script bypass, and runtime hash seed source.
- [x] Replace embedded application SQL constants in `DbMigrationRunner` with a file-backed script provider.
- [x] Add checksum drift validation for already-applied schema migrations and seeds.
- [x] Split seed tracking into `sys_seed_migration`.
- [x] Route database scripts through the application migration runner.
- [x] Update seed SQL to use a Dapper parameter for the generated admin password hash.
- [ ] Run targeted tests, build, script syntax checks, and AOT publish where available.
