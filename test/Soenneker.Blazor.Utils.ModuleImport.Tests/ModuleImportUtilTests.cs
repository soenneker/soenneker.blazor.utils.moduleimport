using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;


namespace Soenneker.Blazor.Utils.ModuleImport.Tests;

[Collection("Collection")]
public class ModuleImportUtilTests : FixturedUnitTest
{
    private readonly IModuleImportUtil _util;

    public ModuleImportUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IModuleImportUtil>(true);
    }
}
