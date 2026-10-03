using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Soenneker.Blazor.Utils.ModuleImport.Tests;

internal sealed class DelayedJsRuntime : IJSRuntime
{
    private int _importCount;
    internal int ImportCount => Volatile.Read(ref _importCount);
    internal TaskCompletionSource<IJSObjectReference> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, default, args);

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (identifier != "import") throw new InvalidOperationException("Unexpected JavaScript call.");
        Interlocked.Increment(ref _importCount);
        return (TValue)await Completion.Task.WaitAsync(cancellationToken);
    }
}
