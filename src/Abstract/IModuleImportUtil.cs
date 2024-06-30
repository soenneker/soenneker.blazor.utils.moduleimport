using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using System.Threading;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;
using System.Diagnostics.Contracts;

namespace Soenneker.Blazor.Utils.ModuleImport.Abstract;

/// <summary>
/// A Blazor utility library assisting with asynchronous module loading
/// </summary>
public interface IModuleImportUtil : IAsyncDisposable
{
    /// <summary>
    /// Asynchronously retrieves a module by its name. If the module is not already loaded, it will be loaded.
    /// </summary>
    /// <param name="name">The name of the module to be retrieved.</param>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation, containing the <see cref="ModuleImportItem"/> for the specified module.</returns>
    /// <exception cref="ArgumentException">Thrown if the module name is null or invalid.</exception>
    [Pure]
    ValueTask<ModuleImportItem> GetModule(string name, CancellationToken cancellationToken = default);

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
    ValueTask ImportAndWait(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until the specified JavaScript module is loaded and JS variable is available (not undefined).
    /// </summary>
    /// <param name="name">The name of the script or style to wait for.</param>
    /// <param name="variableName">The name of the JavaScript variable to check for availability.</param>
    /// <param name="delay">The delay in milliseconds between each check for the variable's availability. The default is 100 milliseconds.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task completes when the variable is available or the operation is cancelled.</returns>
    ValueTask ImportAndWaitUntilAvailable(string name, string variableName, int delay = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disposes of the specified JavaScript module.
    /// </summary>
    /// <param name="name">The name of the JavaScript module to dispose.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask DisposeModule(string name, CancellationToken cancellationToken = default);
}