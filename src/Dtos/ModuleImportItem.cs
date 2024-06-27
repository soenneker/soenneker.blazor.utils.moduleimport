using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Soenneker.Blazor.Utils.ModuleImport.Dtos;

public class ModuleImportItem : IAsyncDisposable
{
    public readonly TaskCompletionSource<bool> ModuleLoadedTcs = new();

    public IJSObjectReference? ScriptReference { get; set; }

    public Task IsLoaded => ModuleLoadedTcs.Task;

    public ValueTask DisposeAsync()
    {
        if (ScriptReference != null)
            return ScriptReference.DisposeAsync();

        return ValueTask.CompletedTask;
    }
}