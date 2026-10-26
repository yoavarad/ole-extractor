using Xunit;

namespace ExtractorOLE.Tests
{
    /// <summary>
    /// Console.SetOut/SetError swap process-global writers. Test classes that
    /// redirect them must not run in parallel, or one test's restore clobbers
    /// another's capture writer (flaky empty-capture assertions).
    /// </summary>
    [CollectionDefinition("ConsoleRedirect", DisableParallelization = true)]
    public sealed class ConsoleRedirectCollectionDefinition { }
}

namespace ExtractorOLE.Tests
{
    /// <summary>
    /// Tests that need ~500 simultaneously-running pool threads (Barrier-gated)
    /// must not compete with other collections for ThreadPool threads/CPU.
    /// </summary>
    [CollectionDefinition("ThreadPoolSensitive", DisableParallelization = true)]
    public sealed class ThreadPoolSensitiveCollectionDefinition { }
}
