using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Soenneker.Blazor.Utils.ModuleImport.Tests;

public sealed class ModuleConcurrencyTests
{
    [Test]
    public async Task Concurrent_waiters_share_an_import_and_one_can_cancel_independently(CancellationToken cancellationToken)
    {
        var runtime = new DelayedJsRuntime();
        await using var modules = new ModuleImportUtil(runtime);
        Task<IJSObjectReference> first = modules.GetContentModuleReference("./_content/test/module.js", cancellationToken: cancellationToken).AsTask();
        using var cancellation = new CancellationTokenSource();
        Task<IJSObjectReference> cancelled = modules.GetContentModuleReference("_content/test/module.js", cancellation.Token).AsTask();
        var waiters = new Task<IJSObjectReference>[64];
        for (var i = 0; i < waiters.Length; i++)
            waiters[i] = Task.Run(async () => await modules.GetContentModuleReference("/_content/test/module.js", cancellationToken: cancellationToken));
        await cancellation.CancelAsync();
        try { await cancelled; throw new InvalidOperationException("Expected cancellation."); }
        catch (OperationCanceledException) { }
        var expected = new TestJsObjectReference();
        runtime.Completion.SetResult(expected);
        if (!ReferenceEquals(await first, expected)) throw new InvalidOperationException("Unexpected imported module.");
        foreach (IJSObjectReference actual in await Task.WhenAll(waiters))
            if (!ReferenceEquals(expected, actual)) throw new InvalidOperationException("Waiters received different modules.");
        if (runtime.ImportCount != 1) throw new InvalidOperationException("Concurrent callers repeated the import.");
    }
}
