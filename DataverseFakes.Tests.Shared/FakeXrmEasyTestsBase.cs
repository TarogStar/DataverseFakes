using Microsoft.Xrm.Sdk;

namespace DataverseFakes.Tests
{
    public class DataverseFakesTestsBase
    {
        protected readonly IOrganizationService _service;
        protected readonly XrmFakedContext _context;

        public DataverseFakesTestsBase()
        {
            _context = new XrmFakedContext();
            _service = _context.GetOrganizationService();
        }
    }
}
