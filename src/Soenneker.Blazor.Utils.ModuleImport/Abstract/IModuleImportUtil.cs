using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;

namespace Soenneker.Blazor.Utils.ModuleImport.Abstract;

/// <summary>
/// Provides utilities for importing JavaScript ES modules via <c>import()</c> and caching the resulting module references.
/// </summary>
/// <remarks>
/// This utility supports two module sources:
/// <list type="bullet">
/// <item>
/// <description>
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>External modules</b> – Loaded from absolute URLs (e.g., CDN-hosted ESM).
/// </description>
/// </item>
/// </list>
/// <para>
/// Imported modules are cached to prevent redundant network requests and ensure reuse across calls.
/// </para>
/// <para>
/// This utility uses JavaScript dynamic <c>import()</c>. It does not support Subresource Integrity (SRI).
/// For SRI-enabled module loading, use a resource loader that injects a <c>&lt;script type="module"&gt;</c> tag.
/// </para>
/// </remarks>
public interface IModuleImportUtil : IAsyncDisposable
{
    /// <summary>
    /// Gets a cached content module item, initializing it if it has not yet been imported.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="ModuleImportItem"/> representing the module and its load state.</returns>
    ValueTask<ModuleImportItem> GetContentModule(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cached external module item, initializing it if it has not yet been imported.
    /// </summary>
    /// <param name="url">The absolute URL of the module.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ModuleImportItem"/> representing the module and its load state.</returns>
    ValueTask<ModuleImportItem> GetExternalModule(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cached JS module reference from the _content folder.
    /// Ensures the module is loaded before returning.
    /// </summary>
    ValueTask<IJSObjectReference> GetContentModuleReference(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cached JS module reference from an external URL.
    /// Ensures the module is loaded before returning.
    /// </summary>
    ValueTask<IJSObjectReference> GetExternalModuleReference(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disposes a previously imported content module and removes it from the cache.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
    ValueTask<bool> DisposeContentModule(string name);

    /// <summary>
    /// Disposes a previously imported external module and removes it from the cache.
    /// </summary>
    /// <param name="url">The absolute URL of the module.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
    ValueTask<bool> DisposeExternalModule(string url);
}