using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Atomics.ValueBools;

namespace Soenneker.Blazor.Utils.ModuleImport.Dtos;

/// <summary>
/// Represents the module import item.
/// </summary>
public sealed class ModuleImportItem : IAsyncDisposable
{
    /// <summary>
    /// The module loaded tcs.
    /// </summary>
    public readonly TaskCompletionSource<bool> ModuleLoadedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets or sets script reference.
    /// </summary>
    public IJSObjectReference? ScriptReference { get; set; }

    /// <summary>
    /// Gets or sets loaded.
    /// </summary>
    public Task Loaded => ModuleLoadedTcs.Task;

    private ValueAtomicBool _disposed;

    /// <summary>
    /// Asynchronously releases resources used by the current instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return ValueTask.CompletedTask;

        if (ScriptReference != null)
            return ScriptReference.DisposeAsync();

        return ValueTask.CompletedTask;
    }
}