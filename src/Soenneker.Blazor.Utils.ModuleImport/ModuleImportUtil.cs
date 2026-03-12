using System;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;
using Soenneker.Dictionaries.Singletons;
using Soenneker.Blazor.Utils.JsVariable.Abstract;
using Soenneker.Extensions.CancellationTokens;
using Soenneker.Utils.CancellationScopes;

namespace Soenneker.Blazor.Utils.ModuleImport;

/// <inheritdoc cref="IModuleImportUtil"/>
public sealed class ModuleImportUtil : IModuleImportUtil
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IJsVariableInterop _jsVariableInterop;
    private readonly SingletonDictionary<ModuleImportItem> _modules;

    private readonly CancellationScope _cancellationScope = new();

    public ModuleImportUtil(IJSRuntime jsRuntime, IJsVariableInterop jsVariableInterop)
    {
        _jsRuntime = jsRuntime;
        _jsVariableInterop = jsVariableInterop;

        _modules = new SingletonDictionary<ModuleImportItem>(InitializeModule);
    }

    private async ValueTask<ModuleImportItem> InitializeModule(string key, CancellationToken cancellationToken)
    {
        var item = new ModuleImportItem();

        try
        {
            item.ScriptReference = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, $"./_content/{key}");

            item.ModuleLoadedTcs.SetResult(true);
        }
        catch (Exception ex)
        {
            item.ModuleLoadedTcs.SetException(ex);
        }

        return item;
    }

    public async ValueTask<ModuleImportItem> GetModule(string name, CancellationToken cancellationToken = default)
    {
        var linked = _cancellationScope.CancellationToken.Link(cancellationToken, out var source);

        using (source)
            return await _modules.Get(name, linked);
    }

    public async ValueTask<IJSObjectReference> Import(string name, CancellationToken cancellationToken = default)
    {
        var linked = _cancellationScope.CancellationToken.Link(cancellationToken, out var source);

        using (source)
        {
            ModuleImportItem item = await GetModule(name, linked);
            return item.ScriptReference!;
        }
    }

    public async ValueTask ImportAndWait(string name, CancellationToken cancellationToken = default)
    {
        var linked = _cancellationScope.CancellationToken.Link(cancellationToken, out var source);

        using (source)
        {
            ModuleImportItem item = await GetModule(name, linked);
            await item.IsLoaded;
        }
    }

    public async ValueTask ImportAndWaitUntilAvailable(string name, string variableName, int delay = 100, CancellationToken cancellationToken = default)
    {
        var linked = _cancellationScope.CancellationToken.Link(cancellationToken, out var source);

        using (source)
        {
            await ImportAndWait(name, linked);
            await _jsVariableInterop.WaitForVariable(variableName, delay, linked);
        }
    }

    public async ValueTask DisposeModule(string name, CancellationToken cancellationToken = default)
    {
        var linked = _cancellationScope.CancellationToken.Link(cancellationToken, out var source);

        using (source)
        {
            ModuleImportItem item = await GetModule(name, linked);
            await item.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _modules.DisposeAsync();
        await _cancellationScope.DisposeAsync();
    }

    public void Dispose()
    {
        _modules.Dispose();
        _cancellationScope.DisposeAsync().GetAwaiter().GetResult();
    }
}