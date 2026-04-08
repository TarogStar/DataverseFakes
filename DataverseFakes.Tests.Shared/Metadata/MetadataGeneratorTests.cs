using FakeXrmEasy.Extensions;
using FakeXrmEasy.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.Metadata
{
    /// <summary>
    /// Tests for the public MetadataGenerator API (GitHub issue #557).
    /// Verifies that users can generate metadata directly without going through XrmFakedContext.
    /// </summary>
    public class MetadataGeneratorTests
    {
        #region FromEarlyBoundEntities Tests

        [Fact]
        public void FromEarlyBoundEntities_Should_Generate_Metadata_For_Assembly()
        {
            // Arrange
            var assembly = typeof(Crm.Account).Assembly;

            // Act
            var metadata = MetadataGenerator.FromEarlyBoundEntities(assembly);

            // Assert
            Assert.NotNull(metadata);
            Assert.True(metadata.Any());
        }

        [Fact]
        public void FromEarlyBoundEntities_Should_Include_Account_Entity()
        {
            // Arrange
            var assembly = typeof(Crm.Account).Assembly;

            // Act
            var metadata = MetadataGenerator.FromEarlyBoundEntities(assembly);
            var accountMetadata = metadata.FirstOrDefault(m => m.LogicalName == "account");

            // Assert
            Assert.NotNull(accountMetadata);
            Assert.Equal("account", accountMetadata.LogicalName);
        }

        [Fact]
        public void FromEarlyBoundEntities_Should_Set_Primary_Id_Attribute()
        {
            // Arrange
            var assembly = typeof(Crm.Account).Assembly;

            // Act
            var metadata = MetadataGenerator.FromEarlyBoundEntities(assembly);
            var accountMetadata = metadata.FirstOrDefault(m => m.LogicalName == "account");

            // Assert
            Assert.Equal("accountid", accountMetadata.PrimaryIdAttribute);
        }

        #endregion

        #region FromEarlyBoundEntity Generic Tests

        [Fact]
        public void FromEarlyBoundEntity_Generic_Should_Generate_Metadata_For_Single_Entity()
        {
            // Act
            var accountMetadata = MetadataGenerator.FromEarlyBoundEntity<Crm.Account>();

            // Assert
            Assert.NotNull(accountMetadata);
            Assert.Equal("account", accountMetadata.LogicalName);
            Assert.Equal("accountid", accountMetadata.PrimaryIdAttribute);
        }

        [Fact]
        public void FromEarlyBoundEntity_Generic_Should_Include_Attributes()
        {
            // Act
            var accountMetadata = MetadataGenerator.FromEarlyBoundEntity<Crm.Account>();

            // Assert
            Assert.NotNull(accountMetadata.Attributes);
            Assert.True(accountMetadata.Attributes.Length > 0);
        }

        [Fact]
        public void FromEarlyBoundEntity_Generic_Should_Set_Primary_Id_AttributeType()
        {
            // Act
            var accountMetadata = MetadataGenerator.FromEarlyBoundEntity<Crm.Account>();
            var primaryIdAttribute = accountMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "accountid");

            // Assert
            Assert.NotNull(primaryIdAttribute);
            Assert.Equal(AttributeTypeCode.Uniqueidentifier, primaryIdAttribute.AttributeType);
        }

        #endregion

        #region FromEarlyBoundEntity Type Tests

        [Fact]
        public void FromEarlyBoundEntity_Type_Should_Generate_Metadata_For_Single_Entity()
        {
            // Arrange
            var entityType = typeof(Crm.Account);

            // Act
            var accountMetadata = MetadataGenerator.FromEarlyBoundEntity(entityType);

            // Assert
            Assert.NotNull(accountMetadata);
            Assert.Equal("account", accountMetadata.LogicalName);
        }

        [Fact]
        public void FromEarlyBoundEntity_Type_Should_Return_Null_For_Non_Entity_Type()
        {
            // Arrange
            var nonEntityType = typeof(string);

            // Act
            var metadata = MetadataGenerator.FromEarlyBoundEntity(nonEntityType);

            // Assert
            Assert.Null(metadata);
        }

        #endregion

        #region CreateAttributeMetadataByType Tests

        [Fact]
        public void CreateAttributeMetadataByType_Should_Return_StringAttributeMetadata_For_String()
        {
            // Act
            var metadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(string));

            // Assert
            Assert.IsType<StringAttributeMetadata>(metadata);
        }

        [Fact]
        public void CreateAttributeMetadataByType_Should_Return_LookupAttributeMetadata_For_EntityReference()
        {
            // Act
            var metadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(EntityReference));

            // Assert
            Assert.IsType<LookupAttributeMetadata>(metadata);
        }

        [Fact]
        public void CreateAttributeMetadataByType_Should_Return_PicklistAttributeMetadata_For_OptionSetValue()
        {
            // Act
            var metadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(OptionSetValue));

            // Assert
            Assert.IsType<PicklistAttributeMetadata>(metadata);
        }

        [Fact]
        public void CreateAttributeMetadataByType_Should_Return_MoneyAttributeMetadata_For_Money()
        {
            // Act
            var metadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(Money));

            // Assert
            Assert.IsType<MoneyAttributeMetadata>(metadata);
        }

        [Fact]
        public void CreateAttributeMetadataByType_Should_Return_IntegerAttributeMetadata_For_NullableInt()
        {
            // Act
            var metadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(int?));

            // Assert
            Assert.IsType<IntegerAttributeMetadata>(metadata);
        }

        [Fact]
        public void CreateAttributeMetadataByType_Should_Return_DoubleAttributeMetadata_For_NullableDouble()
        {
            // Act
            var metadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(double?));

            // Assert
            Assert.IsType<DoubleAttributeMetadata>(metadata);
        }

        [Fact]
        public void CreateAttributeMetadataByType_Should_Return_BooleanAttributeMetadata_For_NullableBool()
        {
            // Act
            var metadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(bool?));

            // Assert
            Assert.IsType<BooleanAttributeMetadata>(metadata);
        }

        [Fact]
        public void CreateAttributeMetadataByType_Should_Return_DecimalAttributeMetadata_For_NullableDecimal()
        {
            // Act
            var metadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(decimal?));

            // Assert
            Assert.IsType<DecimalAttributeMetadata>(metadata);
        }

        [Fact]
        public void CreateAttributeMetadataByType_Should_Return_DateTimeAttributeMetadata_For_NullableDateTime()
        {
            // Act
            var metadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(DateTime?));

            // Assert
            Assert.IsType<DateTimeAttributeMetadata>(metadata);
        }

        [Fact]
        public void CreateAttributeMetadataByType_Should_Return_BigIntAttributeMetadata_For_NullableLong()
        {
            // Act
            var metadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(long?));

            // Assert
            Assert.IsType<BigIntAttributeMetadata>(metadata);
        }

        [Fact]
        public void CreateAttributeMetadataByType_Should_Throw_For_Unmapped_Type()
        {
            // Arrange - use a type that's not mapped
            var unmappedType = typeof(System.Collections.ArrayList);

            // Act & Assert
            Assert.Throws<Exception>(() => MetadataGenerator.CreateAttributeMetadataByType(unmappedType));
        }

        #endregion

        #region GetCustomAttribute Tests

        [Fact]
        public void GetCustomAttribute_Should_Return_EntityLogicalNameAttribute()
        {
            // Arrange
            var accountType = typeof(Crm.Account);

            // Act
            var attribute = MetadataGenerator.GetCustomAttribute<Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute>(accountType);

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal("account", attribute.LogicalName);
        }

        [Fact]
        public void GetCustomAttribute_Should_Return_Null_For_Missing_Attribute()
        {
            // Arrange
            var stringType = typeof(string);

            // Act
            var attribute = MetadataGenerator.GetCustomAttribute<Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute>(stringType);

            // Assert
            Assert.Null(attribute);
        }

        #endregion

        #region Integration with XrmFakedContext Tests

        [Fact]
        public void Generated_Metadata_Should_Work_With_XrmFakedContext()
        {
            // Arrange
            var accountMetadata = MetadataGenerator.FromEarlyBoundEntity<Crm.Account>();
            var context = new XrmFakedContext();

            // Act
            context.InitializeMetadata(accountMetadata);
            var retrievedMetadata = context.GetEntityMetadataByName("account");

            // Assert
            Assert.NotNull(retrievedMetadata);
            Assert.Equal("account", retrievedMetadata.LogicalName);
        }

        [Fact]
        public void Generated_Metadata_From_Assembly_Should_Work_With_XrmFakedContext()
        {
            // Arrange
            var metadata = MetadataGenerator.FromEarlyBoundEntities(typeof(Crm.Account).Assembly);
            var context = new XrmFakedContext();

            // Act
            context.InitializeMetadata(metadata);
            var accountMetadata = context.GetEntityMetadataByName("account");

            // Assert
            Assert.NotNull(accountMetadata);
            Assert.Equal("accountid", accountMetadata.PrimaryIdAttribute);
        }

        #endregion

        #region IsPrimaryName Preservation Tests (GitHub Issue #505)

        /// <summary>
        /// Tests that IsPrimaryName property is preserved when initializing metadata.
        /// GitHub Issue #505: IsPrimaryName unpopulated in RetrieveEntityRequest
        /// </summary>
        [Fact]
        public void InitializeMetadata_Should_Preserve_IsPrimaryName_Property()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entityMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };

            var nameAttribute = new StringAttributeMetadata
            {
                LogicalName = "name",
                SchemaName = "Name"
            };
            // Set IsPrimaryName to true using reflection (it's a sealed property)
            nameAttribute.SetSealedPropertyValue("IsPrimaryName", true);

            var otherAttribute = new StringAttributeMetadata
            {
                LogicalName = "description",
                SchemaName = "Description"
            };
            // Explicitly set IsPrimaryName to false
            otherAttribute.SetSealedPropertyValue("IsPrimaryName", false);

            entityMetadata.SetAttributeCollection(new AttributeMetadata[] { nameAttribute, otherAttribute });

            // Act
            context.InitializeMetadata(entityMetadata);
            var retrievedMetadata = context.GetEntityMetadataByName("account");

            // Assert
            Assert.NotNull(retrievedMetadata);
            Assert.NotNull(retrievedMetadata.Attributes);

            var retrievedNameAttribute = retrievedMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "name");
            var retrievedOtherAttribute = retrievedMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "description");

            Assert.NotNull(retrievedNameAttribute);
            Assert.NotNull(retrievedOtherAttribute);

            // The IsPrimaryName property should be preserved
            Assert.True(retrievedNameAttribute.IsPrimaryName, "IsPrimaryName should be true for the 'name' attribute");
            Assert.False(retrievedOtherAttribute.IsPrimaryName, "IsPrimaryName should be false for the 'description' attribute");
        }

        /// <summary>
        /// Tests that IsPrimaryName property is returned correctly via RetrieveEntityRequest.
        /// This is the exact scenario described in GitHub Issue #505.
        /// </summary>
        [Fact]
        public void RetrieveEntityRequest_Should_Return_IsPrimaryName_Property()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata
            {
                LogicalName = "contact"
            };

            // Create an attribute with IsPrimaryName = true
            var fullnameAttribute = new StringAttributeMetadata
            {
                LogicalName = "fullname",
                SchemaName = "FullName",
                MaxLength = 500
            };
            fullnameAttribute.SetSealedPropertyValue("IsPrimaryName", true);

            // Create an attribute with IsPrimaryName = false
            var emailAttribute = new StringAttributeMetadata
            {
                LogicalName = "emailaddress1",
                SchemaName = "EMailAddress1",
                MaxLength = 100
            };
            emailAttribute.SetSealedPropertyValue("IsPrimaryName", false);

            // Create an attribute without explicitly setting IsPrimaryName (should default to false/null)
            var phoneAttribute = new StringAttributeMetadata
            {
                LogicalName = "telephone1",
                SchemaName = "Telephone1"
            };

            entityMetadata.SetAttributeCollection(new AttributeMetadata[] { fullnameAttribute, emailAttribute, phoneAttribute });

            context.InitializeMetadata(entityMetadata);

            // Act - Use RetrieveEntityRequest to get the metadata
            var request = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = "contact"
            };
            var response = (RetrieveEntityResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.EntityMetadata);
            Assert.NotNull(response.EntityMetadata.Attributes);

            var retrievedFullnameAttr = response.EntityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "fullname");
            var retrievedEmailAttr = response.EntityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "emailaddress1");
            var retrievedPhoneAttr = response.EntityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "telephone1");

            Assert.NotNull(retrievedFullnameAttr);
            Assert.NotNull(retrievedEmailAttr);
            Assert.NotNull(retrievedPhoneAttr);

            // Verify IsPrimaryName values are preserved
            Assert.True(retrievedFullnameAttr.IsPrimaryName, "IsPrimaryName should be true for 'fullname' attribute after RetrieveEntityRequest");
            Assert.False(retrievedEmailAttr.IsPrimaryName, "IsPrimaryName should be false for 'emailaddress1' attribute after RetrieveEntityRequest");
            // Note: IsPrimaryName defaults to null/false when not explicitly set
            Assert.True(retrievedPhoneAttr.IsPrimaryName == null || retrievedPhoneAttr.IsPrimaryName == false,
                "IsPrimaryName should be null or false for 'telephone1' attribute (default value)");
        }

        /// <summary>
        /// Tests that multiple attributes can be queried for IsPrimaryName property.
        /// Simulates the scenario from GitHub Issue #505 where user loops through all attribute metadata.
        /// </summary>
        [Fact]
        public void RetrieveEntityRequest_Should_Allow_Querying_IsPrimaryName_On_All_Attributes()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata
            {
                LogicalName = "opportunity"
            };

            // Create multiple attributes with various IsPrimaryName values
            var attributes = new List<AttributeMetadata>();

            var nameAttribute = new StringAttributeMetadata { LogicalName = "name" };
            nameAttribute.SetSealedPropertyValue("IsPrimaryName", true);
            attributes.Add(nameAttribute);

            for (int i = 1; i <= 5; i++)
            {
                var attr = new StringAttributeMetadata { LogicalName = $"field{i}" };
                attr.SetSealedPropertyValue("IsPrimaryName", false);
                attributes.Add(attr);
            }

            entityMetadata.SetAttributeCollection(attributes.ToArray());
            context.InitializeMetadata(entityMetadata);

            // Act
            var request = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = "opportunity"
            };
            var response = (RetrieveEntityResponse)service.Execute(request);

            // Assert - Query all attributes for IsPrimaryName (the scenario from the issue)
            var primaryNameAttributes = response.EntityMetadata.Attributes
                .Where(a => a.IsPrimaryName == true)
                .ToList();

            Assert.Single(primaryNameAttributes);
            Assert.Equal("name", primaryNameAttributes[0].LogicalName);
        }

        /// <summary>
        /// Tests that IsPrimaryName property is preserved when SetEntityMetadata is used.
        /// </summary>
        [Fact]
        public void SetEntityMetadata_Should_Preserve_IsPrimaryName_Property()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entityMetadata = new EntityMetadata
            {
                LogicalName = "lead"
            };

            var subjectAttribute = new StringAttributeMetadata
            {
                LogicalName = "subject",
                SchemaName = "Subject"
            };
            subjectAttribute.SetSealedPropertyValue("IsPrimaryName", true);

            entityMetadata.SetAttributeCollection(new AttributeMetadata[] { subjectAttribute });

            // Act
            context.SetEntityMetadata(entityMetadata);
            var retrievedMetadata = context.GetEntityMetadataByName("lead");

            // Assert
            Assert.NotNull(retrievedMetadata);
            var retrievedSubjectAttr = retrievedMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "subject");
            Assert.NotNull(retrievedSubjectAttr);
            Assert.True(retrievedSubjectAttr.IsPrimaryName, "IsPrimaryName should be preserved when using SetEntityMetadata");
        }

        #endregion
    }
}
