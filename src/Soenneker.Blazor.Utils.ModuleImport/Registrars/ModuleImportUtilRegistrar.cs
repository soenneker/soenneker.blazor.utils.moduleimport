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
    /// Adds <see cref="IModuleImportUtil"/> as a scoped service. <para/>
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddModuleImportUtilAsScoped(this IServiceCollection services)
    {
        services.TryAddScoped<IModuleImportUtil, ModuleImportUtil>();

        return services;
    }
}
