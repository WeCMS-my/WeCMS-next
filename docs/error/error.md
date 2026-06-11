Run bash scripts/quality-gate-backend.sh all

&#x20; **bash scripts/quality-gate-backend.sh all**

shell: /usr/bin/bash -e {0}

env:

&#x20; DOTNET\_ROOT: /usr/share/dotnet

&#x20; WeCMS\_\_ConnectionStrings\_\_Default: Server=127.0.0.1;Port=3306;Database=wecms\_dev;User=root;\*\*\*;Charset=utf8mb4;

\=== WeCMS M0-BE Backend Quality Gate ===

\[1/15] dotnet build -warnaserror

&#x20; Determining projects to restore...

&#x20; All projects are up-to-date for restore.

&#x20; WeCms.Shared -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.dll

&#x20; WeCms.Infrastructure -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.dll

&#x20; WeCms.Modules.Cms -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.dll

&#x20; WeCms.Modules.System -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.dll

&#x20; WeCms.Persistence -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.dll

&#x20; WeCms.Api -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Api.dll

&#x20; WeCms.Tests.Integration -> /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Tests.Integration.dll

&#x20; WeCms.Tests.Unit -> /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Tests.Unit.dll

&#x20; WeCms.Tests.Architecture -> /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Tests.Architecture.dll

Build succeeded.

&#x20;   0 Warning(s)

&#x20;   0 Error(s)

Time Elapsed 00:00:10.73

&#x20; PASSED

\[2/15] AOT exception baseline check (ADR-0006)

&#x20; AOT warning baseline check passed (Dapper, Dapper.AOT, and MySqlConnector are aligned with ADR-0006).

&#x20; PASSED

\[3/15] AOT self-warning suppression check (ADR-0006)

&#x20; No self-owned IL2026/IL3050 suppressions detected in source.

&#x20; PASSED

\[4/15] dotnet publish (Native AOT)

&#x20; Determining projects to restore...

&#x20; Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/WeCms.Api.csproj (in 482 ms).

&#x20; Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/WeCms.Infrastructure.csproj (in 482 ms).

&#x20; Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/WeCms.Shared.csproj (in 482 ms).

&#x20; Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/WeCms.Modules.Cms.csproj (in 483 ms).

&#x20; Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/WeCms.Modules.System.csproj (in 6 ms).

&#x20; Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/WeCms.Persistence.csproj (in 13 ms).

&#x20; WeCms.Shared -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Release/net10.0/WeCms.Shared.dll

&#x20; WeCms.Infrastructure -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Release/net10.0/WeCms.Infrastructure.dll

&#x20; WeCms.Modules.Cms -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Release/net10.0/WeCms.Modules.Cms.dll

&#x20; WeCms.Modules.System -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Release/net10.0/WeCms.Modules.System.dll

&#x20; WeCms.Persistence -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Release/net10.0/WeCms.Persistence.dll

&#x20; WeCms.Api -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Release/net10.0/linux-x64/WeCms.Api.dll

&#x20; Generating native code

&#x20; WeCms.Api -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Release/net10.0/linux-x64/publish/

&#x20; PASSED

\[5/15] dotnet test

Build started 06/11/2026 08:49:00.

&#x20;    1>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" on node 1 (Restore target(s)).

&#x20;    1>ValidateSolutionConfiguration:

&#x20;        Building solution configuration "Debug|Any CPU".

&#x20;      \_GetAllRestoreProjectPathItems:

&#x20;        Determining projects to restore...

&#x20;    1>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" (1) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/WeCms.Persistence.csproj" (3:6) on node 1 (\_GenerateProjectRestoreGraph target(s)).

&#x20;    3>AddPrunePackageReferences:

&#x20;        Loading prune package data from PrunePackageData folder

&#x20;        Failed to load prune package data from PrunePackageData folder, loading from targeting packs instead

&#x20;        Looking for targeting packs in /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref

&#x20;        Pack directories found: /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9

&#x20;        /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.8

&#x20;        Found package overrides file /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/data/PackageOverrides.txt

&#x20;    1>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" (1) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/WeCms.Infrastructure.csproj" (10:8) on node 4 (\_GenerateProjectRestoreGraph target(s)).

&#x20;   10>AddPrunePackageReferences:

&#x20;        Loading prune package data from PrunePackageData folder

&#x20;        Failed to load prune package data from PrunePackageData folder, loading from targeting packs instead

&#x20;    1>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" (1) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/WeCms.Modules.Cms.csproj" (8:8) on node 2 (\_GenerateProjectRestoreGraph target(s)).

&#x20;    8>AddPrunePackageReferences:

&#x20;        Loading prune package data from PrunePackageData folder

&#x20;        Failed to load prune package data from PrunePackageData folder, loading from targeting packs instead

&#x20;    1>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" (1) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/WeCms.Modules.System.csproj" (9:8) on node 3 (\_GenerateProjectRestoreGraph target(s)).

&#x20;    9>AddPrunePackageReferences:

&#x20;        Loading prune package data from PrunePackageData folder

&#x20;        Failed to load prune package data from PrunePackageData folder, loading from targeting packs instead

&#x20;        Looking for targeting packs in /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref

&#x20;        Pack directories found: /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9

&#x20;        /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.8

&#x20;    8>AddPrunePackageReferences:

&#x20;        Looking for targeting packs in /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref

&#x20;   10>AddPrunePackageReferences:

&#x20;        Looking for targeting packs in /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref

&#x20;        Pack directories found: /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9

&#x20;        /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.8

&#x20;    8>AddPrunePackageReferences:

&#x20;        Pack directories found: /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9

&#x20;        /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.8

&#x20;    9>AddPrunePackageReferences:

&#x20;        Found package overrides file /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/data/PackageOverrides.txt

&#x20;   10>AddPrunePackageReferences:

&#x20;        Found package overrides file /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/data/PackageOverrides.txt

&#x20;    8>AddPrunePackageReferences:

&#x20;        Found package overrides file /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/data/PackageOverrides.txt

&#x20;    3>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/WeCms.Persistence.csproj" (\_GenerateProjectRestoreGraph target(s)).

&#x20;    9>AddPrunePackageReferences:

&#x20;        Loading prune package data from PrunePackageData folder

&#x20;        Failed to load prune package data from PrunePackageData folder, loading from targeting packs instead

&#x20;        Looking for targeting packs in /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref

&#x20;        Pack directories found: /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9

&#x20;        /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.8

&#x20;        Found package overrides file /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/data/PackageOverrides.txt

&#x20;    8>AddPrunePackageReferences:

&#x20;        Loading prune package data from PrunePackageData folder

&#x20;        Failed to load prune package data from PrunePackageData folder, loading from targeting packs instead

&#x20;        Looking for targeting packs in /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref

&#x20;        Pack directories found: /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9

&#x20;        /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.8

&#x20;        Found package overrides file /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/data/PackageOverrides.txt

&#x20;   10>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/WeCms.Infrastructure.csproj" (\_GenerateProjectRestoreGraph target(s)).

&#x20;    8>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/WeCms.Modules.Cms.csproj" (\_GenerateProjectRestoreGraph target(s)).

&#x20;    9>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/WeCms.Modules.System.csproj" (\_GenerateProjectRestoreGraph target(s)).

&#x20;    1>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" (1) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/WeCms.Api.csproj" (2:8) on node 1 (\_GenerateProjectRestoreGraph target(s)).

&#x20;    2>AddPrunePackageReferences:

&#x20;        Loading prune package data from PrunePackageData folder

&#x20;        Failed to load prune package data from PrunePackageData folder, loading from targeting packs instead

&#x20;        Looking for targeting packs in /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref

&#x20;        Pack directories found: /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9

&#x20;        /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.8

&#x20;        Found package overrides file /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/data/PackageOverrides.txt

&#x20;        Loading prune package data from PrunePackageData folder

&#x20;        Failed to load prune package data from PrunePackageData folder, loading from targeting packs instead

&#x20;        Looking for targeting packs in /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref

&#x20;        Pack directories found: /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9

&#x20;        /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.8

&#x20;        Found package overrides file /usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/data/PackageOverrides.txt

&#x20;    2>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/WeCms.Api.csproj" (\_GenerateProjectRestoreGraph target(s)).

&#x20;    1>Restore:

&#x20;        X.509 certificate chain validation will use the fallback certificate bundle at '/usr/share/dotnet/sdk/10.0.301/trustedroots/codesignctl.pem'.

&#x20;        X.509 certificate chain validation will use the fallback certificate bundle at '/usr/share/dotnet/sdk/10.0.301/trustedroots/timestampctl.pem'.

&#x20;        Assets file has not changed. Skipping assets file writing. Path: /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/obj/project.assets.json

&#x20;        Assets file has not changed. Skipping assets file writing. Path: /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/obj/project.assets.json

&#x20;        Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj (in 41 ms).

&#x20;        Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj (in 41 ms).

&#x20;        Assets file has not changed. Skipping assets file writing. Path: /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/obj/project.assets.json

&#x20;        Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj (in 4 ms).

&#x20;        Restoring packages for /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/WeCms.Infrastructure.csproj...

&#x20;        Restoring packages for /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/WeCms.Modules.System.csproj...

&#x20;        Restoring packages for /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/WeCms.Modules.Cms.csproj...

&#x20;        Restoring packages for /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/WeCms.Api.csproj...

&#x20;          CACHE <https://api.nuget.org/v3/vulnerabilities/index.json>

&#x20;          CACHE <https://api.nuget.org/v3-vulnerabilities/2026.06.11.05.34.05/vulnerability.base.json>

&#x20;          CACHE <https://api.nuget.org/v3-vulnerabilities/2026.06.11.05.34.05/2026.06.11.05.34.05/vulnerability.update.json>

