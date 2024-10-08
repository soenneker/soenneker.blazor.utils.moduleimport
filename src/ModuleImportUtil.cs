using System;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.SingletonDictionary;
using Soenneker.Blazor.Utils.JsVariable.Abstract;

namespace Soenneker.Blazor.Utils.ModuleImport;

/// <inheritdoc cref="IModuleImportUtil"/>
public class ModuleImportUtil : IModuleImportUtil
{
    private readonly IJsVariableInterop _jsVariableInterop;
    private readonly SingletonDictionary<ModuleImportItem> _modules;

    public ModuleImportUtil(IJSRuntime jsRuntime, IJsVariableInterop jsVariableInterop)
    {
        _jsVariableInterop = jsVariableInterop;
        _modules = new SingletonDictionary<ModuleImportItem>(async (key, token, _) =>
        {
            var item = new ModuleImportItem();

            try
            {
                item.ScriptReference = await jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import", token, $"./_content/{key}").NoSync();

                item.ModuleLoadedTcs.SetResult(true);
            }
            catch (Exception ex)
            {
                // Handle exceptions and set the task completion source as failed
                item.ModuleLoadedTcs.SetException(ex);
            }

            return item;
        });
    }

    public ValueTask<ModuleImportItem> GetModule(string name, CancellationToken cancellationToken = default)
    {
        return _modules.Get(name, cancellationToken);
    }

    public async ValueTask<IJSObjectReference> Import(string name, CancellationToken cancellationToken = default)
    {
        ModuleImportItem item = await GetModule(name, cancellationToken).NoSync();
        return item.ScriptReference!;
    }

    public async ValueTask ImportAndWait(string name, CancellationToken cancellationToken = default)
    {
        ModuleImportItem item = await GetModule(name, cancellationToken).NoSync();
        await item.IsLoaded;
    }

    public async ValueTask ImportAndWaitUntilAvailable(string name, string variableName, int delay = 100, CancellationToken cancellationToken = default)
    {
        await ImportAndWait(name, cancellationToken).NoSync();
        await _jsVariableInterop.WaitForVariable(variableName, delay, cancellationToken).NoSync();
    }

    public async ValueTask DisposeModule(string name, CancellationToken cancellationToken = default)
    {
        ModuleImportItem item = await GetModule(name, cancellationToken).NoSync();
        await item.DisposeAsync().NoSync();
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        return _modules.DisposeAsync();
    }
}