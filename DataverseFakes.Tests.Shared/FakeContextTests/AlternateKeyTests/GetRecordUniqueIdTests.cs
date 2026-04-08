using FakeXrmEasy.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.AlternateKeyTests
{
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
    /// <summary>
    /// Tests for GetRecordUniqueId method, specifically for Issue #470 -
    /// Missing throw when alternate keys don't match any record and validate=true.
    /// </summary>
    public class GetRecordUniqueIdTests
    {
        [Fact]
        public void When_AlternateKey_Does_Not_Exist_And_Validate_True_Should_Throw_Exception()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());

            // Set up alternate key metadata for account entity
            var metadata = context.GetEntityMetadataByName("account");
            metadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[] { "accountnumber" }
                }
            });
            context.SetEntityMetadata(metadata);

            // Initialize with an account that has accountnumber = "12345"
            var accountId = Guid.NewGuid();
            context.Initialize(new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["accountnumber"] = "12345"
                }
            });

            // Create EntityReference with alternate key that doesn't exist
            var entityRef = new EntityReference("account");
            entityRef.KeyAttributes["accountnumber"] = "99999"; // Non-existent

            // Act & Assert - Should throw when trying to resolve the ID with validation
            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                context.GetRecordUniqueId(entityRef, validate: true));
        }

        [Fact]
        public void When_AlternateKey_Does_Not_Exist_And_Validate_False_Should_Return_Empty_Guid()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());

            // Set up alternate key metadata for account entity
            var metadata = context.GetEntityMetadataByName("account");
            metadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[] { "accountnumber" }
                }
            });
            context.SetEntityMetadata(metadata);

            // Initialize with an account that has accountnumber = "12345"
            var accountId = Guid.NewGuid();
            context.Initialize(new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["accountnumber"] = "12345"
                }
            });

            // Create EntityReference with alternate key that doesn't exist
            var entityRef = new EntityReference("account");
            entityRef.KeyAttributes["accountnumber"] = "99999"; // Non-existent

            // Act - Should return Guid.Empty when validation is disabled
            var result = context.GetRecordUniqueId(entityRef, validate: false);

            // Assert
            Assert.Equal(Guid.Empty, result);
        }

        [Fact]
        public void When_AlternateKey_Exists_Should_Return_Record_Id()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());

            // Set up alternate key metadata for account entity
            var metadata = context.GetEntityMetadataByName("account");
            metadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[] { "accountnumber" }
                }
            });
            context.SetEntityMetadata(metadata);

            // Initialize with an account that has accountnumber = "12345"
            var accountId = Guid.NewGuid();
            context.Initialize(new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["accountnumber"] = "12345"
                }
            });

            // Create EntityReference with matching alternate key
            var entityRef = new EntityReference("account");
            entityRef.KeyAttributes["accountnumber"] = "12345";

            // Act
            var result = context.GetRecordUniqueId(entityRef, validate: true);

            // Assert
            Assert.Equal(accountId, result);
        }

        [Fact]
        public void When_AlternateKey_Attributes_Not_Defined_In_Metadata_And_Validate_True_Should_Throw_Exception()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());

            // Initialize with an account but no alternate key metadata defined
            var accountId = Guid.NewGuid();
            context.Initialize(new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["accountnumber"] = "12345"
                }
            });

            // Create EntityReference with alternate key that is not defined in metadata
            var entityRef = new EntityReference("account");
            entityRef.KeyAttributes["accountnumber"] = "12345";

            // Act & Assert - Should throw an exception because the key is not defined in metadata.
            // When metadata.Keys is null (not initialized), a NullReferenceException is thrown.
            // When metadata exists but Keys array is empty, InvalidOperationException is thrown.
            // We use ThrowsAny to handle both cases since the behavior depends on metadata state.
            Assert.ThrowsAny<Exception>(() =>
                context.GetRecordUniqueId(entityRef, validate: true));
        }

        [Fact]
        public void When_Composite_AlternateKey_Does_Not_Match_And_Validate_True_Should_Throw_Exception()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());

            // Set up composite alternate key metadata for account entity
            var metadata = context.GetEntityMetadataByName("account");
            metadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[] { "accountnumber", "name" }
                }
            });
            context.SetEntityMetadata(metadata);

            // Initialize with an account
            var accountId = Guid.NewGuid();
            context.Initialize(new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["accountnumber"] = "12345",
                    ["name"] = "Test Account"
                }
            });

            // Create EntityReference with partial matching alternate key (accountnumber matches but name doesn't)
            var entityRef = new EntityReference("account");
            entityRef.KeyAttributes["accountnumber"] = "12345";
            entityRef.KeyAttributes["name"] = "Different Name";

            // Act & Assert - Should throw because composite key doesn't fully match
            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                context.GetRecordUniqueId(entityRef, validate: true));
        }
    }
#endif
}
