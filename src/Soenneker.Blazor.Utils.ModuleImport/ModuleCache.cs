using Soenneker.Asyncs.Locks;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;

namespace Soenneker.Blazor.Utils.ModuleImport;

internal sealed class ModuleCache(Func<string, CancellationToken, ValueTask<ModuleImportItem>> import)
    : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, object> _entries = new(1, 4, StringComparer.Ordinal);
    private readonly AsyncLock _gate = new();
    private bool _disposed;

    internal bool TryGet(string path, out ModuleImportItem? item)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
        if (_entries.TryGetValue(path, out object? value) && value is ModuleImportItem loaded)
        {
            item = loaded;
            return true;
        }
        item = null;
        return false;
    }

    internal async ValueTask<ModuleImportItem> Get(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TaskCompletionSource<ModuleImportItem> entry;
        bool created = false;
        using (await _gate.Lock(cancellationToken).ConfigureAwait(false))
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_entries.TryGetValue(path, out object? value))
            {
                entry = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _entries[path] = entry;
                created = true;
            }
            else if (value is ModuleImportItem item)
                return item;
            else
                entry = (TaskCompletionSource<ModuleImportItem>)value;
        }
        if (created)
            _ = Initialize(path, entry, cancellationToken);
        return await entry.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    internal bool TryGetContent(string path, out ModuleImportItem? item)
    {
        if (path.StartsWith("./", StringComparison.Ordinal))
            return TryGet(path, out item);

        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
        int prefixLength = path[0] == '/' ? 1 : 2;
        int length = checked(path.Length + prefixLength);
        char[]? rented = null;
        Span<char> normalized = length <= 256 ? stackalloc char[length] : (rented = ArrayPool<char>.Shared.Rent(length)).AsSpan(0, length);
        try
        {
            normalized[0] = '.';
            if (prefixLength == 2)
                normalized[1] = '/';
            path.AsSpan().CopyTo(normalized[prefixLength..]);
            if (_entries.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(normalized, out object? value) && value is ModuleImportItem loaded)
            {
                item = loaded;
                return true;
            }
            item = null;
            return false;
        }
        finally
        {
            if (rented is not null)
                ArrayPool<char>.Shared.Return(rented, clearArray: true);
        }
    }

    private async Task Initialize(string path, TaskCompletionSource<ModuleImportItem> entry, CancellationToken cancellationToken)
    {
        try
        {
            ModuleImportItem item = await import(path, cancellationToken).ConfigureAwait(false);
            // Retain just the reference holder once ready; existing waiters keep
            // their completion task. Eviction/disposal can already own that task.
            _entries.TryUpdate(path, item, entry);
            entry.SetResult(item);
        }
        catch (Exception exception)
        {
            _entries.TryRemove(new KeyValuePair<string, object>(path, entry));
            if (exception is OperationCanceledException cancelled)
                entry.SetCanceled(cancelled.CancellationToken);
            else
            {
                entry.SetException(exception);
                // All callers may have cancelled their waits before the factory failed.
                _ = entry.Task.Exception;
            }
        }
    }

    internal async ValueTask<bool> Evict(string path)
    {
        object? entry;
        using (await _gate.Lock().ConfigureAwait(false))
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_entries.TryRemove(path, out entry))
                return false;
        }

        ModuleImportItem item;
        try
        {
            item = entry is ModuleImportItem loaded ? loaded : await ((TaskCompletionSource<ModuleImportItem>)entry).Task.ConfigureAwait(false);
        }
        catch
        {
            // There is no reference to dispose when the import itself failed.
            return false;
        }
        await item.DisposeAsync().ConfigureAwait(false);
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        ICollection<object> entries;
        using (await _gate.Lock().ConfigureAwait(false))
        {
            if (_disposed)
                return;
            _disposed = true;
            entries = _entries.Values;
            _entries.Clear();
        }
        List<Exception>? errors = null;
        foreach (object entry in entries)
        {
            ModuleImportItem item;
            try { item = entry is ModuleImportItem loaded ? loaded : await ((TaskCompletionSource<ModuleImportItem>)entry).Task.ConfigureAwait(false); }
            catch { continue; }
            try { await item.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception) { (errors ??= []).Add(exception); }
        }
        if (errors is not null)
            throw new AggregateException("One or more JavaScript modules could not be disposed.", errors);
    }
}
