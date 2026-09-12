using Soenneker.Asyncs.Locks;
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;
using Soenneker.Atomics.ValueBools;
using Soenneker.Extensions.CancellationTokens;
using Soenneker.Utils.CancellationScopes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Blazor.Utils.ModuleImport;

public sealed class ModuleImportUtil : IModuleImportUtil
{
    private readonly IJSRuntime _jsRuntime;
    private ModuleCache? _contentModules;
    private ModuleCache? _externalModules;
    private readonly CancellationScope _lifetimeCancellation = new();
    private ValueAtomicBool _disposed;
    private readonly AsyncLock _lifetimeGate = new();

    public ModuleImportUtil(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string NormalizeContentModulePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (char.IsWhiteSpace(path[0]) || char.IsWhiteSpace(path[^1]))
            throw new ArgumentException("Module paths cannot start or end with whitespace.", nameof(path));

        if (path.Contains('\\') || path.Contains("://", StringComparison.Ordinal))
            throw new ArgumentException("Content module paths must be relative URLs that use forward slashes.",
                nameof(path));

        ReadOnlySpan<char> pathOnly = path.AsSpan();
        int suffixStart = pathOnly.IndexOfAny('?', '#');
        if (suffixStart >= 0)
            pathOnly = pathOnly[..suffixStart];

        foreach (Range segment in pathOnly.Split('/'))
        {
            if (pathOnly[segment] is "..")
                throw new ArgumentException("Relative parent path segments are not supported.", nameof(path));
        }

        if (path.StartsWith("./", StringComparison.Ordinal))
            return path;

        if (path[0] == '/')
            return "." + path;

        return "./" + path;
    }

    private static string NormalizeExternalModuleUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("External module URLs must be absolute HTTP or HTTPS URLs.", nameof(url));

        return uri.AbsoluteUri;
    }

    private async ValueTask<ModuleImportItem> InitializeModule(string path, CancellationToken cancellationToken)
    {
        // Publish only successful imports. Failed factories are never cached, so a
        // failed waiter cannot evict a newer successful retry.
        IJSObjectReference reference =
            await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, path);
        return new ModuleImportItem(reference);
    }

    public ValueTask<IJSObjectReference> GetContentModuleReference(string path,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(path);
        ModuleCache? modules = Volatile.Read(ref _contentModules);
        // Exact canonical hits are already validated. Avoid projecting through
        // an intermediate ValueTask<ModuleImportItem> on the interop hot path.
        if (modules is not null && path.StartsWith("./", StringComparison.Ordinal) &&
            modules.TryGet(path, out ModuleImportItem? item))
            return new ValueTask<IJSObjectReference>(item!.ScriptReference!);
        return GetReference(GetContentModule(path, cancellationToken));
    }

    public ValueTask<IJSObjectReference> GetExternalModuleReference(string url,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(url);
        ModuleCache? modules = Volatile.Read(ref _externalModules);
        if (modules is not null && modules.TryGet(url, out ModuleImportItem? item))
            return new ValueTask<IJSObjectReference>(item!.ScriptReference!);
        return GetReference(GetExternalModule(url, cancellationToken));
    }

    private static ValueTask<IJSObjectReference> GetReference(ValueTask<ModuleImportItem> loading)
    {
        return loading.IsCompletedSuccessfully
            ? new ValueTask<IJSObjectReference>(loading.Result.ScriptReference!)
            : AwaitReference(loading);
    }

    private static async ValueTask<IJSObjectReference> AwaitReference(ValueTask<ModuleImportItem> loading)
    {
        return (await loading).ScriptReference!;
    }

    public ValueTask<ModuleImportItem> GetContentModule(string path, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateContentPathEdges(path);

        ModuleCache modules = Volatile.Read(ref _contentModules) ?? CreateModules(true);
        if (modules.TryGetContent(path, out ModuleImportItem? item))
            return new ValueTask<ModuleImportItem>(item!);

        string normalizedPath = NormalizeContentModulePath(path);
        return GetUncachedItem(modules, normalizedPath, cancellationToken);
    }

    private static void ValidateContentPathEdges(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        // These invalid raw spellings could otherwise compare equal to a valid
        // canonical path with whitespace inside its first segment.
        if (char.IsWhiteSpace(path[0]) || char.IsWhiteSpace(path[^1]))
            throw new ArgumentException("Module paths cannot start or end with whitespace.", nameof(path));
    }

    public ValueTask<ModuleImportItem> GetExternalModule(string url, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(url);

        ModuleCache modules = Volatile.Read(ref _externalModules) ?? CreateModules(false);
        if (modules.TryGet(url, out ModuleImportItem? item))
            return new ValueTask<ModuleImportItem>(item!);

        string normalizedUrl = NormalizeExternalModuleUrl(url);
        if (normalizedUrl != url && modules.TryGet(normalizedUrl, out item))
            return new ValueTask<ModuleImportItem>(item!);

        return GetUncachedItem(modules, normalizedUrl, cancellationToken);
    }

    public ValueTask<bool> DisposeContentModule(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        string normalized = NormalizeContentModulePath(path);
        return Volatile.Read(ref _contentModules)?.Evict(normalized) ?? new ValueTask<bool>(false);
    }

    public ValueTask<bool> DisposeExternalModule(string url)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        string normalized = NormalizeExternalModuleUrl(url);
        return Volatile.Read(ref _externalModules)?.Evict(normalized) ?? new ValueTask<bool>(false);
    }

    private ModuleCache CreateModules(bool content)
    {
        using (_lifetimeGate.LockSync())
        {
            ObjectDisposedException.ThrowIf(_disposed.Value, this);
            if (content)
                return _contentModules ??= new ModuleCache(InitializeModule);
            return _externalModules ??= new ModuleCache(InitializeModule);
        }
    }

    private async ValueTask<ModuleImportItem> GetUncachedItem(ModuleCache modules, string key,
        CancellationToken cancellationToken)
    {
        CancellationToken linked = GetLifetimeToken().Link(cancellationToken, out CancellationTokenSource? source);

        using (source)
            return await modules.Get(key, linked);
    }

    private CancellationToken GetLifetimeToken()
    {
        using (_lifetimeGate.LockSync())
        {
            ObjectDisposedException.ThrowIf(_disposed.Value, this);
            return _lifetimeCancellation.CancellationToken;
        }
    }

    public async ValueTask DisposeAsync()
    {
        using (await _lifetimeGate.Lock().ConfigureAwait(false))
        {
            if (!_disposed.TrySetTrue())
                return;
        }

        await _lifetimeCancellation.DisposeAsync();
        List<Exception>? exceptions = null;
        try
        {
            if (_contentModules is not null)
                await _contentModules.DisposeAsync();
        }
        catch (Exception exception)
        {
            (exceptions ??= []).Add(exception);
        }

        try
        {
            if (_externalModules is not null)
                await _externalModules.DisposeAsync();
        }
        catch (Exception exception)
        {
            (exceptions ??= []).Add(exception);
        }

        if (exceptions is not null)
            throw new AggregateException("One or more JavaScript modules could not be disposed.", exceptions);
    }
}