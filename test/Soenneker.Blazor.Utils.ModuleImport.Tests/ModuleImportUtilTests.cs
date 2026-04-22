using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Blazor.Utils.ModuleImport.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class ModuleImportUtilTests : HostedUnitTest
{
    private readonly IModuleImportUtil _util;

    public ModuleImportUtilTests(Host host) : base(host)
    {
        _util = Resolve<IModuleImportUtil>(true);
    }

    [Test]
    public void Default()
    {

    }
}
