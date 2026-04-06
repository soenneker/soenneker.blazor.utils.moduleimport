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
/// <b>Content modules</b> – Loaded from the application's static web assets under <c>./_content/</c>.
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
public interface IModuleImportUtil : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets a cached content module item, initializing it if it has not yet been imported.
    /// </summary>
    /// <param name="name">The module path relative to <c>./_content/</c>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ModuleImportItem"/> representing the module and its load state.</returns>
    ValueTask<ModuleImportItem> GetContentModule(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cached external module item, initializing it if it has not yet been imported.
    /// </summary>
    /// <param name="url">The absolute URL of the module.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ModuleImportItem"/> representing the module and its load state.</returns>
    ValueTask<ModuleImportItem> GetExternalModule(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a JavaScript module from the application's static web assets (<c>./_content/</c>).
    /// </summary>
    /// <param name="name">The module path relative to <c>./_content/</c>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// An <see cref="IJSObjectReference"/> representing the imported module, allowing invocation of its exported members.
    /// </returns>
    ValueTask<IJSObjectReference> ImportContentModule(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a JavaScript module from an external URL using dynamic <c>import()</c>.
    /// </summary>
    /// <param name="url">The absolute URL of the module.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// An <see cref="IJSObjectReference"/> representing the imported module, allowing invocation of its exported members.
    /// </returns>
    /// <remarks>
    /// This method uses dynamic <c>import()</c> and does not support Subresource Integrity (SRI).
    /// </remarks>
    ValueTask<IJSObjectReference> ImportExternalModule(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disposes a previously imported content module and removes it from the cache.
    /// </summary>
    /// <param name="name">The module path relative to <c>./_content/</c>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
    ValueTask DisposeContentModule(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disposes a previously imported external module and removes it from the cache.
    /// </summary>
    /// <param name="url">The absolute URL of the module.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
    ValueTask DisposeExternalModule(string url, CancellationToken cancellationToken = default);
}