&#x20;        Generating MSBuild file /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/obj/WeCms.Modules.Cms.csproj.nuget.g.props.

&#x20;        Generating MSBuild file /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/obj/WeCms.Modules.System.csproj.nuget.g.props.

&#x20;        Generating MSBuild file /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/obj/WeCms.Infrastructure.csproj.nuget.g.props.

&#x20;        Writing assets file to disk. Path: /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/obj/project.assets.json

&#x20;        Writing assets file to disk. Path: /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/obj/project.assets.json

&#x20;        Writing assets file to disk. Path: /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/obj/project.assets.json

&#x20;        Assets file has not changed. Skipping assets file writing. Path: /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/obj/project.assets.json

&#x20;        Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/WeCms.Infrastructure.csproj (in 192 ms).

&#x20;        Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/WeCms.Modules.Cms.csproj (in 237 ms).

&#x20;        Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/WeCms.Modules.System.csproj (in 237 ms).

&#x20;        Restoring packages for /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/WeCms.Shared.csproj...

&#x20;        Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/WeCms.Api.csproj (in 188 ms).

&#x20;        Generating MSBuild file /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/obj/WeCms.Shared.csproj.nuget.g.props.

&#x20;        Writing assets file to disk. Path: /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/obj/project.assets.json

&#x20;        Restoring packages for /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/WeCms.Persistence.csproj...

&#x20;        Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/WeCms.Shared.csproj (in 2 ms).

&#x20;        Generating MSBuild file /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/obj/WeCms.Persistence.csproj.nuget.g.props.

&#x20;        Writing assets file to disk. Path: /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/obj/project.assets.json

&#x20;        Restored /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/WeCms.Persistence.csproj (in 5 ms).

&#x20;        NuGet Config files used:

&#x20;            /home/runner/.nuget/NuGet/NuGet.Config

&#x20;        Feeds used:

<https://api.nuget.org/v3/index.json>

&#x20;        3 of 9 projects are up-to-date for restore.

&#x20;    1>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" (Restore target(s)).

&#x20;  1:2>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" on node 1 (VSTest target(s)).

&#x20;    1>ValidateSolutionConfiguration:

&#x20;        Building solution configuration "Debug|Any CPU".

&#x20;  1:2>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" (1:2) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj" (5:6) on node 2 (VSTest target(s)).

&#x20;    5>BuildProject:

&#x20;        Build started, please wait...

&#x20;  1:2>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" (1:2) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj" (7:6) on node 4 (VSTest target(s)).

&#x20;    7>BuildProject:

&#x20;        Build started, please wait...

&#x20;  1:2>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" (1:2) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj" (6:6) on node 3 (VSTest target(s)).

&#x20;    6>BuildProject:

&#x20;        Build started, please wait...

&#x20;  6:6>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj" (6:6) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj" (6:7) on node 3 (default targets).

&#x20;  6:7>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj" (6:7) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/WeCms.Modules.Cms.csproj" (8:13) on node 3 (default targets).

&#x20; 8:13>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/WeCms.Modules.Cms.csproj" (8:13) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/WeCms.Shared.csproj" (4:20) on node 3 (default targets).

&#x20;    4>GenerateTargetFrameworkMonikerAttribute:

&#x20;      Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.

&#x20;      CoreGenerateAssemblyInfo:

&#x20;      Skipping target "CoreGenerateAssemblyInfo" because all output files are up-to-date with respect to the input files.

&#x20;      \_GenerateSourceLinkFile:

&#x20;        Source Link file 'obj/Debug/net10.0/WeCms.Shared.sourcelink.json' is up-to-date.

&#x20;      CoreCompile:

&#x20;        Setting DOTNET\_ROOT to '/usr/share/dotnet'

&#x20;        /usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc /noconfig /unsafe- /checked- /nowarn:1701,1702,1701,1702,8002 /fullpaths /nostdlib+ /errorreport:prompt /warn:10 /define:TRACE;DEBUG;NET;NET10\_0;NETCOREAPP;NET5\_0\_OR\_GREATER;NET6\_0\_OR\_GREATER;NET7\_0\_OR\_GREATER;NET8\_0\_OR\_GREATER;NET9\_0\_OR\_GREATER;NET10\_0\_OR\_GREATER;NETCOREAPP1\_0\_OR\_GREATER;NETCOREAPP1\_1\_OR\_GREATER;NETCOREAPP2\_0\_OR\_GREATER;NETCOREAPP2\_1\_OR\_GREATER;NETCOREAPP2\_2\_OR\_GREATER;NETCOREAPP3\_0\_OR\_GREATER;NETCOREAPP3\_1\_OR\_GREATER /highentropyva+ /nullable:enable /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0/Microsoft.CSharp.dll /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0/Microsoft.VisualBasic.Core.dll /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0/Microsoft.VisualBasic.dll /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0/Microsoft.Win32.Primitives.dll /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10

&#x20;        Compilation request WeCms.Shared (net10.0), PathToTool=/usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc

&#x20;        CommandLine = ' /noconfig'

&#x20;        BuildResponseFile = '/unsafe- /checked- /nowarn:1701,1702,1701,1702,8002 /fullpaths /nostdlib+ /errorreport:prompt /warn:10 /define:TRACE;DEBUG;NET;NET10\_0;NETCOREAPP;NET5\_0\_OR\_GREATER;NET6\_0\_OR\_GREATER;NET7\_0\_OR\_GREATER;NET8\_0\_OR\_GREATER;NET9\_0\_OR\_GREATER;NET10\_0\_OR\_GREATER;NETCOREAPP1\_0\_OR\_GREATER;NETCOREAPP1\_1\_OR\_GREATER;NETCOREAPP2\_0\_OR\_GREATER;NETCOREAPP2\_1\_OR\_GREATER;NETCOREAPP2\_2\_OR\_GREATER;NETCOREAPP3\_0\_OR\_GREATER;NETCOREAPP3\_1\_OR\_GREATER /highentropyva+ /nullable:enable /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0/Microsoft.CSharp.dll /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0/Microsoft.VisualBasic.Core.dll /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0/Microsoft.VisualBasic.dll /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0/Microsoft.Win32.Primitives.dll /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0/Microsoft.Win32.Regist

&#x20;        Attempt to open named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw'

&#x20;        Attempt to connect named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw'

&#x20;        Named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw' connected

&#x20;        Begin writing request for WeCms.Shared (net10.0)

&#x20;        End writing request for WeCms.Shared (net10.0)

&#x20;        Begin reading response for WeCms.Shared (net10.0)

&#x20;        End reading response for WeCms.Shared (net10.0)

&#x20;        CompilerServer: server - server processed compilation - WeCms.Shared (net10.0)

&#x20;      CopyFilesToOutputDirectory:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/obj/Debug/net10.0/WeCms.Shared.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.dll".

&#x20;        WeCms.Shared -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.dll

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/obj/Debug/net10.0/WeCms.Shared.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.pdb".

&#x20;    4>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/WeCms.Shared.csproj" (default targets).

&#x20;  5:6>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj" (5:6) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj" (5:7) on node 2 (default targets).

&#x20;  5:7>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj" (5:7) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/WeCms.Infrastructure.csproj" (10:13) on node 2 (default targets).

&#x20;   10>GenerateTargetFrameworkMonikerAttribute:

&#x20;      Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.

&#x20;    8>GenerateTargetFrameworkMonikerAttribute:

&#x20;      Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.

&#x20;      CoreGenerateAssemblyInfo:

&#x20;      Skipping target "CoreGenerateAssemblyInfo" because all output files are up-to-date with respect to the input files.

&#x20;      \_GenerateSourceLinkFile:

&#x20;        Source Link file 'obj/Debug/net10.0/WeCms.Modules.Cms.sourcelink.json' is up-to-date.

&#x20;      CoreCompile:

&#x20;        Setting DOTNET\_ROOT to '/usr/share/dotnet'

&#x20;        /usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc /noconfig /unsafe- /checked- /nowarn:1701,1702,1701,1702,8002 /fullpaths /nostdlib+ /errorreport:prompt /warn:10 /define:TRACE;DEBUG;NET;NET10\_0;NETCOREAPP;NET5\_0\_OR\_GREATER;NET6\_0\_OR\_GREATER;NET7\_0\_OR\_GREATER;NET8\_0\_OR\_GREATER;NET9\_0\_OR\_GREATER;NET10\_0\_OR\_GREATER;NETCOREAPP1\_0\_OR\_GREATER;NETCOREAPP1\_1\_OR\_GREATER;NETCOREAPP2\_0\_OR\_GREATER;NETCOREAPP2\_1\_OR\_GREATER;NETCOREAPP2\_2\_OR\_GREATER;NETCOREAPP3\_0\_OR\_GREATER;NETCOREAPP3\_1\_OR\_GREATER /highentropyva+ /nullable:enable /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Antiforgery.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.Abstractions.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.BearerToken.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCo

&#x20;        Compilation request WeCms.Modules.Cms (net10.0), PathToTool=/usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc

&#x20;        CommandLine = ' /noconfig'

&#x20;        BuildResponseFile = '/unsafe- /checked- /nowarn:1701,1702,1701,1702,8002 /fullpaths /nostdlib+ /errorreport:prompt /warn:10 /define:TRACE;DEBUG;NET;NET10\_0;NETCOREAPP;NET5\_0\_OR\_GREATER;NET6\_0\_OR\_GREATER;NET7\_0\_OR\_GREATER;NET8\_0\_OR\_GREATER;NET9\_0\_OR\_GREATER;NET10\_0\_OR\_GREATER;NETCOREAPP1\_0\_OR\_GREATER;NETCOREAPP1\_1\_OR\_GREATER;NETCOREAPP2\_0\_OR\_GREATER;NETCOREAPP2\_1\_OR\_GREATER;NETCOREAPP2\_2\_OR\_GREATER;NETCOREAPP3\_0\_OR\_GREATER;NETCOREAPP3\_1\_OR\_GREATER /highentropyva+ /nullable:enable /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Antiforgery.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.Abstractions.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.BearerToken.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.Cookies.dll /referenc

