using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;

namespace Soenneker.Blazor.Utils.ModuleImport.Registrars;

/// <summary>
/// A Blazor utility library assisting with asynchronous module loading
/// </summary>
public static class ModuleImportUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IModuleImportUtil"/> as a singleton service. <para/>
    /// </summary>
    public static void AddModuleImportUtil(this IServiceCollection services)
    {
        services.TryAddSingleton<IModuleImportUtil, ModuleImportUtil>();
    }
}
