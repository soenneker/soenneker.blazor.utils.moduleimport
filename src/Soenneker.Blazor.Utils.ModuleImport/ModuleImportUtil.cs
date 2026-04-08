using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;
using Soenneker.Dictionaries.Singletons;
using Soenneker.Extensions.CancellationTokens;
using Soenneker.Utils.CancellationScopes;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Blazor.Utils.ModuleImport;

///<inheritdoc cref="IModuleImportUtil"/>
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string NormalizeContentModulePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (path[0] == '.')
        {
            if (path.Length >= 2 && path[1] == '/')
                return path;

            if (path.Length >= 3 && path[1] == '.' && path[2] == '/')
                throw new ArgumentException("Relative parent paths (../) are not supported. Use './_content/...' or './...'.", nameof(path));
        }

        if (path[0] == '/')
            return "." + path;

        return "./" + path;
    }

    private async ValueTask<ModuleImportItem> InitializeContentModule(string path, CancellationToken cancellationToken)
    {
        var item = new ModuleImportItem();

        try
        {
            string resolved = NormalizeContentModulePath(path);
            item.ScriptReference = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, resolved);
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

    public async ValueTask<IJSObjectReference> GetContentModuleReference(string path, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            ModuleImportItem item = await _contentModules.Get(path, linked);
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

    public async ValueTask<ModuleImportItem> GetContentModule(string path, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
            return await _contentModules.Get(path, linked);
    }

    public async ValueTask<ModuleImportItem> GetExternalModule(string url, CancellationToken cancellationToken = default)
    {
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
            return await _externalModules.Get(url, linked);
    }

    public ValueTask<bool> DisposeContentModule(string path)
    {
        return _contentModules.TryRemoveAndDispose(path);
    }

    public ValueTask<bool> DisposeExternalModule(string url)
    {
        return _externalModules.TryRemoveAndDispose(url);
    }

    public async ValueTask DisposeAsync()
    {
        await _contentModules.DisposeAsync();
        await _externalModules.DisposeAsync();
        await _cancellationScope.DisposeAsync();
    }
}