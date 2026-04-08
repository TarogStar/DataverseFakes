using Crm;
using FakeXrmEasy.Extensions;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.UpsertRequestTests
{
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
    public class UpsertRequestTests
    {
        [Fact]
        public void Upsert_Creates_Record_When_It_Does_Not_Exist()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      var service = context.GetOrganizationService();

            var contact = new Contact()
            {
                Id = Guid.NewGuid(),
                FirstName = "FakeXrm",
                LastName = "Easy"
            };

            var request = new UpsertRequest()
            {
                Target = contact
            };

            var response = (UpsertResponse)service.Execute(request);

            var contactCreated = context.CreateQuery<Contact>().FirstOrDefault();

            Assert.True(response.RecordCreated);
            Assert.NotNull(contactCreated);
        }

        [Fact]
        public void Upsert_Updates_Record_When_It_Exists()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      var service = context.GetOrganizationService();

            var contact = new Contact()
            {
                Id = Guid.NewGuid(),
                FirstName = "FakeXrm"
            };
            context.Initialize(new[] { contact });

            contact = new Contact()
            {
                Id = contact.Id,
                FirstName = "FakeXrm2",
                LastName = "Easy"
            };

            var request = new UpsertRequest()
            {
                Target = contact
            };


            var response = (UpsertResponse)service.Execute(request);
            var contactUpdated = context.CreateQuery<Contact>().FirstOrDefault();

            Assert.False(response.RecordCreated);
            Assert.Equal("FakeXrm2", contactUpdated.FirstName);
        }

        [Fact]
        public void Upsert_Creates_Record_When_It_Does_Not_Exist_Using_Alternate_Key()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());
            var service = context.GetOrganizationService();

            var metadata = context.GetEntityMetadataByName("contact");
            metadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[]{"firstname"}
                }
            });
            context.SetEntityMetadata(metadata);
            var contact = new Contact()
            {
                FirstName = "FakeXrm",
                LastName = "Easy"
            };
            contact.KeyAttributes.Add("firstname", contact.FirstName);

            var request = new UpsertRequest()
            {
                Target = contact
            };

            var response = (UpsertResponse)service.Execute(request);

            Assert.True(response.RecordCreated);
        }

        [Fact]
        public void Upsert_Updates_Record_When_It_Exists_Using_Alternate_Key()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());
            var service = context.GetOrganizationService();


            var metadata = context.GetEntityMetadataByName("contact");
            metadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[]{"firstname"}
                }
            });
            context.SetEntityMetadata(metadata);

            var contact = new Contact()
            {
                Id = Guid.NewGuid(),
                FirstName = "FakeXrm",
                LastName = "Easy"
            };
            context.Initialize(new[] { contact });

            contact = new Contact()
            {
                FirstName = "FakeXrm2",
                LastName = "Easy2"
            };

            contact.KeyAttributes.Add("firstname", "FakeXrm");

            var request = new UpsertRequest()
            {
                Target = contact
            };

            var response = (UpsertResponse)service.Execute(request);

            Assert.False(response.RecordCreated);
        }

        /// <summary>
        /// Tests Issue #566: When creating a record with Upsert using an alternate key,
        /// if the alternate key value is NOT present in the Attributes collection,
        /// it should be automatically copied from KeyAttributes to Attributes.
        ///
        /// Per Microsoft docs: "If there's no alternate key data in Entity.Attributes collection,
        /// the alternate key data from the Entity.KeyAttributes Property are copied into the
        /// Entity.Attributes collection."
        /// </summary>
        [Fact]
        public void Upsert_Create_With_Alt_Key_Should_Copy_Key_To_Attribute()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());
            var service = context.GetOrganizationService();

            var metadata = context.GetEntityMetadataByName("contact");
            metadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[]{"firstname"}
                }
            });
            context.SetEntityMetadata(metadata);

            // Create entity with alternate key ONLY in KeyAttributes, not in Attributes
            var contact = new Contact()
            {
                LastName = "Easy"  // Only LastName in Attributes, no FirstName
            };
            // Set FirstName only in KeyAttributes - this should be auto-copied to Attributes
            contact.KeyAttributes.Add("firstname", "FakeXrm");

            var request = new UpsertRequest()
            {
                Target = contact
            };

            // Act
            var response = (UpsertResponse)service.Execute(request);

            // Assert
            Assert.True(response.RecordCreated);

            // The key point of issue #566: The alternate key value should be copied to the attribute
            var contactCreated = context.CreateQuery<Contact>().FirstOrDefault();
            Assert.NotNull(contactCreated);
            Assert.Equal("FakeXrm", contactCreated.FirstName); // This is the key assertion - should have been copied from KeyAttributes
            Assert.Equal("Easy", contactCreated.LastName);
        }

        /// <summary>
        /// Tests Issue #566: When the alternate key value IS already present in Attributes,
        /// the KeyAttributes value should NOT override it (Attributes takes precedence).
        /// </summary>
        [Fact]
        public void Upsert_Create_With_Alt_Key_Should_Not_Override_Existing_Attribute()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());
            var service = context.GetOrganizationService();

            var metadata = context.GetEntityMetadataByName("contact");
            metadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[]{"firstname"}
                }
            });
            context.SetEntityMetadata(metadata);

            // Create entity with alternate key in BOTH KeyAttributes AND Attributes
            var contact = new Contact()
            {
                FirstName = "AttributeValue",  // Value in Attributes (should be used)
                LastName = "Easy"
            };
            // Different value in KeyAttributes - this should NOT override the Attribute value
            contact.KeyAttributes.Add("firstname", "KeyAttributeValue");

            var request = new UpsertRequest()
            {
                Target = contact
            };

            // Act
            var response = (UpsertResponse)service.Execute(request);

            // Assert
            Assert.True(response.RecordCreated);

            // The Attributes value should be preserved, not overwritten by KeyAttributes
            var contactCreated = context.CreateQuery<Contact>().FirstOrDefault();
            Assert.NotNull(contactCreated);
            Assert.Equal("AttributeValue", contactCreated.FirstName);
        }

        /// <summary>
        /// Tests Issue #566: Composite alternate keys should all be copied to attributes when creating.
        /// </summary>
        [Fact]
        public void Upsert_Create_With_Composite_Alt_Key_Should_Copy_All_Keys_To_Attributes()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());
            var service = context.GetOrganizationService();

            var metadata = context.GetEntityMetadataByName("contact");
            metadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[]{"firstname", "lastname"}  // Composite key
                }
            });
            context.SetEntityMetadata(metadata);

            // Create entity with ONLY KeyAttributes, no Attributes
            var contact = new Contact();  // Empty entity
            contact.KeyAttributes.Add("firstname", "John");
            contact.KeyAttributes.Add("lastname", "Doe");

            var request = new UpsertRequest()
            {
                Target = contact
            };

            // Act
            var response = (UpsertResponse)service.Execute(request);

            // Assert
            Assert.True(response.RecordCreated);

            // Both key attributes should be copied
            var contactCreated = context.CreateQuery<Contact>().FirstOrDefault();
            Assert.NotNull(contactCreated);
            Assert.Equal("John", contactCreated.FirstName);
            Assert.Equal("Doe", contactCreated.LastName);
        }
    }
#endif
}
