using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.JSInterop;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Blazor.Utils.ModuleImport.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class ModuleImportUtilTests : HostedUnitTest
{
    public ModuleImportUtilTests(Host host) : base(host)
    {
    }

    [Test]
    public async Task Equivalent_content_paths_share_one_import(CancellationToken cancellationToken)
    {
        var jsRuntime = new TestJsRuntime();
        await using var modules = new ModuleImportUtil(jsRuntime);

        IJSObjectReference first = await modules.GetContentModuleReference("/js/example.js", cancellationToken: cancellationToken);
        IJSObjectReference second = await modules.GetContentModuleReference("./js/example.js", cancellationToken: cancellationToken);

        ReferenceEquals(first, second).Should().BeTrue();
        jsRuntime.ImportCount.Should().Be(1);
    }

    [Test]
    public async Task Failed_import_can_be_retried(CancellationToken cancellationToken)
    {
        var jsRuntime = new TestJsRuntime(failuresBeforeSuccess: 1);
        await using var modules = new ModuleImportUtil(jsRuntime);

        Func<Task> firstAttempt = async () => await modules.GetContentModuleReference("/js/retry.js", cancellationToken: cancellationToken);
        await firstAttempt.Should().ThrowAsync<InvalidOperationException>();

        IJSObjectReference module = await modules.GetContentModuleReference("/js/retry.js", cancellationToken: cancellationToken);

        module.Should().NotBeNull();
        jsRuntime.ImportCount.Should().Be(2);
    }
}
