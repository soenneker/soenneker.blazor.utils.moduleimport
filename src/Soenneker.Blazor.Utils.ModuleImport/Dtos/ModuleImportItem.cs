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
    private readonly TaskCompletionSource<bool>? _moduleLoadedTcs;

    /// <summary>
    /// Creates an empty module item. Use the module import utility to obtain loaded items.
    /// </summary>
    public ModuleImportItem()
    {
        _moduleLoadedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    internal ModuleImportItem(IJSObjectReference reference)
    {
        ScriptReference = reference ?? throw new ArgumentNullException(nameof(reference));
    }

    /// <summary>
    /// Gets the imported module reference after <see cref="Loaded"/> completes successfully.
    /// </summary>
    public IJSObjectReference? ScriptReference { get; internal set; }

    /// <summary>
    /// Gets the task that completes when the import succeeds or fails.
    /// </summary>
    public Task Loaded => _moduleLoadedTcs?.Task ?? Task.CompletedTask;

    private ValueAtomicBool _disposed;

    /// <summary>
    /// Asynchronously releases resources used by the current instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return;

        try
        {
            if (ScriptReference != null)
                await ScriptReference.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
