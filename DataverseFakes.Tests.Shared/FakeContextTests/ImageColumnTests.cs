using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests
{
    /// <summary>
    /// Tests that image (byte[]) columns follow real Dataverse behavior:
    /// - Retrieve (single record): returns image binary data
    /// - RetrieveMultiple (batch query): silently omits image binary data
    /// - Both accept entityimage in ColumnSet without throwing
    /// </summary>
    public class ImageColumnTests
    {
        private readonly byte[] _testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        [Fact]
        public void Retrieve_WithImageColumn_ReturnsImageData()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var contactId = Guid.NewGuid();
            var contact = new Entity("contact", contactId);
            contact["fullname"] = "Test User";
            contact["entityimage"] = _testImageBytes;
            contact["entityimage_url"] = "/Image/download.aspx?Id=" + contactId;

            context.Initialize(new List<Entity> { contact });

            var result = service.Retrieve("contact", contactId,
                new ColumnSet("fullname", "entityimage", "entityimage_url"));

            Assert.Equal("Test User", result["fullname"]);
            Assert.NotNull(result["entityimage"]);
            Assert.IsType<byte[]>(result["entityimage"]);
            Assert.Equal(_testImageBytes, (byte[])result["entityimage"]);
            Assert.NotNull(result["entityimage_url"]);
        }

        [Fact]
        public void RetrieveMultiple_WithImageColumn_OmitsImageData()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var contactId = Guid.NewGuid();
            var contact = new Entity("contact", contactId);
            contact["fullname"] = "Test User";
            contact["entityimage"] = _testImageBytes;
            contact["entityimage_url"] = "/Image/download.aspx?Id=" + contactId;

            context.Initialize(new List<Entity> { contact });

            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("fullname", "entityimage", "entityimage_url")
            };

            var results = service.RetrieveMultiple(query);

            Assert.Single(results.Entities);
            var result = results.Entities[0];
            Assert.Equal("Test User", result["fullname"]);
            Assert.NotNull(result["entityimage_url"]);
            // Image binary should NOT be returned by RetrieveMultiple — matches real Dataverse behavior
            Assert.False(result.Attributes.ContainsKey("entityimage"),
                "entityimage binary should not be returned by RetrieveMultiple");
        }

        [Fact]
        public void RetrieveMultiple_WithImageColumn_DoesNotThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var contact = new Entity("contact", Guid.NewGuid());
            contact["fullname"] = "Test User";

            context.Initialize(new List<Entity> { contact });

            // Including entityimage in ColumnSet should not throw even when no image exists
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("fullname", "entityimage")
            };

            var results = service.RetrieveMultiple(query);
            Assert.Single(results.Entities);
            Assert.Equal("Test User", results.Entities[0]["fullname"]);
        }

        [Fact]
        public void Retrieve_WithOnlyImageColumn_ReturnsImage()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var contactId = Guid.NewGuid();
            var contact = new Entity("contact", contactId);
            contact["fullname"] = "Test User";
            contact["entityimage"] = _testImageBytes;

            context.Initialize(new List<Entity> { contact });

            // Retrieve with only the image column — common pattern for fetching image after batch query
            var result = service.Retrieve("contact", contactId,
                new ColumnSet("entityimage"));

            Assert.NotNull(result["entityimage"]);
            Assert.Equal(_testImageBytes, (byte[])result["entityimage"]);
        }

        [Fact]
        public void RetrieveMultiple_MultipleRecords_OmitsImageFromAll()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var contacts = new List<Entity>();
            for (int i = 0; i < 3; i++)
            {
                var contact = new Entity("contact", Guid.NewGuid());
                contact["fullname"] = $"User {i}";
                contact["entityimage"] = _testImageBytes;
                contact["entityimage_url"] = "/Image/download.aspx?Id=" + contact.Id;
                contacts.Add(contact);
            }

            context.Initialize(contacts);

            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("fullname", "entityimage", "entityimage_url")
            };

            var results = service.RetrieveMultiple(query);
            Assert.Equal(3, results.Entities.Count);

            foreach (var result in results.Entities)
            {
                Assert.NotNull(result["entityimage_url"]);
                Assert.False(result.Attributes.ContainsKey("entityimage"),
                    "entityimage binary should not be returned by RetrieveMultiple for any record");
            }
        }
    }
}
