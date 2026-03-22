using System;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;
using Soenneker.Dictionaries.Singletons;
using Soenneker.Extensions.CancellationTokens;
using Soenneker.Utils.CancellationScopes;

namespace Soenneker.Blazor.Utils.ModuleImport;

/// <inheritdoc cref="IModuleImportUtil"/>
public sealed class ModuleImportUtil : IModuleImportUtil
{
    private readonly IJSRuntime _jsRuntime;
    private readonly SingletonDictionary<ModuleImportItem> _modules;

    private readonly CancellationScope _cancellationScope = new();

    public ModuleImportUtil(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;

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
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
            return await GetModuleInternal(name, linked);
    }

    public async ValueTask<IJSObjectReference> Import(string name, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
            return await ImportInternal(name, linked);
    }

    public async ValueTask ImportAndWait(string name, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
            _ = await ImportInternal(name, linked);
    }

    public async ValueTask DisposeModule(string name, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            ModuleImportItem item = await GetModuleInternal(name, linked);
            await item.DisposeAsync();
        }
    }

    private ValueTask<ModuleImportItem> GetModuleInternal(string name, CancellationToken cancellationToken)
    {
        return _modules.Get(name, cancellationToken);
    }

    private async ValueTask<IJSObjectReference> ImportInternal(string name, CancellationToken cancellationToken)
    {
        ModuleImportItem item = await GetModuleInternal(name, cancellationToken);
        await item.IsLoaded;
        return item.ScriptReference!;
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