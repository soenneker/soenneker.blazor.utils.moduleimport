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
/// <description><b>Content modules</b> – Loaded from application or Razor class library static web assets using a relative URL.</description>
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
    /// Gets a loaded, cached content module item, importing it when necessary.
    /// </summary>
    /// <param name="path">A relative application or static-web-asset module URL.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the import.</param>
    /// <returns>A <see cref="ModuleImportItem"/> representing the module and its load state.</returns>
    ValueTask<ModuleImportItem> GetContentModule(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a loaded, cached external module item, importing it when necessary.
    /// </summary>
    /// <param name="url">The absolute HTTP or HTTPS URL of the module.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ModuleImportItem"/> representing the module and its load state.</returns>
    ValueTask<ModuleImportItem> GetExternalModule(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a loaded, cached JavaScript module reference from application or Razor class library static web assets.
    /// </summary>
    /// <param name="path">Path of the file or directory to use.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested javaScript Object Reference.</returns>
    ValueTask<IJSObjectReference> GetContentModuleReference(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cached JS module reference from an external URL.
    /// Ensures the module is loaded before returning.
    /// </summary>
    /// <param name="url">The absolute HTTP or HTTPS URL of the module.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested javaScript Object Reference.</returns>
    ValueTask<IJSObjectReference> GetExternalModuleReference(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disposes a previously imported content module and removes it from the cache.
    /// </summary>
    /// <param name="name">The same relative content-module path used to import the module.</param>
    /// <returns><see langword="true"/> when a cached reference was removed; otherwise, <see langword="false"/>.</returns>
    ValueTask<bool> DisposeContentModule(string name);

    /// <summary>
    /// Disposes a previously imported external module and removes it from the cache.
    /// </summary>
    /// <param name="url">The absolute URL of the module.</param>
    /// <returns><see langword="true"/> when a cached reference was removed; otherwise, <see langword="false"/>.</returns>
    ValueTask<bool> DisposeExternalModule(string url);
}
