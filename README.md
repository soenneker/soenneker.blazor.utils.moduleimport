[![](https://img.shields.io/nuget/v/soenneker.blazor.utils.moduleimport.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.utils.moduleimport/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.utils.moduleimport/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.utils.moduleimport/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.blazor.utils.moduleimport.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.utils.moduleimport/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.utils.moduleimport/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.utils.moduleimport/actions/workflows/codeql.yml)

# Soenneker.Blazor.Utils.ModuleImport

A scoped Blazor utility for dynamically importing and reusing JavaScript ES module references.

It supports relative application/static-web-asset modules and absolute HTTP(S) modules. Concurrent callers for the same normalized location share one cached import.

## Installation

```bash
dotnet add package Soenneker.Blazor.Utils.ModuleImport
```

```csharp
using Soenneker.Blazor.Utils.ModuleImport.Registrars;

builder.Services.AddModuleImportUtilAsScoped();
```

Inject `IModuleImportUtil` into an interop service. Module imports require an interactive browser renderer and cannot run during server prerendering.

## Import an application module

For a file at `wwwroot/js/orders.js`:

```javascript
export function formatOrderNumber(value) {
    return `ORD-${value}`;
}
```

```csharp
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;

public sealed class OrdersInterop(IModuleImportUtil modules)
{
    private const string ModulePath = "/js/orders.js";

    public async ValueTask<string> Format(
        int value,
        CancellationToken cancellationToken = default)
    {
        IJSObjectReference module =
            await modules.GetContentModuleReference(ModulePath, cancellationToken);

        return await module.InvokeAsync<string>(
            "formatOrderNumber",
            cancellationToken,
            value);
    }
}
```

For a Razor class library static asset, use its `_content` URL:

```csharp
const string ModulePath =
    "_content/Example.Components/js/widget.js";
```

Leading `/`, `./`, or no prefix normalize to the same relative import URL. Backslashes, absolute URLs, and parent (`..`) path segments are rejected by the content-module APIs.

## Import an external module

```csharp
IJSObjectReference module =
    await modules.GetExternalModuleReference(
        "https://cdn.example.com/library/4.2.0/index.js",
        cancellationToken);
```

External imports accept only absolute HTTP or HTTPS URLs. The remote server must allow module loading under browser CORS rules, and the application’s Content Security Policy must permit the source.

Dynamic `import()` does not provide Subresource Integrity through this API. Pin an immutable version, trust the host, and never accept a module URL from user input. A remotely imported module executes code in the page and is part of the application’s supply chain.

## Module items

Most callers should use the reference methods. `GetContentModule` and `GetExternalModule` return a `ModuleImportItem` after loading succeeds:

```csharp
ModuleImportItem item = await modules.GetContentModule(ModulePath);
IJSObjectReference module = item.ScriptReference!;
```

`Loaded` is already complete when these methods return and remains available for compatibility. The item’s completion source and reference setter are controlled by the library.

Failed or cancelled imports are evicted from the utility cache, allowing a later call to retry. Cancellation stops the .NET caller from waiting, but the browser may already have fetched or evaluated part of the module.

## Ownership and disposal

The utility owns cached `ModuleImportItem` and `IJSObjectReference` instances. Consumers should not dispose returned items or references directly.

Remove one cached handle when its owner is finished and no other consumer in the same scope uses it:

```csharp
bool removed = await modules.DisposeContentModule(ModulePath);
```

```csharp
bool removed = await modules.DisposeExternalModule(externalUrl);
```

Disposal removes and releases the cached Blazor reference. It does not unload JavaScript code from the browser or guarantee that a later dynamic import re-evaluates the module; browsers cache ES modules by resolved URL, so module-level state can persist.

The registry is scoped. In Blazor Server that normally means a circuit; in WebAssembly it normally means the application. Remaining references are disposed with the scope.

Do not evict a module while another consumer is invoking it. If multiple services share a module, let the scope own its lifetime or coordinate a single owner instead of disposing it from each consumer.

Module paths and export names should be trusted application constants. Values returned by JavaScript are still untrusted input and require validation before privileged use.
