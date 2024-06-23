using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Soenneker.Blazor.Utils.ModuleImport.Abstract;

/// <summary>
/// A Blazor utility library assisting with asynchronous module loading
/// </summary>
public interface IModuleImportUtil : IAsyncDisposable
{
    /// <summary>
    /// Imports a JavaScript module by its name.
    /// </summary>
    /// <param name="name">The name of the JavaScript module to import.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the imported JavaScript module reference.</returns>
    ValueTask<IJSObjectReference> Import(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until the specified module is loaded.
    /// </summary>
    /// <param name="name">The name of the JavaScript module.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask WaitUntilLoaded(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disposes of the specified JavaScript module.
    /// </summary>
    /// <param name="name">The name of the JavaScript module to dispose.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask DisposeModule(string name, CancellationToken cancellationToken = default);
}