&#x20;        Attempt to open named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw'

&#x20;        Attempt to connect named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw'

&#x20;        Named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw' connected

&#x20;        Begin writing request for WeCms.Modules.Cms (net10.0)

&#x20;        End writing request for WeCms.Modules.Cms (net10.0)

&#x20;        Begin reading response for WeCms.Modules.Cms (net10.0)

&#x20;   10>CoreGenerateAssemblyInfo:

&#x20;      Skipping target "CoreGenerateAssemblyInfo" because all output files are up-to-date with respect to the input files.

&#x20;    8>CoreCompile:

&#x20;        End reading response for WeCms.Modules.Cms (net10.0)

&#x20;        CompilerServer: server - server processed compilation - WeCms.Modules.Cms (net10.0)

&#x20;      \_CopyFilesMarkedCopyLocal:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Shared.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Shared.pdb".

&#x20;  5:7>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj" (5:7) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/WeCms.Modules.System.csproj" (9:13) on node 1 (default targets).

&#x20;    9>GenerateTargetFrameworkMonikerAttribute:

&#x20;      Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.

&#x20;    8>\_CopyFilesMarkedCopyLocal:

&#x20;        Touching "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/obj/Debug/net10.0/WeCms.Mo.B118CC2E.Up2Date".

&#x20;   10>\_GenerateSourceLinkFile:

&#x20;        Source Link file 'obj/Debug/net10.0/WeCms.Infrastructure.sourcelink.json' is up-to-date.

&#x20;      CoreCompile:

&#x20;        Setting DOTNET\_ROOT to '/usr/share/dotnet'

&#x20;    8>CopyFilesToOutputDirectory:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/obj/Debug/net10.0/WeCms.Modules.Cms.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.dll".

&#x20;        WeCms.Modules.Cms -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.dll

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/obj/Debug/net10.0/WeCms.Modules.Cms.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.pdb".

&#x20;   10>CoreCompile:

&#x20;        /usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc /noconfig /unsafe- /checked- /nowarn:1701,1702,1701,1702,8002 /fullpaths /nostdlib+ /errorreport:prompt /warn:10 /define:TRACE;DEBUG;NET;NET10\_0;NETCOREAPP;NET5\_0\_OR\_GREATER;NET6\_0\_OR\_GREATER;NET7\_0\_OR\_GREATER;NET8\_0\_OR\_GREATER;NET9\_0\_OR\_GREATER;NET10\_0\_OR\_GREATER;NETCOREAPP1\_0\_OR\_GREATER;NETCOREAPP1\_1\_OR\_GREATER;NETCOREAPP2\_0\_OR\_GREATER;NETCOREAPP2\_1\_OR\_GREATER;NETCOREAPP2\_2\_OR\_GREATER;NETCOREAPP3\_0\_OR\_GREATER;NETCOREAPP3\_1\_OR\_GREATER /highentropyva+ /nullable:enable /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0/Microsoft.CSharp.dll /reference:/home/runner/.nuget/packages/microsoft.extensions.configuration.abstractions/10.0.0/lib/net10.0/Microsoft.Extensions.Configuration.Abstractions.dll /reference:/home/runner/.nuget/packages/microsoft.extensions.dependencyinjection.abstractions/10.0.0/lib/net10.0/Microsoft.Extensions.DependencyInjection.Abstractions.dll /reference:/home/runner/.nuget/packages/microsoft.extensions.

&#x20;        Compilation request WeCms.Infrastructure (net10.0), PathToTool=/usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc

&#x20;        CommandLine = ' /noconfig'

&#x20;        BuildResponseFile = '/unsafe- /checked- /nowarn:1701,1702,1701,1702,8002 /fullpaths /nostdlib+ /errorreport:prompt /warn:10 /define:TRACE;DEBUG;NET;NET10\_0;NETCOREAPP;NET5\_0\_OR\_GREATER;NET6\_0\_OR\_GREATER;NET7\_0\_OR\_GREATER;NET8\_0\_OR\_GREATER;NET9\_0\_OR\_GREATER;NET10\_0\_OR\_GREATER;NETCOREAPP1\_0\_OR\_GREATER;NETCOREAPP1\_1\_OR\_GREATER;NETCOREAPP2\_0\_OR\_GREATER;NETCOREAPP2\_1\_OR\_GREATER;NETCOREAPP2\_2\_OR\_GREATER;NETCOREAPP3\_0\_OR\_GREATER;NETCOREAPP3\_1\_OR\_GREATER /highentropyva+ /nullable:enable /reference:/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.9/ref/net10.0/Microsoft.CSharp.dll /reference:/home/runner/.nuget/packages/microsoft.extensions.configuration.abstractions/10.0.0/lib/net10.0/Microsoft.Extensions.Configuration.Abstractions.dll /reference:/home/runner/.nuget/packages/microsoft.extensions.dependencyinjection.abstractions/10.0.0/lib/net10.0/Microsoft.Extensions.DependencyInjection.Abstractions.dll /reference:/home/runner/.nuget/packages/microsoft.extensions.logging.abstractions/10.0.0/lib/net10.0

&#x20;        Attempt to open named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw'

&#x20;        Attempt to connect named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw'

&#x20;        Named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw' connected

&#x20;        Begin writing request for WeCms.Infrastructure (net10.0)

&#x20;    8>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/WeCms.Modules.Cms.csproj" (default targets).

&#x20;   10>CoreCompile:

&#x20;        End writing request for WeCms.Infrastructure (net10.0)

&#x20;        Begin reading response for WeCms.Infrastructure (net10.0)

&#x20;    9>CoreGenerateAssemblyInfo:

&#x20;      Skipping target "CoreGenerateAssemblyInfo" because all output files are up-to-date with respect to the input files.

&#x20;      \_GenerateSourceLinkFile:

&#x20;        Source Link file 'obj/Debug/net10.0/WeCms.Modules.System.sourcelink.json' is up-to-date.

&#x20;      CoreCompile:

&#x20;        Setting DOTNET\_ROOT to '/usr/share/dotnet'

&#x20;        /usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc /noconfig /unsafe- /checked- /nowarn:1701,1702,1701,1702,8002 /fullpaths /nostdlib+ /errorreport:prompt /warn:10 /define:TRACE;DEBUG;NET;NET10\_0;NETCOREAPP;NET5\_0\_OR\_GREATER;NET6\_0\_OR\_GREATER;NET7\_0\_OR\_GREATER;NET8\_0\_OR\_GREATER;NET9\_0\_OR\_GREATER;NET10\_0\_OR\_GREATER;NETCOREAPP1\_0\_OR\_GREATER;NETCOREAPP1\_1\_OR\_GREATER;NETCOREAPP2\_0\_OR\_GREATER;NETCOREAPP2\_1\_OR\_GREATER;NETCOREAPP2\_2\_OR\_GREATER;NETCOREAPP3\_0\_OR\_GREATER;NETCOREAPP3\_1\_OR\_GREATER /highentropyva+ /nullable:enable /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Antiforgery.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.Abstractions.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.BearerToken.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCo

&#x20;        Compilation request WeCms.Modules.System (net10.0), PathToTool=/usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc

&#x20;        CommandLine = ' /noconfig'

&#x20;        BuildResponseFile = '/unsafe- /checked- /nowarn:1701,1702,1701,1702,8002 /fullpaths /nostdlib+ /errorreport:prompt /warn:10 /define:TRACE;DEBUG;NET;NET10\_0;NETCOREAPP;NET5\_0\_OR\_GREATER;NET6\_0\_OR\_GREATER;NET7\_0\_OR\_GREATER;NET8\_0\_OR\_GREATER;NET9\_0\_OR\_GREATER;NET10\_0\_OR\_GREATER;NETCOREAPP1\_0\_OR\_GREATER;NETCOREAPP1\_1\_OR\_GREATER;NETCOREAPP2\_0\_OR\_GREATER;NETCOREAPP2\_1\_OR\_GREATER;NETCOREAPP2\_2\_OR\_GREATER;NETCOREAPP3\_0\_OR\_GREATER;NETCOREAPP3\_1\_OR\_GREATER /highentropyva+ /nullable:enable /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Antiforgery.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.Abstractions.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.BearerToken.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.Cookies.dll /referenc

&#x20;        Attempt to open named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw'

&#x20;        Attempt to connect named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw'

&#x20;        Named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw' connected

&#x20;        Begin writing request for WeCms.Modules.System (net10.0)

&#x20;        End writing request for WeCms.Modules.System (net10.0)

&#x20;        Begin reading response for WeCms.Modules.System (net10.0)

&#x20;   10>CoreCompile:

&#x20;        End reading response for WeCms.Infrastructure (net10.0)

&#x20;        CompilerServer: server - server processed compilation - WeCms.Infrastructure (net10.0)

&#x20;      \_CopyFilesMarkedCopyLocal:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Shared.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Shared.pdb".

&#x20;        Touching "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/obj/Debug/net10.0/WeCms.In.68B03D10.Up2Date".

&#x20;      CopyFilesToOutputDirectory:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/obj/Debug/net10.0/WeCms.Infrastructure.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.dll".

&#x20;        WeCms.Infrastructure -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.dll

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/obj/Debug/net10.0/WeCms.Infrastructure.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.pdb".

&#x20;   10>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/WeCms.Infrastructure.csproj" (default targets).

