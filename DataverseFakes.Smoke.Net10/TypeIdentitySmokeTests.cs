using DataverseFakes;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DataverseFakes.Smoke.Net10
{
    public class TypeIdentitySmokeTests
    {
        [Fact]
        public void GetOrganizationService_satisfies_modern_IOrganizationService()
        {
            var context = new XrmFakedContext();

            // Identity proof: this assignment only compiles/runs if the IOrganizationService
            // the fake returns is the SAME assembly type that Dataverse.Client 1.2.10 brings
            // into this project. A mismatched (legacy) identity would not satisfy this variable.
            IOrganizationService service = context.GetOrganizationService();

            Assert.NotNull(service);
        }

        [Fact]
        public void Create_then_Retrieve_round_trips_on_net10()
        {
            var context = new XrmFakedContext();
            IOrganizationService service = context.GetOrganizationService();

            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
            var retrieved = service.Retrieve("account", id, new ColumnSet(true));

            Assert.Equal("Contoso", retrieved.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void XrmRealContext_compiles_and_returns_injected_service_on_net10()
        {
            // XrmRealContext now builds on net10 (ServiceClient instead of CrmServiceClient).
            // The IOrganizationService-injecting constructor needs no live org, so we can
            // prove the ported type works without connecting.
            IOrganizationService inner = new XrmFakedContext().GetOrganizationService();

            var real = new XrmRealContext(inner);

            Assert.Same(inner, real.GetOrganizationService());
        }
    }
}
