using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Atomics.ValueBools;

namespace Soenneker.Blazor.Utils.ModuleImport.Dtos;

public sealed class ModuleImportItem : IAsyncDisposable
{
    public readonly TaskCompletionSource<bool> ModuleLoadedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IJSObjectReference? ScriptReference { get; set; }

    public Task IsLoaded => ModuleLoadedTcs.Task;

    private ValueAtomicBool _disposed;

    public ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return ValueTask.CompletedTask;

        if (ScriptReference != null)
            return ScriptReference.DisposeAsync();

        return ValueTask.CompletedTask;
    }
}