using Xunit;

namespace WeCms.Tests.Integration;

[CollectionDefinition(nameof(SharedMySqlCollection), DisableParallelization = true)]
public sealed class SharedMySqlCollection;
