using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Soenneker.Blazor.Utils.ModuleImport.Dtos;

internal class ModuleImportItem : IAsyncDisposable
{
    public TaskCompletionSource<bool> ModuleLoadedTcs = new();

    public IJSObjectReference ScriptReference { get; set; }

    public Task IsLoaded => ModuleLoadedTcs.Task;

    public ValueTask DisposeAsync()
    {
        return ScriptReference.DisposeAsync();
    }
}