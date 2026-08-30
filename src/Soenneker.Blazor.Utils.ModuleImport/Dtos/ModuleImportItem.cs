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
    /// Coordinates completion of the module import.
    /// </summary>
    internal readonly TaskCompletionSource<bool> ModuleLoadedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the imported module reference after <see cref="Loaded"/> completes successfully.
    /// </summary>
    public IJSObjectReference? ScriptReference { get; internal set; }

    /// <summary>
    /// Gets the task that completes when the import succeeds or fails.
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