&#x20;    9>CoreCompile:

&#x20;        End reading response for WeCms.Modules.System (net10.0)

&#x20;        CompilerServer: server - server processed compilation - WeCms.Modules.System (net10.0)

&#x20;      \_CopyFilesMarkedCopyLocal:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Shared.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Shared.pdb".

&#x20;        Touching "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/obj/Debug/net10.0/WeCms.Mo.B472F750.Up2Date".

&#x20;      CopyFilesToOutputDirectory:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/obj/Debug/net10.0/WeCms.Modules.System.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.dll".

&#x20;        WeCms.Modules.System -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.dll

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/obj/Debug/net10.0/WeCms.Modules.System.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.pdb".

&#x20;    9>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/WeCms.Modules.System.csproj" (default targets).

&#x20;  7:6>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj" (7:6) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj" (7:7) on node 4 (default targets).

&#x20;  7:7>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj" (7:7) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/WeCms.Api.csproj" (2:13) on node 4 (default targets).

&#x20; 2:13>Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/WeCms.Api.csproj" (2:13) is building "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/WeCms.Persistence.csproj" (3:12) on node 2 (default targets).

&#x20;    3>GenerateTargetFrameworkMonikerAttribute:

&#x20;      Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.

&#x20;      CoreGenerateAssemblyInfo:

&#x20;      Skipping target "CoreGenerateAssemblyInfo" because all output files are up-to-date with respect to the input files.

&#x20;      \_GenerateSourceLinkFile:

&#x20;        Source Link file 'obj/Debug/net10.0/WeCms.Persistence.sourcelink.json' is up-to-date.

&#x20;      CoreCompile:

&#x20;        Setting DOTNET\_ROOT to '/usr/share/dotnet'

&#x20;        /usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc /noconfig /unsafe- /checked- /nowarn:1701,1702,1701,1702,8002 /fullpaths /nostdlib+ /errorreport:prompt /warn:10 /define:TRACE;DEBUG;NET;NET10\_0;NETCOREAPP;NET5\_0\_OR\_GREATER;NET6\_0\_OR\_GREATER;NET7\_0\_OR\_GREATER;NET8\_0\_OR\_GREATER;NET9\_0\_OR\_GREATER;NET10\_0\_OR\_GREATER;NETCOREAPP1\_0\_OR\_GREATER;NETCOREAPP1\_1\_OR\_GREATER;NETCOREAPP2\_0\_OR\_GREATER;NETCOREAPP2\_1\_OR\_GREATER;NETCOREAPP2\_2\_OR\_GREATER;NETCOREAPP3\_0\_OR\_GREATER;NETCOREAPP3\_1\_OR\_GREATER /highentropyva+ /nullable:enable /reference:/home/runner/.nuget/packages/dapper.aot/1.0.31/lib/net8.0/Dapper.AOT.dll /reference:/home/runner/.nuget/packages/dapper/2.1.66/lib/net8.0/Dapper.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Antiforgery.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.Abstractions.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/n

&#x20;        Compilation request WeCms.Persistence (net10.0), PathToTool=/usr/share/dotnet/sdk/10.0.301/Roslyn/bincore/csc

&#x20;        CommandLine = ' /noconfig'

&#x20;        BuildResponseFile = '/unsafe- /checked- /nowarn:1701,1702,1701,1702,8002 /fullpaths /nostdlib+ /errorreport:prompt /warn:10 /define:TRACE;DEBUG;NET;NET10\_0;NETCOREAPP;NET5\_0\_OR\_GREATER;NET6\_0\_OR\_GREATER;NET7\_0\_OR\_GREATER;NET8\_0\_OR\_GREATER;NET9\_0\_OR\_GREATER;NET10\_0\_OR\_GREATER;NETCOREAPP1\_0\_OR\_GREATER;NETCOREAPP1\_1\_OR\_GREATER;NETCOREAPP2\_0\_OR\_GREATER;NETCOREAPP2\_1\_OR\_GREATER;NETCOREAPP2\_2\_OR\_GREATER;NETCOREAPP3\_0\_OR\_GREATER;NETCOREAPP3\_1\_OR\_GREATER /highentropyva+ /nullable:enable /reference:/home/runner/.nuget/packages/dapper.aot/1.0.31/lib/net8.0/Dapper.AOT.dll /reference:/home/runner/.nuget/packages/dapper/2.1.66/lib/net8.0/Dapper.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Antiforgery.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authentication.Abstractions.dll /reference:/usr/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.9/ref/net10.0/Microsoft.AspNetCore.Authenticat

&#x20;        Attempt to open named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw'

&#x20;        Attempt to connect named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw'

&#x20;        Named pipe '\_GTn1bTPUq7Zg4GL+bdbSQhiFL088OW+cWBzxmgt1Gw' connected

&#x20;        Begin writing request for WeCms.Persistence (net10.0)

&#x20;        End writing request for WeCms.Persistence (net10.0)

&#x20;        Begin reading response for WeCms.Persistence (net10.0)

&#x20;        End reading response for WeCms.Persistence (net10.0)

&#x20;        CompilerServer: server - server processed compilation - WeCms.Persistence (net10.0)

&#x20;      \_CopyFilesMarkedCopyLocal:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Modules.Cms.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Modules.System.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Shared.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Shared.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Modules.System.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Modules.Cms.pdb".

&#x20;        Touching "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/obj/Debug/net10.0/WeCms.Pe.4B18087F.Up2Date".

&#x20;      CopyFilesToOutputDirectory:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/obj/Debug/net10.0/WeCms.Persistence.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.dll".

&#x20;        WeCms.Persistence -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.dll

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/obj/Debug/net10.0/WeCms.Persistence.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.pdb".

&#x20;    3>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/WeCms.Persistence.csproj" (default targets).

&#x20;    2>GenerateTargetFrameworkMonikerAttribute:

&#x20;      Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.

&#x20;      CoreGenerateAssemblyInfo:

&#x20;      Skipping target "CoreGenerateAssemblyInfo" because all output files are up-to-date with respect to the input files.

&#x20;      \_DiscoverMvcApplicationParts:

&#x20;      Skipping target "\_DiscoverMvcApplicationParts" because all output files are up-to-date with respect to the input files.

&#x20;      \_GenerateSourceLinkFile:

&#x20;        Source Link file 'obj/Debug/net10.0/WeCms.Api.sourcelink.json' is up-to-date.

&#x20;      CoreCompile:

&#x20;      Skipping target "CoreCompile" because all output files are up-to-date with respect to the input files.

&#x20;      \_CreateAppHost:

&#x20;      Skipping target "\_CreateAppHost" because all output files are up-to-date with respect to the input files.

&#x20;      \_ProcessScopedCssFiles:

&#x20;      Skipping target "\_ProcessScopedCssFiles" because it has no outputs.

&#x20;      \_ProcessScopedCssFiles:

&#x20;      Skipping target "\_ProcessScopedCssFiles" because it has no outputs.

&#x20;      \_BuildCopyStaticWebAssetsPreserveNewest:

&#x20;      Skipping target "\_BuildCopyStaticWebAssetsPreserveNewest" because it has no outputs.

&#x20;      \_CopyFilesMarkedCopyLocal:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Infrastructure.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Modules.Cms.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Persistence.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Shared.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Modules.System.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Modules.System.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Modules.Cms.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Infrastructure.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Shared.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Persistence.pdb".

&#x20;        Touching "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/obj/Debug/net10.0/WeCms.Api.csproj.Up2Date".

&#x20;      \_CopyOutOfDateSourceItemsToOutputDirectory:

&#x20;      Skipping target "\_CopyOutOfDateSourceItemsToOutputDirectory" because all output files are up-to-date with respect to the input files.

&#x20;      GenerateBuildDependencyFile:

&#x20;      Skipping target "GenerateBuildDependencyFile" because all output files are up-to-date with respect to the input files.

&#x20;      GenerateBuildRuntimeConfigurationFiles:

&#x20;      Skipping target "GenerateBuildRuntimeConfigurationFiles" because all output files are up-to-date with respect to the input files.

&#x20;      CopyFilesToOutputDirectory:

&#x20;        WeCms.Api -> /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Api.dll

&#x20;    2>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/WeCms.Api.csproj" (default targets).

&#x20;    6>GenerateTargetFrameworkMonikerAttribute:

&#x20;      Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.

&#x20;      CoreGenerateAssemblyInfo:

&#x20;      Skipping target "CoreGenerateAssemblyInfo" because all output files are up-to-date with respect to the input files.

&#x20;    5>GenerateTargetFrameworkMonikerAttribute:

&#x20;      Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.

&#x20;      CoreGenerateAssemblyInfo:

&#x20;      Skipping target "CoreGenerateAssemblyInfo" because all output files are up-to-date with respect to the input files.

&#x20;    7>GenerateTargetFrameworkMonikerAttribute:

&#x20;      Skipping target "GenerateTargetFrameworkMonikerAttribute" because all output files are up-to-date with respect to the input files.

&#x20;      CoreGenerateAssemblyInfo:

&#x20;      Skipping target "CoreGenerateAssemblyInfo" because all output files are up-to-date with respect to the input files.

&#x20;    5>\_GenerateSourceLinkFile:

&#x20;        Source Link file 'obj/Debug/net10.0/WeCms.Tests.Architecture.sourcelink.json' is up-to-date.

&#x20;      CoreCompile:

&#x20;      Skipping target "CoreCompile" because all output files are up-to-date with respect to the input files.

&#x20;      \_CopyFilesMarkedCopyLocal:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Modules.System.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Persistence.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Modules.Cms.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Infrastructure.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Infrastructure.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Modules.System.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Shared.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Persistence.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Shared.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Modules.Cms.pdb".

