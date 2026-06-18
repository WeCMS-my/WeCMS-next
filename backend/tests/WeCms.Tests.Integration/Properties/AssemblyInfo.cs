using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]
[assembly: WeCms.Tests.Integration.ResetIntegrationDatabaseBeforeTest]
