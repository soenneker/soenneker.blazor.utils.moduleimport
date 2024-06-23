[![](https://img.shields.io/nuget/v/soenneker.blazor.utils.moduleimport.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.utils.moduleimport/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.utils.moduleimport/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.utils.moduleimport/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.blazor.utils.moduleimport.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.utils.moduleimport/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Blazor.Utils.ModuleImport
### A Blazor utility library assisting with asynchronous module loading

This library simplifies the process of loading JavaScript modules and provides methods for waiting until a module is loaded and disposing of modules when they are no longer needed.

## Features

- Import JavaScript modules dynamically.
- Wait until a module is fully loaded.
- Dispose of JavaScript modules when they are no longer needed.
- Singleton pattern to ensure that each module is loaded only once.

## Installation

To install, add the package to your Blazor project using the .NET CLI:

```sh
dotnet add package Soenneker.Blazor.Utils.ModuleImport
```

Register it in DI:

```csharp
builder.Services.AddModuleImportUtil();
```

### Example

Here's an example of how to use the `ModuleImportUtil` in a Blazor component:

```csharp
@page "/example"
@inject IModuleImportUtil ModuleImportUtil

<h3>Module Import Example</h3>

<button @onclick="LoadModule">Load Module</button>

@code {
    private async Task LoadModule()
    {
        var module = await ModuleImportUtil.Import("exampleModule");
        await ModuleImportUtil.WaitUntilLoaded("exampleModule");

        // Guaranteed that the module has been added to the DOM, and available at this point
    }
}
```