&#x20;        Touching "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/obj/Debug/net10.0/WeCms.Te.1B725674.Up2Date".

&#x20;      \_CopyOutOfDateSourceItemsToOutputDirectory:

&#x20;      Skipping target "\_CopyOutOfDateSourceItemsToOutputDirectory" because all output files are up-to-date with respect to the input files.

&#x20;      GenerateBuildDependencyFile:

&#x20;      Skipping target "GenerateBuildDependencyFile" because all output files are up-to-date with respect to the input files.

&#x20;      GenerateBuildRuntimeConfigurationFiles:

&#x20;      Skipping target "GenerateBuildRuntimeConfigurationFiles" because all output files are up-to-date with respect to the input files.

&#x20;      CopyFilesToOutputDirectory:

&#x20;        WeCms.Tests.Architecture -> /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Tests.Architecture.dll

&#x20;    6>\_GenerateSourceLinkFile:

&#x20;        Source Link file 'obj/Debug/net10.0/WeCms.Tests.Integration.sourcelink.json' is up-to-date.

&#x20;    5>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj" (default targets).

&#x20;    6>CoreCompile:

&#x20;      Skipping target "CoreCompile" because all output files are up-to-date with respect to the input files.

&#x20;      \_CopyFilesMarkedCopyLocal:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Infrastructure.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Modules.Cms.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Modules.System.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Persistence.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Shared.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Infrastructure.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Modules.Cms.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Modules.System.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Shared.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Persistence.pdb".

&#x20;        Touching "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/obj/Debug/net10.0/WeCms.Te.C496E711.Up2Date".

&#x20;    5>BuildProject:

&#x20;        Build completed.

Test run for /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/bin/Debug/net10.0/WeCms.Tests.Architecture.dll (.NETCoreApp,Version=v10.0)

&#x20;    7>\_GenerateSourceLinkFile:

&#x20;        Source Link file 'obj/Debug/net10.0/WeCms.Tests.Unit.sourcelink.json' is up-to-date.

&#x20;      CoreCompile:

&#x20;      Skipping target "CoreCompile" because all output files are up-to-date with respect to the input files.

&#x20;      \_CopyFilesMarkedCopyLocal:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Modules.Cms.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Infrastructure.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Modules.System.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Persistence.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Modules.Cms.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.dll" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Shared.dll".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Modules.System.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Shared.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Persistence.pdb".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.pdb" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Infrastructure.pdb".

&#x20;        Touching "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/obj/Debug/net10.0/WeCms.Te.4E5C7CEE.Up2Date".

&#x20;    6>\_CopyOutOfDateSourceItemsToOutputDirectory:

&#x20;      Building target "\_CopyOutOfDateSourceItemsToOutputDirectory" partially, because some output files are out of date with respect to their input files.

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/obj/Debug/net10.0/MvcTestingAppManifest.json" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/MvcTestingAppManifest.json".

&#x20;      GenerateBuildDependencyFile:

&#x20;      Skipping target "GenerateBuildDependencyFile" because all output files are up-to-date with respect to the input files.

&#x20;      GenerateBuildRuntimeConfigurationFiles:

&#x20;      Skipping target "GenerateBuildRuntimeConfigurationFiles" because all output files are up-to-date with respect to the input files.

&#x20;      CopyFilesToOutputDirectory:

&#x20;        WeCms.Tests.Integration -> /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Tests.Integration.dll

&#x20;    7>\_CopyOutOfDateSourceItemsToOutputDirectory:

&#x20;      Skipping target "\_CopyOutOfDateSourceItemsToOutputDirectory" because all output files are up-to-date with respect to the input files.

&#x20;      GenerateBuildDependencyFile:

&#x20;      Skipping target "GenerateBuildDependencyFile" because all output files are up-to-date with respect to the input files.

&#x20;      GenerateBuildRuntimeConfigurationFiles:

&#x20;      Skipping target "GenerateBuildRuntimeConfigurationFiles" because all output files are up-to-date with respect to the input files.

&#x20;      CopyFilesToOutputDirectory:

&#x20;        WeCms.Tests.Unit -> /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Tests.Unit.dll

&#x20;    7>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj" (default targets).

Test run for /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/bin/Debug/net10.0/WeCms.Tests.Unit.dll (.NETCoreApp,Version=v10.0)

&#x20;    7>BuildProject:

&#x20;        Build completed.

&#x20;    6>\_MvcCopyDependencyFiles:

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/bin/Debug/net10.0/WeCms.Api.deps.json" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Api.deps.json".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Infrastructure/bin/Debug/net10.0/WeCms.Infrastructure.deps.json" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Infrastructure.deps.json".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.Cms/bin/Debug/net10.0/WeCms.Modules.Cms.deps.json" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Modules.Cms.deps.json".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Modules.System/bin/Debug/net10.0/WeCms.Modules.System.deps.json" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Modules.System.deps.json".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/bin/Debug/net10.0/WeCms.Persistence.deps.json" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Persistence.deps.json".

&#x20;        Copying file from "/home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Shared/bin/Debug/net10.0/WeCms.Shared.deps.json" to "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Shared.deps.json".

&#x20;    6>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj" (default targets).

&#x20;    6>BuildProject:

&#x20;        Build completed.

Test run for /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/bin/Debug/net10.0/WeCms.Tests.Integration.dll (.NETCoreApp,Version=v10.0)

A total of 1 test files matched the specified pattern.

A total of 1 test files matched the specified pattern.

A total of 1 test files matched the specified pattern.

\[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.9)

\[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.9)

\[xUnit.net 00:00:00.23]   Discovering: WeCms.Tests.Unit

\[xUnit.net 00:00:00.15]   Discovering: WeCms.Tests.Integration

\[xUnit.net 00:00:00.34]   Discovered:  WeCms.Tests.Unit

\[xUnit.net 00:00:00.40]   Starting:    WeCms.Tests.Unit

\[xUnit.net 00:00:00.28]   Discovered:  WeCms.Tests.Integration

\[xUnit.net 00:00:00.33]   Starting:    WeCms.Tests.Integration

\[xUnit.net 00:00:00.01] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.9)

\[xUnit.net 00:00:00.29]   Discovering: WeCms.Tests.Architecture

&#x20; Passed WeCms.Tests.Unit.Infrastructure.Data.DbConnectionFactoryTests.Constructor\_ShouldReadConnectionString\_FromConfiguration \[6 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.DomainExceptionTests.ShouldBeThrowable \[8 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.DomainExceptionTests.Constructor\_ShouldSetProperties \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Infrastructure.Data.DbConnectionFactoryTests.Constructor\_ShouldThrow\_WhenConnectionStringMissing \[8 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.ApiResultTests.Ok\_ShouldReturnSuccess\_WithTraceId \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.ApiResultTests.Ok\_ShouldReturnSuccess\_WhenGivenData \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.ApiResultTests.Fail\_ShouldReturnError\_WithCodeAndMessage \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Auth.AuthServiceTests.LoginAsync\_ShouldExecutePersistenceStepsInSameTransaction \[25 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.PermissionMetadataTests.Constructor\_ShouldSetCode \[4 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.PermissionMetadataTests.Code\_ShouldNotBeEmpty\_WhenValid \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.ApiResultTests.Fail\_ShouldReturnError\_WithFieldErrors \[8 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.PagedResultTests.Constructor\_ShouldHandleEmptyRecords \[8 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.PagedResultTests.Constructor\_ShouldSetAllProperties \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.PermissionCheckResultTests.Inactive\_ShouldSetHasPermissionFalse \[3 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.PermissionMetadataTests.TwoInstances\_WithSameCode\_ShouldBeEqual \[17 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.PermissionCheckResultTests.Active\_WithoutPermission\_ShouldSetCorrectly \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.SystemPermissionsTests.SystemSecurePing\_ShouldFollowNamingConvention \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.PermissionCheckResultTests.Active\_WithPermission\_ShouldSetBothTrue \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.SystemPermissionsTests.AllPermissions\_ShouldBeNonEmpty \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.PaginationRequestTests.MetadataOnly\_ShouldBeOneBased \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.PaginationRequestTests.Constructor\_ShouldSetDefaults \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.PaginationRequestTests.Constructor\_ShouldSetCustomValues \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.CurrentUserTests.Constructor\_ShouldSetProperties \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.CurrentUserTests.IsAuthenticated\_ShouldBeFalse\_WhenAnonymous \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.CurrentUserTests.Anonymous\_ShouldHaveZeroId \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.CurrentUserTests.IsAuthenticated\_ShouldBeTrue\_WhenHasId \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Infrastructure.Time.SystemClockTests.UtcNow\_ShouldReturnCurrentTime \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Auth.AuthServiceTests.RefreshAsync\_WhenRevokeRowsIsZero\_ShouldRollbackAndThrowUnauthorized \[23 ms]

&#x20; Passed WeCms.Tests.Unit.Auth.AuthServiceTests.RefreshAsync\_ShouldQueryAndRotateTokenInSameTransaction \[1 ms]

&#x20; Passed WeCms.Tests.Unit.Auth.AuthServiceTests.LoginAsync\_WhenInsertRefreshTokenFails\_ShouldRollbackAndThrowSystemError \[17 ms]

&#x20; Passed WeCms.Tests.Unit.Infrastructure.Time.SystemClockTests.UtcNow\_ShouldReturnUtcTime \[19 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.ApiCodesTests.SystemError\_ShouldBe5000 \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.ApiCodesTests.BusinessError\_ShouldBe2001 \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.ApiCodesTests.Unauthorized\_ShouldBe401 \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Permissions.PermissionEndpointFilterTests.InvokeAsync\_ShouldShortCircuit\_WhenUserDisabled \[23 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.ApiCodesTests.Forbidden\_ShouldBe403 \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.ApiCodesTests.NotFound\_ShouldBe404 \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Shared.ApiCodesTests.ValidationError\_ShouldBe1001 \[< 1 ms]

