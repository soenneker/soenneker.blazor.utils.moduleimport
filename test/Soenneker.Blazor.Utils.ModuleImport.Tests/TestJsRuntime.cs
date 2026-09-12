using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.JSInterop;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Blazor.Utils.ModuleImport.Tests;

internal sealed class TestJsRuntime(int failuresBeforeSuccess = 0) : IJSRuntime
{
    private readonly TestJsObjectReference _module = new();
    private int _failuresRemaining = failuresBeforeSuccess;

    public int ImportCount { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        identifier.Should().Be("import");
        ImportCount++;

        if (_failuresRemaining-- > 0)
            return ValueTask.FromException<TValue>(new InvalidOperationException("Import failed."));

        return ValueTask.FromResult((TValue)(object)_module);
    }
}
