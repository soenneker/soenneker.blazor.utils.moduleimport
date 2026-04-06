using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;
using Soenneker.Dictionaries.Singletons;
using Soenneker.Extensions.CancellationTokens;
using Soenneker.Utils.CancellationScopes;

namespace Soenneker.Blazor.Utils.ModuleImport;

/// <inheritdoc cref="IModuleImportUtil"/>
public sealed class ModuleImportUtil : IModuleImportUtil
{
    private readonly IJSRuntime _jsRuntime;
    private readonly SingletonDictionary<ModuleImportItem> _contentModules;
    private readonly SingletonDictionary<ModuleImportItem> _externalModules;

    private readonly CancellationScope _cancellationScope = new();

    public ModuleImportUtil(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;

        _contentModules = new SingletonDictionary<ModuleImportItem>(InitializeContentModule);
        _externalModules = new SingletonDictionary<ModuleImportItem>(InitializeExternalModule);
    }

    private async ValueTask<ModuleImportItem> InitializeContentModule(string name, CancellationToken cancellationToken)
    {
        var item = new ModuleImportItem();

        try
        {
            item.ScriptReference = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, $"./_content/{name}");
            item.ModuleLoadedTcs.SetResult(true);
        }
        catch (Exception ex)
        {
            item.ModuleLoadedTcs.SetException(ex);
        }

        return item;
    }

    private async ValueTask<ModuleImportItem> InitializeExternalModule(string url, CancellationToken cancellationToken)
    {
        var item = new ModuleImportItem();

        try
        {
            item.ScriptReference = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, url);
            item.ModuleLoadedTcs.SetResult(true);
        }
        catch (Exception ex)
        {
            item.ModuleLoadedTcs.SetException(ex);
        }

        return item;
    }

    public async ValueTask<IJSObjectReference> GetContentModuleReference(string name, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            ModuleImportItem item = await _contentModules.Get(name, linked);
            await item.Loaded;
            return item.ScriptReference!;
        }
    }

    public async ValueTask<IJSObjectReference> GetExternalModuleReference(string url, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            ModuleImportItem item = await _externalModules.Get(url, linked);
            await item.Loaded;
            return item.ScriptReference!;
        }
    }

    public async ValueTask<ModuleImportItem> GetContentModule(string name, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
            return await _contentModules.Get(name, linked);
    }

    public async ValueTask<ModuleImportItem> GetExternalModule(string url, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
            return await _externalModules.Get(url, linked);
    }

    public async ValueTask DisposeContentModule(string name, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            ModuleImportItem item = await _contentModules.Get(name, linked);
            await item.DisposeAsync();
        }
    }

    public async ValueTask DisposeExternalModule(string url, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            ModuleImportItem item = await _externalModules.Get(url, linked);
            await item.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _contentModules.DisposeAsync();
        await _externalModules.DisposeAsync();
        await _cancellationScope.DisposeAsync();
    }

    public void Dispose()
    {
        _contentModules.Dispose();
        _externalModules.Dispose();
        _cancellationScope.DisposeAsync().GetAwaiter().GetResult();
    }
}