\[xUnit.net 00:00:00.66]   Finished:    WeCms.Tests.Unit

\[xUnit.net 00:00:00.39]   Discovered:  WeCms.Tests.Architecture

\[xUnit.net 00:00:00.46]   Starting:    WeCms.Tests.Architecture

&#x20; Passed WeCms.Tests.Unit.Shared.ApiCodesTests.Success\_ShouldBeZero \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Permissions.PermissionEndpointFilterTests.InvokeAsync\_ShouldCallNext\_WhenNoPermissionMetadata \[1 ms]

&#x20; Passed WeCms.Tests.Unit.Permissions.PermissionEndpointFilterTests.InvokeAsync\_ShouldShortCircuit\_WhenUserHasNoPermission \[< 1 ms]

&#x20; Passed WeCms.Tests.Unit.Auth.AuthServiceTests.RefreshAsync\_WhenInsertNewTokenFails\_ShouldRollbackAndThrowSystemError \[4 ms]

&#x20; Passed WeCms.Tests.Unit.Permissions.PermissionEndpointFilterTests.InvokeAsync\_ShouldShortCircuit\_WhenUserNotAuthenticated \[10 ms]

&#x20; Passed WeCms.Tests.Unit.Permissions.PermissionEndpointFilterTests.InvokeAsync\_ShouldCallNext\_WhenUserHasPermission \[< 1 ms]

Test Run Successful.

Total tests: 44

&#x20;    Passed: 44

&#x20;Total time: 1.6364 Seconds

&#x20;    7>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj" (VSTest target(s)).

\[xUnit.net 00:00:01.15]     WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.Refresh\_ShouldReturn400\_WhenTokenMissing \[FAIL]

\[xUnit.net 00:00:01.16]       MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

\[xUnit.net 00:00:01.16]       Stack Trace:

\[xUnit.net 00:00:01.16]         /\_/src/MySqlConnector/Core/ServerSession.cs(1081,0): at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.16]         /\_/src/MySqlConnector/Core/ServerSession.cs(931,0): at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.16]         /\_/src/MySqlConnector/Core/ServerSession.cs(900,0): at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.16]         /\_/src/MySqlConnector/Core/ServerSession.cs(539,0): at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.16]         /\_/src/MySqlConnector/Core/ServerSession.cs(697,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.16]         /\_/src/MySqlConnector/Core/ServerSession.cs(702,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]     WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.UnhandledException\_ShouldReturn500\_WithGenericMessage \[FAIL]

\[xUnit.net 00:00:01.16]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(413,0): at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.16]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(39,0): at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.16]         /\_/src/MySqlConnector/MySqlConnection.cs(1092,0): at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.16]         /\_/src/MySqlConnector/MySqlConnection.cs(567,0): at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.16]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs(21,0): at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]     WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.Me\_ShouldReturn401\_WhenUnauthorized \[FAIL]

\[xUnit.net 00:00:01.16]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs(20,0): at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.16]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs(100,0): at Program.\<Main>$(String\[] args)

\[xUnit.net 00:00:01.16]            at Program.\<Main>(String\[] args)

