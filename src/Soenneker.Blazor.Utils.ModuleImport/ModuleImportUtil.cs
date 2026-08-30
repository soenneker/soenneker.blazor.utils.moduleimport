using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;
using Soenneker.Dictionaries.Singletons;
using Soenneker.Atomics.ValueBools;
using Soenneker.Extensions.CancellationTokens;
using Soenneker.Utils.CancellationScopes;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Blazor.Utils.ModuleImport;

/// <inheritdoc cref="IModuleImportUtil"/>
public sealed class ModuleImportUtil : IModuleImportUtil
{
    private readonly IJSRuntime _jsRuntime;
    private readonly SingletonDictionary<ModuleImportItem> _contentModules;
    private readonly SingletonDictionary<ModuleImportItem> _externalModules;
    private readonly CancellationScope _cancellationScope = new();
    private ValueAtomicBool _disposed;

    public ModuleImportUtil(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

        _contentModules = new SingletonDictionary<ModuleImportItem>(InitializeContentModule);
        _externalModules = new SingletonDictionary<ModuleImportItem>(InitializeExternalModule);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string NormalizeContentModulePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!path.Equals(path.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("Module paths cannot start or end with whitespace.", nameof(path));

        if (path.Contains('\\') || path.Contains("://", StringComparison.Ordinal))
            throw new ArgumentException("Content module paths must be relative URLs that use forward slashes.", nameof(path));

        string pathOnly = path.Split('?', '#')[0];

        foreach (string segment in pathOnly.Split('/'))
        {
            if (segment == "..")
                throw new ArgumentException("Relative parent path segments are not supported.", nameof(path));
        }

        if (path[0] == '.')
        {
            if (path.Length >= 2 && path[1] == '/')
                return path;

        }

        if (path[0] == '/')
            return "." + path;

        return "./" + path;
    }

    private static string NormalizeExternalModuleUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("External module URLs must be absolute HTTP or HTTPS URLs.", nameof(url));

        return uri.AbsoluteUri;
    }

    private async ValueTask<ModuleImportItem> InitializeContentModule(string path, CancellationToken cancellationToken)
    {
        var item = new ModuleImportItem();

        try
        {
            item.ScriptReference = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, path);
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
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        string normalizedPath = NormalizeContentModulePath(path);
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            ModuleImportItem item = await GetLoadedItem(_contentModules, normalizedPath, linked);
            return item.ScriptReference ?? throw new InvalidOperationException("The content module loaded without returning a reference.");
        }
    }

    public async ValueTask<IJSObjectReference> GetExternalModuleReference(string url, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        string normalizedUrl = NormalizeExternalModuleUrl(url);
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
        {
            ModuleImportItem item = await GetLoadedItem(_externalModules, normalizedUrl, linked);
            return item.ScriptReference ?? throw new InvalidOperationException("The external module loaded without returning a reference.");
        }
    }

    public async ValueTask<ModuleImportItem> GetContentModule(string path, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        string normalizedPath = NormalizeContentModulePath(path);
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
            return await GetLoadedItem(_contentModules, normalizedPath, linked);
    }

    public async ValueTask<ModuleImportItem> GetExternalModule(string url, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        string normalizedUrl = NormalizeExternalModuleUrl(url);
        CancellationToken linked = _cancellationScope.CancellationToken.Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
            return await GetLoadedItem(_externalModules, normalizedUrl, linked);
    }

    public ValueTask<bool> DisposeContentModule(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        return _contentModules.TryRemoveAndDispose(NormalizeContentModulePath(path));
    }

    public ValueTask<bool> DisposeExternalModule(string url)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        return _externalModules.TryRemoveAndDispose(NormalizeExternalModuleUrl(url));
    }

    private static async ValueTask<ModuleImportItem> GetLoadedItem(SingletonDictionary<ModuleImportItem> modules, string key,
        CancellationToken cancellationToken)
    {
        ModuleImportItem item = await modules.Get(key, cancellationToken);

        try
        {
            await item.Loaded.WaitAsync(cancellationToken);
            return item;
        }
        catch
        {
            if (modules.TryRemove(key, out ModuleImportItem? cachedItem) && cachedItem is not null)
                await cachedItem.DisposeAsync();

            throw;
        }
    }

    /// <summary>
    /// Asynchronously releases resources used by the current instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return;

        await _cancellationScope.DisposeAsync();
        await _contentModules.DisposeAsync();
        await _externalModules.DisposeAsync();
    }
}
