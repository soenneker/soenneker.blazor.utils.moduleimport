using System;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;
using Soenneker.Utils.SingletonDictionary;

namespace Soenneker.Blazor.Utils.ModuleImport;

/// <inheritdoc cref="IModuleImportUtil"/>
public class ModuleImportUtil : IModuleImportUtil
{
    private readonly SingletonDictionary<ModuleImportItem> _modules;

    public ModuleImportUtil(IJSRuntime jsRuntime)
    {
        _modules = new SingletonDictionary<ModuleImportItem>(async objectArray =>
        {
            try
            {
                var name = objectArray[0] as string;
                var cancellationToken = (CancellationToken) objectArray[1];

                var item = new ModuleImportItem
                {
                    ScriptReference = await jsRuntime.InvokeAsync<IJSObjectReference>(
                        "import", cancellationToken, $"./_content/{name}")
                };

                item.ModuleLoadedTcs.SetResult(true);

                return item;
            }
            catch (Exception ex)
            {
                // Handle exceptions and set the task completion source as failed
                var item = new ModuleImportItem();
                item.ModuleLoadedTcs.SetException(ex);
                return item;
            }
        });
    }

    public async ValueTask<IJSObjectReference> Import(string name, CancellationToken cancellationToken = default)
    {
        ModuleImportItem item = await _modules.Get(name, [name, cancellationToken]);
        return item.ScriptReference;
    }

    public async ValueTask WaitUntilLoaded(string name, CancellationToken cancellationToken = default)
    {
        ModuleImportItem item = await _modules.Get(name, [name, cancellationToken]);
        await item.IsLoaded;
    }

    public async ValueTask DisposeModule(string name, CancellationToken cancellationToken = default)
    {
        ModuleImportItem item = await _modules.Get(name, [name, cancellationToken]);
        await item.DisposeAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _modules.DisposeAsync();
    }
}