\[xUnit.net 00:00:01.16]            at InvokeStub\_Program.\<Main>(Object, Span\`1)

\[xUnit.net 00:00:01.16]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.16]         --- End of stack trace from previous location ---

\[xUnit.net 00:00:01.16]            at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.16]            at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

\[xUnit.net 00:00:01.16]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

\[xUnit.net 00:00:01.16]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

\[xUnit.net 00:00:01.16]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

\[xUnit.net 00:00:01.16]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.16]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.16]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

\[xUnit.net 00:00:01.16]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

\[xUnit.net 00:00:01.16]         /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs(16,0): at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory)

\[xUnit.net 00:00:01.16]            at System.Reflection.MethodBaseInvoker.InterpretedInvoke\_Constructor(Object obj, IntPtr\* args)

\[xUnit.net 00:00:01.16]            at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span\`1 copyOfArgs, BindingFlags invokeAttr)

\[xUnit.net 00:00:01.18]       MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

\[xUnit.net 00:00:01.23]     WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.AllResponses\_ShouldContain\_TraceIdHeader \[FAIL]

\[xUnit.net 00:00:01.18]       Stack Trace:

\[xUnit.net 00:00:01.18]         /\_/src/MySqlConnector/Core/ServerSession.cs(1081,0): at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /\_/src/MySqlConnector/Core/ServerSession.cs(931,0): at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /\_/src/MySqlConnector/Core/ServerSession.cs(900,0): at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /\_/src/MySqlConnector/Core/ServerSession.cs(539,0): at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /\_/src/MySqlConnector/Core/ServerSession.cs(697,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /\_/src/MySqlConnector/Core/ServerSession.cs(702,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(413,0): at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(39,0): at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /\_/src/MySqlConnector/MySqlConnection.cs(1092,0): at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /\_/src/MySqlConnector/MySqlConnection.cs(567,0): at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs(21,0): at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs(20,0): at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs(100,0): at Program.\<Main>$(String\[] args)

\[xUnit.net 00:00:01.18]            at Program.\<Main>(String\[] args)

\[xUnit.net 00:00:01.18]            at InvokeStub\_Program.\<Main>(Object, Span\`1)

\[xUnit.net 00:00:01.18]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.18]         --- End of stack trace from previous location ---

\[xUnit.net 00:00:01.18]            at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.18]            at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

\[xUnit.net 00:00:01.18]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

\[xUnit.net 00:00:01.29]     WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.ErrorResponse\_ShouldNotContain\_StackTrace \[FAIL]

\[xUnit.net 00:00:01.18]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

\[xUnit.net 00:00:01.18]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

\[xUnit.net 00:00:01.18]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.18]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.18]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

\[xUnit.net 00:00:01.18]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

\[xUnit.net 00:00:01.19]         /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs(16,0): at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory)

\[xUnit.net 00:00:01.19]            at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

\[xUnit.net 00:00:01.19]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.19]       MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

\[xUnit.net 00:00:01.19]       Stack Trace:

\[xUnit.net 00:00:01.19]         /\_/src/MySqlConnector/Core/ServerSession.cs(1081,0): at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /\_/src/MySqlConnector/Core/ServerSession.cs(931,0): at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /\_/src/MySqlConnector/Core/ServerSession.cs(900,0): at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /\_/src/MySqlConnector/Core/ServerSession.cs(539,0): at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /\_/src/MySqlConnector/Core/ServerSession.cs(697,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /\_/src/MySqlConnector/Core/ServerSession.cs(702,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(413,0): at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(39,0): at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /\_/src/MySqlConnector/MySqlConnection.cs(1092,0): at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /\_/src/MySqlConnector/MySqlConnection.cs(567,0): at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs(21,0): at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs(20,0): at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs(100,0): at Program.\<Main>$(String\[] args)

\[xUnit.net 00:00:01.19]            at Program.\<Main>(String\[] args)

\[xUnit.net 00:00:01.19]            at InvokeStub\_Program.\<Main>(Object, Span\`1)

\[xUnit.net 00:00:01.19]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.19]         --- End of stack trace from previous location ---

\[xUnit.net 00:00:01.34]     WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.DomainException\_ShouldReturnBusinessError\_WithBadRequest \[FAIL]

\[xUnit.net 00:00:01.19]            at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.19]            at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

\[xUnit.net 00:00:01.19]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

\[xUnit.net 00:00:01.19]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

\[xUnit.net 00:00:01.19]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

\[xUnit.net 00:00:01.19]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.19]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.19]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

\[xUnit.net 00:00:01.19]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

\[xUnit.net 00:00:01.19]         /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs(16,0): at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory)

\[xUnit.net 00:00:01.19]            at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

\[xUnit.net 00:00:01.19]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.23]       MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

\[xUnit.net 00:00:01.23]       Stack Trace:

\[xUnit.net 00:00:01.23]         /\_/src/MySqlConnector/Core/ServerSession.cs(1081,0): at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]         /\_/src/MySqlConnector/Core/ServerSession.cs(931,0): at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]         /\_/src/MySqlConnector/Core/ServerSession.cs(900,0): at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]         /\_/src/MySqlConnector/Core/ServerSession.cs(539,0): at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]         /\_/src/MySqlConnector/Core/ServerSession.cs(697,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]         /\_/src/MySqlConnector/Core/ServerSession.cs(702,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(413,0): at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(39,0): at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]         /\_/src/MySqlConnector/MySqlConnection.cs(1092,0): at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]         /\_/src/MySqlConnector/MySqlConnection.cs(567,0): at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs(21,0): at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]     WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.Login\_ShouldReturn400\_WhenCredentialsMissing \[FAIL]

\[xUnit.net 00:00:01.23]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs(20,0): at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs(100,0): at Program.\<Main>$(String\[] args)

\[xUnit.net 00:00:01.23]            at Program.\<Main>(String\[] args)

\[xUnit.net 00:00:01.23]            at InvokeStub\_Program.\<Main>(Object, Span\`1)

\[xUnit.net 00:00:01.23]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.23]         --- End of stack trace from previous location ---

\[xUnit.net 00:00:01.23]            at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.23]            at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

\[xUnit.net 00:00:01.23]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

\[xUnit.net 00:00:01.23]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

\[xUnit.net 00:00:01.23]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

\[xUnit.net 00:00:01.23]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.23]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.23]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

\[xUnit.net 00:00:01.23]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

\[xUnit.net 00:00:01.23]         /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs(16,0): at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory)

\[xUnit.net 00:00:01.23]            at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

\[xUnit.net 00:00:01.23]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.29]       MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

\[xUnit.net 00:00:01.29]       Stack Trace:

\[xUnit.net 00:00:01.29]         /\_/src/MySqlConnector/Core/ServerSession.cs(1081,0): at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]         /\_/src/MySqlConnector/Core/ServerSession.cs(931,0): at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]         /\_/src/MySqlConnector/Core/ServerSession.cs(900,0): at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]         /\_/src/MySqlConnector/Core/ServerSession.cs(539,0): at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]         /\_/src/MySqlConnector/Core/ServerSession.cs(697,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]     WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.Ping\_ShouldReturnSuccess\_WhenNoError \[FAIL]

\[xUnit.net 00:00:01.29]         /\_/src/MySqlConnector/Core/ServerSession.cs(702,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(413,0): at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(39,0): at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]         /\_/src/MySqlConnector/MySqlConnection.cs(1092,0): at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]         /\_/src/MySqlConnector/MySqlConnection.cs(567,0): at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs(21,0): at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs(20,0): at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs(100,0): at Program.\<Main>$(String\[] args)

\[xUnit.net 00:00:01.29]            at Program.\<Main>(String\[] args)

\[xUnit.net 00:00:01.29]            at InvokeStub\_Program.\<Main>(Object, Span\`1)

\[xUnit.net 00:00:01.29]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.29]         --- End of stack trace from previous location ---

\[xUnit.net 00:00:01.29]            at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.29]            at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

\[xUnit.net 00:00:01.29]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

\[xUnit.net 00:00:01.29]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

\[xUnit.net 00:00:01.29]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

\[xUnit.net 00:00:01.29]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.29]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.29]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

\[xUnit.net 00:00:01.29]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

\[xUnit.net 00:00:01.29]         /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs(16,0): at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory)

\[xUnit.net 00:00:01.29]            at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

\[xUnit.net 00:00:01.29]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

&#x20; Passed WeCms.Tests.Architecture.PersistenceBoundaryTests.OnlyPersistence\_ShouldReferenceDapperAndMySqlPackages \[63 ms]

&#x20; Passed WeCms.Tests.Architecture.PersistenceBoundaryTests.Modules\_ShouldNotReferenceDapperOrMySqlPackages \[1 ms]

&#x20; Passed WeCms.Tests.Architecture.PersistenceBoundaryTests.Modules\_ShouldNotContainSqlKeywords \[52 ms]

&#x20; Passed WeCms.Tests.Architecture.PersistenceBoundaryTests.Modules\_ShouldNotUseDbConnectionOrDbTransactionDirectly \[34 ms]

&#x20; Passed WeCms.Tests.Architecture.PersistenceBoundaryTests.Modules\_ShouldNotCallDapperAsyncApisOrCommandDefinition \[3 ms]

&#x20; Passed WeCms.Tests.Architecture.PersistenceBoundaryTests.Modules\_ShouldNotDirectlyReferencePersistenceTypes \[9 ms]

&#x20; Passed WeCms.Tests.Architecture.PersistenceBoundaryTests.Modules\_ShouldNotReferencePersistenceImplementation \[4 ms]

&#x20; Passed WeCms.Tests.Architecture.PermissionMetadataScanTests.AllAuthenticatedEndpoints\_ShouldHave\_PermissionMetadata\_OrBeExempt \[254 ms]

&#x20; Passed WeCms.Tests.Architecture.PermissionMetadataScanTests.SecurePing\_ShouldHave\_PermissionMetadata \[80 ms]

\[xUnit.net 00:00:00.95]   Finished:    WeCms.Tests.Architecture

\[xUnit.net 00:00:01.34]       MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

\[xUnit.net 00:00:01.34]       Stack Trace:

\[xUnit.net 00:00:01.34]         /\_/src/MySqlConnector/Core/ServerSession.cs(1081,0): at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /\_/src/MySqlConnector/Core/ServerSession.cs(931,0): at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /\_/src/MySqlConnector/Core/ServerSession.cs(900,0): at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /\_/src/MySqlConnector/Core/ServerSession.cs(539,0): at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /\_/src/MySqlConnector/Core/ServerSession.cs(697,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /\_/src/MySqlConnector/Core/ServerSession.cs(702,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(413,0): at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(39,0): at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /\_/src/MySqlConnector/MySqlConnection.cs(1092,0): at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /\_/src/MySqlConnector/MySqlConnection.cs(567,0): at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs(21,0): at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs(20,0): at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs(100,0): at Program.\<Main>$(String\[] args)

\[xUnit.net 00:00:01.34]            at Program.\<Main>(String\[] args)

\[xUnit.net 00:00:01.34]            at InvokeStub\_Program.\<Main>(Object, Span\`1)

\[xUnit.net 00:00:01.34]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.34]         --- End of stack trace from previous location ---

\[xUnit.net 00:00:01.34]            at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.34]            at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

\[xUnit.net 00:00:01.34]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

\[xUnit.net 00:00:01.34]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

\[xUnit.net 00:00:01.34]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

\[xUnit.net 00:00:01.34]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.34]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.34]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

\[xUnit.net 00:00:01.34]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

\[xUnit.net 00:00:01.34]         /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs(16,0): at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory)

\[xUnit.net 00:00:01.34]            at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

\[xUnit.net 00:00:01.34]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.39]       MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

\[xUnit.net 00:00:01.39]       Stack Trace:

\[xUnit.net 00:00:01.39]         /\_/src/MySqlConnector/Core/ServerSession.cs(1081,0): at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /\_/src/MySqlConnector/Core/ServerSession.cs(931,0): at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /\_/src/MySqlConnector/Core/ServerSession.cs(900,0): at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /\_/src/MySqlConnector/Core/ServerSession.cs(539,0): at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /\_/src/MySqlConnector/Core/ServerSession.cs(697,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /\_/src/MySqlConnector/Core/ServerSession.cs(702,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(413,0): at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(39,0): at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /\_/src/MySqlConnector/MySqlConnection.cs(1092,0): at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /\_/src/MySqlConnector/MySqlConnection.cs(567,0): at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs(21,0): at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs(20,0): at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs(100,0): at Program.\<Main>$(String\[] args)

\[xUnit.net 00:00:01.39]            at Program.\<Main>(String\[] args)

\[xUnit.net 00:00:01.39]            at InvokeStub\_Program.\<Main>(Object, Span\`1)

\[xUnit.net 00:00:01.39]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.39]         --- End of stack trace from previous location ---

\[xUnit.net 00:00:01.39]            at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.39]            at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

\[xUnit.net 00:00:01.39]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

\[xUnit.net 00:00:01.39]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

\[xUnit.net 00:00:01.39]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

\[xUnit.net 00:00:01.39]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.39]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.39]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

\[xUnit.net 00:00:01.39]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

\[xUnit.net 00:00:01.39]         /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs(16,0): at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory)

\[xUnit.net 00:00:01.39]            at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

\[xUnit.net 00:00:01.39]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

&#x20; Passed WeCms.Tests.Architecture.PermissionMetadataScanTests.SecurePing\_ShouldRequire\_Authorization \[18 ms]

Test Run Successful.

Total tests: 10

&#x20;    Passed: 10

&#x20;Total time: 2.2933 Seconds

\[xUnit.net 00:00:01.44]       MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

\[xUnit.net 00:00:01.44]       Stack Trace:

\[xUnit.net 00:00:01.44]         /\_/src/MySqlConnector/Core/ServerSession.cs(1081,0): at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /\_/src/MySqlConnector/Core/ServerSession.cs(931,0): at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /\_/src/MySqlConnector/Core/ServerSession.cs(900,0): at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /\_/src/MySqlConnector/Core/ServerSession.cs(539,0): at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /\_/src/MySqlConnector/Core/ServerSession.cs(697,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /\_/src/MySqlConnector/Core/ServerSession.cs(702,0): at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(413,0): at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /\_/src/MySqlConnector/Core/ConnectionPool.cs(39,0): at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /\_/src/MySqlConnector/MySqlConnection.cs(1092,0): at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /\_/src/MySqlConnector/MySqlConnection.cs(567,0): at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs(21,0): at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs(20,0): at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]         /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs(100,0): at Program.\<Main>$(String\[] args)

\[xUnit.net 00:00:01.44]            at Program.\<Main>(String\[] args)

\[xUnit.net 00:00:01.44]            at InvokeStub\_Program.\<Main>(Object, Span\`1)

\[xUnit.net 00:00:01.44]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.44]         --- End of stack trace from previous location ---

\[xUnit.net 00:00:01.44]            at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

\[xUnit.net 00:00:01.44]            at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

\[xUnit.net 00:00:01.44]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

\[xUnit.net 00:00:01.44]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

\[xUnit.net 00:00:01.44]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

\[xUnit.net 00:00:01.44]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.44]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

\[xUnit.net 00:00:01.44]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

\[xUnit.net 00:00:01.44]            at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

\[xUnit.net 00:00:01.44]         /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs(16,0): at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory)

\[xUnit.net 00:00:01.44]            at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

\[xUnit.net 00:00:01.44]            at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

&#x20;    5>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj" (VSTest target(s)).

&#x20; Passed WeCms.Tests.Integration.Auth.AuthRefreshConcurrencyTests.Refresh\_WithSameTokenConcurrently\_ShouldAllowOnlyOneSuccess \[683 ms]

&#x20; Failed WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.Refresh\_ShouldReturn400\_WhenTokenMissing \[1 ms]

&#x20; Error Message:

&#x20;  MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

&#x20; Stack Trace:

&#x20;    at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 1081

&#x20;  at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 931

&#x20;  at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 900

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 539

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 697

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 702

&#x20;  at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 413

&#x20;  at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 39

&#x20;  at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 1092

&#x20;  at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 567

&#x20;  at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs:line 21

&#x20;  at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs:line 20

&#x20;  at Program.\<Main>$(String\[] args) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs:line 100

&#x20;  at Program.\<Main>(String\[] args)

&#x20;  at InvokeStub\_Program.\<Main>(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\--- End of stack trace from previous location ---

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

&#x20;  at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

&#x20;  at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory) in /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs:line 16

&#x20;  at System.Reflection.MethodBaseInvoker.InterpretedInvoke\_Constructor(Object obj, IntPtr\* args)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span\`1 copyOfArgs, BindingFlags invokeAttr)

&#x20; Failed WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.UnhandledException\_ShouldReturn500\_WithGenericMessage \[1 ms]

&#x20; Error Message:

&#x20;  MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

&#x20; Stack Trace:

&#x20;    at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 1081

&#x20;  at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 931

&#x20;  at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 900

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 539

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 697

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 702

&#x20;  at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 413

&#x20;  at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 39

&#x20;  at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 1092

&#x20;  at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 567

&#x20;  at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs:line 21

&#x20;  at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs:line 20

&#x20;  at Program.\<Main>$(String\[] args) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs:line 100

&#x20;  at Program.\<Main>(String\[] args)

&#x20;  at InvokeStub\_Program.\<Main>(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\--- End of stack trace from previous location ---

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

&#x20;  at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

&#x20;  at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory) in /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs:line 16

&#x20;  at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

&#x20; Failed WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.Me\_ShouldReturn401\_WhenUnauthorized \[1 ms]

&#x20; Error Message:

&#x20;  MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

&#x20; Stack Trace:

&#x20;    at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 1081

&#x20;  at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 931

&#x20;  at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 900

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 539

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 697

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 702

&#x20;  at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 413

&#x20;  at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 39

&#x20;  at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 1092

&#x20;  at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 567

&#x20;  at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs:line 21

&#x20;  at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs:line 20

&#x20;  at Program.\<Main>$(String\[] args) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs:line 100

&#x20;  at Program.\<Main>(String\[] args)

&#x20;  at InvokeStub\_Program.\<Main>(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\--- End of stack trace from previous location ---

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

&#x20;  at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

&#x20;  at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory) in /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs:line 16

&#x20;  at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

&#x20; Failed WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.AllResponses\_ShouldContain\_TraceIdHeader \[1 ms]

&#x20; Error Message:

&#x20;  MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

&#x20; Stack Trace:

&#x20;    at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 1081

&#x20;  at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 931

&#x20;  at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 900

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 539

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 697

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 702

&#x20;  at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 413

&#x20;  at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 39

&#x20;  at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 1092

&#x20;  at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 567

&#x20;  at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs:line 21

&#x20;  at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs:line 20

&#x20;  at Program.\<Main>$(String\[] args) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs:line 100

&#x20;  at Program.\<Main>(String\[] args)

&#x20;  at InvokeStub\_Program.\<Main>(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\--- End of stack trace from previous location ---

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

&#x20;  at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

&#x20;  at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory) in /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs:line 16

&#x20;  at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

&#x20; Failed WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.ErrorResponse\_ShouldNotContain\_StackTrace \[1 ms]

&#x20; Error Message:

&#x20;  MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

&#x20; Stack Trace:

&#x20;    at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 1081

&#x20;  at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 931

&#x20;  at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 900

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 539

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 697

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 702

&#x20;  at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 413

&#x20;  at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 39

&#x20;  at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 1092

&#x20;  at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 567

&#x20;  at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs:line 21

&#x20;  at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs:line 20

&#x20;  at Program.\<Main>$(String\[] args) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs:line 100

&#x20;  at Program.\<Main>(String\[] args)

&#x20;  at InvokeStub\_Program.\<Main>(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\--- End of stack trace from previous location ---

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

&#x20;  at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

&#x20;  at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory) in /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs:line 16

&#x20;  at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

&#x20; Failed WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.DomainException\_ShouldReturnBusinessError\_WithBadRequest \[1 ms]

&#x20; Error Message:

&#x20;  MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

&#x20; Stack Trace:

&#x20;    at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 1081

&#x20;  at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 931

&#x20;  at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 900

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 539

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 697

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 702

&#x20;  at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 413

&#x20;  at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 39

&#x20;  at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 1092

&#x20;  at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 567

&#x20;  at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs:line 21

&#x20;  at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs:line 20

&#x20;  at Program.\<Main>$(String\[] args) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs:line 100

&#x20;  at Program.\<Main>(String\[] args)

&#x20;  at InvokeStub\_Program.\<Main>(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\--- End of stack trace from previous location ---

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

&#x20;  at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

&#x20;  at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory) in /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs:line 16

&#x20;  at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

&#x20; Failed WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.Login\_ShouldReturn400\_WhenCredentialsMissing \[1 ms]

&#x20; Error Message:

&#x20;  MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

&#x20; Stack Trace:

&#x20;    at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 1081

&#x20;  at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 931

&#x20;  at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 900

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 539

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 697

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 702

&#x20;  at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 413

&#x20;  at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 39

&#x20;  at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 1092

&#x20;  at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 567

&#x20;  at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs:line 21

&#x20;  at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs:line 20

&#x20;  at Program.\<Main>$(String\[] args) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs:line 100

&#x20;  at Program.\<Main>(String\[] args)

&#x20;  at InvokeStub\_Program.\<Main>(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\--- End of stack trace from previous location ---

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

&#x20;  at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

&#x20;  at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory) in /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs:line 16

&#x20;  at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\[xUnit.net 00:00:01.52]   Finished:    WeCms.Tests.Integration

&#x20; Failed WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests.Ping\_ShouldReturnSuccess\_WhenNoError \[1 ms]

&#x20; Error Message:

&#x20;  MySqlConnector.MySqlException : Access denied for user 'wecms'@'172.18.0.1' (using password: YES)

&#x20; Stack Trace:

&#x20;    at MySqlConnector.Core.ServerSession.ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 1081

Test Run Failed.

&#x20;  at MySqlConnector.Core.ServerSession.SendClearPasswordAsync(String password, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 931

&#x20;  at MySqlConnector.Core.ServerSession.SwitchAuthenticationAsync(ConnectionSettings cs, String password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 900

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAsync(ConnectionSettings cs, MySqlConnection connection, Int64 startingTimestamp, ILoadBalancer loadBalancer, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 539

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 697

&#x20;  at MySqlConnector.Core.ServerSession.ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer loadBalancer, MySqlConnection connection, Action\`4 logMessage, Int64 startingTimestamp, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ServerSession.cs:line 702

&#x20;  at MySqlConnector.Core.ConnectionPool.CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 413

&#x20;  at MySqlConnector.Core.ConnectionPool.GetSessionAsync(MySqlConnection connection, Int64 startingTimestamp, Int32 timeoutMilliseconds, Activity activity, IOBehavior ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/Core/ConnectionPool.cs:line 39

&#x20;  at MySqlConnector.MySqlConnection.CreateSessionAsync(ConnectionPool pool, Int64 startingTimestamp, Activity activity, Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 1092

&#x20;  at MySqlConnector.MySqlConnection.OpenAsync(Nullable\`1 ioBehavior, CancellationToken cancellationToken) in /\_/src/MySqlConnector/MySqlConnection.cs:line 567

&#x20;  at WeCms.Persistence.Data.DbConnectionFactory.OpenAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Data/DbConnectionFactory.cs:line 21

&#x20;  at WeCms.Persistence.Migration.DbMigrationRunner.RunAsync(CancellationToken cancellationToken) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Persistence/Migration/DbMigrationRunner.cs:line 20

&#x20;  at Program.\<Main>$(String\[] args) in /home/runner/work/WeCMS-next/WeCMS-next/backend/src/WeCms.Api/Program.cs:line 100

&#x20;  at Program.\<Main>(String\[] args)

&#x20;  at InvokeStub\_Program.\<Main>(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

\--- End of stack trace from previous location ---

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.DeferredHostBuilder.DeferredHost.StartAsync(CancellationToken cancellationToken)

&#x20;  at Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.Start(IHost host)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateHost(IHostBuilder builder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.ConfigureHostBuilder(IHostBuilder hostBuilder)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.StartServer()

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateDefaultClient(Uri baseAddress, DelegatingHandler\[] handlers)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient(WebApplicationFactoryClientOptions options)

&#x20;  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory\`1.CreateClient()

&#x20;  at WeCms.Tests.Integration.Middleware.ExceptionMiddlewareTests..ctor(WebApplicationFactory\`1 factory) in /home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/Middleware/ExceptionMiddlewareTests.cs:line 16

&#x20;  at InvokeStub\_ExceptionMiddlewareTests..ctor(Object, Span\`1)

&#x20;  at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object\[] parameters, CultureInfo culture)

Total tests: 9

&#x20;    Passed: 1

&#x20;    Failed: 8

&#x20;Total time: 2.4369 Seconds

&#x20;    6>\_VSTestConsole:

&#x20;        MSB4181: The "VSTestTask" task returned false but did not log an error.

&#x20;    6>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj" (VSTest target(s)) -- FAILED.

&#x20;    1>Done Building Project "/home/runner/work/WeCMS-next/WeCMS-next/backend/WeCms.slnx" (VSTest target(s)) -- FAILED.

Build FAILED.

&#x20;   0 Warning(s)

&#x20;   0 Error(s)

Time Elapsed 00:00:07.24

\=== WeCMS M0-BE Backend Quality Gate FAILED ===

**Error:** Process completed with exit code 1.
