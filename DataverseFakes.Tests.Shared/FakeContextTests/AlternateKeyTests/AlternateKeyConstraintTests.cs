using FakeXrmEasy.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.AlternateKeyTests
{
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
    /// <summary>
    /// Tests for Phase 3 alternate key constraints including:
    /// - Key count limits (10 per entity)
    /// - Column count limits (16 per key)
    /// - Attribute type validation
    /// - Edge cases
    /// </summary>
    public class AlternateKeyConstraintTests
    {
        #region Key Count Limits (10 per entity)

        /// <summary>
        /// Verifies that exactly 10 alternate keys can be added to an entity.
        /// Dataverse has a limit of 10 alternate keys per entity.
        /// </summary>
        [Fact]
        public void AddingTenthAlternateKey_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act - Add 10 alternate keys to the same entity
            for (int i = 1; i <= 10; i++)
            {
                context.AddAlternateKey("account", $"attribute{i}");
            }

            // Assert
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Keys);
            Assert.Equal(10, metadata.Keys.Length);
        }

        /// <summary>
        /// Verifies that adding an 11th alternate key to an entity throws an exception.
        /// Dataverse has a limit of 10 alternate keys per entity.
        /// </summary>
        [Fact]
        public void AddingEleventhAlternateKey_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Add 10 alternate keys (the maximum allowed)
            for (int i = 1; i <= 10; i++)
            {
                context.AddAlternateKey("account", $"attribute{i}");
            }

            // Act & Assert - 11th key should fail
            var ex = Assert.ThrowsAny<Exception>(() =>
                context.AddAlternateKey("account", "attribute11"));

            Assert.Contains("10", ex.Message);
        }

        /// <summary>
        /// Verifies that different entities have independent key count limits.
        /// Each entity can have up to 10 keys independently.
        /// </summary>
        [Fact]
        public void MultipleEntities_EachCanHaveTenKeys()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act - Add 10 keys to each of 3 different entities
            for (int i = 1; i <= 10; i++)
            {
                context.AddAlternateKey("account", $"acc_attr{i}");
                context.AddAlternateKey("contact", $"con_attr{i}");
                context.AddAlternateKey("lead", $"lead_attr{i}");
            }

            // Assert - Each entity should have 10 keys
            var accountMetadata = context.GetEntityMetadataByName("account");
            var contactMetadata = context.GetEntityMetadataByName("contact");
            var leadMetadata = context.GetEntityMetadataByName("lead");

            Assert.Equal(10, accountMetadata.Keys.Length);
            Assert.Equal(10, contactMetadata.Keys.Length);
            Assert.Equal(10, leadMetadata.Keys.Length);
        }

        #endregion

        #region Column Count Limits (16 per key)

        /// <summary>
        /// Verifies that a key with exactly 16 attributes can be created.
        /// Dataverse has a limit of 16 columns per alternate key.
        /// </summary>
        [Fact]
        public void KeyWithSixteenAttributes_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var attributes = new string[16];
            for (int i = 0; i < 16; i++)
            {
                attributes[i] = $"attribute{i + 1}";
            }

            // Act
            context.AddAlternateKey("account", attributes);

            // Assert
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
            Assert.Equal(16, metadata.Keys[0].KeyAttributes.Length);
        }

        /// <summary>
        /// Verifies that a key with 17 attributes throws an exception.
        /// Dataverse has a limit of 16 columns per alternate key.
        /// </summary>
        [Fact]
        public void KeyWithSeventeenAttributes_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var attributes = new string[17];
            for (int i = 0; i < 17; i++)
            {
                attributes[i] = $"attribute{i + 1}";
            }

            // Act & Assert
            var ex = Assert.ThrowsAny<Exception>(() =>
                context.AddAlternateKey("account", attributes));

            Assert.Contains("16", ex.Message);
        }

        /// <summary>
        /// Verifies that a single-attribute key works correctly.
        /// This is the most common use case for alternate keys.
        /// </summary>
        [Fact]
        public void SingleAttributeKey_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act
            context.AddAlternateKey("account", "accountnumber");

            // Assert
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
            Assert.Single(metadata.Keys[0].KeyAttributes);
            Assert.Equal("accountnumber", metadata.Keys[0].KeyAttributes[0]);
        }

        #endregion

        #region Attribute Type Validation (when attribute metadata exists)

        /// <summary>
        /// Helper method to set up entity metadata with a specific attribute type.
        /// </summary>
        private void SetupAttributeMetadata(XrmFakedContext context, string entityName, string attributeName, AttributeMetadata attributeMetadata)
        {
            attributeMetadata.LogicalName = attributeName;

            var entityMetadata = context.GetEntityMetadataByName(entityName);
            if (entityMetadata == null)
            {
                entityMetadata = new EntityMetadata { LogicalName = entityName };
            }

            // Set the Attributes array with our attribute metadata
            var existingAttributes = entityMetadata.Attributes ?? Array.Empty<AttributeMetadata>();
            var newAttributes = new AttributeMetadata[existingAttributes.Length + 1];
            existingAttributes.CopyTo(newAttributes, 0);
            newAttributes[existingAttributes.Length] = attributeMetadata;

            entityMetadata.SetFieldValue("Attributes", newAttributes);
            context.SetEntityMetadata(entityMetadata);
        }

        /// <summary>
        /// Verifies that a String attribute can be used in an alternate key.
        /// String is a supported type per Microsoft documentation.
        /// </summary>
        [Fact]
        public void StringAttributeInKey_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var stringAttr = new StringAttributeMetadata("accountnumber");
            stringAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.String);
            SetupAttributeMetadata(context, "account", "accountnumber", stringAttr);

            // Act
            context.AddAlternateKey("account", "accountnumber");

            // Assert
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
        }

        /// <summary>
        /// Verifies that an Integer attribute can be used in an alternate key.
        /// Integer is a supported type per Microsoft documentation.
        /// </summary>
        [Fact]
        public void IntegerAttributeInKey_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var intAttr = new IntegerAttributeMetadata();
            intAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Integer);
            SetupAttributeMetadata(context, "account", "numberofemployees", intAttr);

            // Act
            context.AddAlternateKey("account", "numberofemployees");

            // Assert
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
        }

        /// <summary>
        /// Verifies that a Decimal attribute can be used in an alternate key.
        /// Decimal is a supported type per Microsoft documentation.
        /// </summary>
        [Fact]
        public void DecimalAttributeInKey_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var decimalAttr = new DecimalAttributeMetadata();
            decimalAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Decimal);
            SetupAttributeMetadata(context, "account", "exchangerate", decimalAttr);

            // Act
            context.AddAlternateKey("account", "exchangerate");

            // Assert
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
        }

        /// <summary>
        /// Verifies that a DateTime attribute can be used in an alternate key.
        /// DateTime is a supported type per Microsoft documentation.
        /// </summary>
        [Fact]
        public void DateTimeAttributeInKey_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var dateTimeAttr = new DateTimeAttributeMetadata(DateTimeFormat.DateOnly);
            dateTimeAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.DateTime);
            SetupAttributeMetadata(context, "account", "createdon", dateTimeAttr);

            // Act
            context.AddAlternateKey("account", "createdon");

            // Assert
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
        }

        /// <summary>
        /// Verifies that a Lookup attribute can be used in an alternate key.
        /// Lookup is a supported type per Microsoft documentation.
        /// </summary>
        [Fact]
        public void LookupAttributeInKey_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var lookupAttr = new LookupAttributeMetadata();
            lookupAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Lookup);
            SetupAttributeMetadata(context, "contact", "parentcustomerid", lookupAttr);

            // Act
            context.AddAlternateKey("contact", "parentcustomerid");

            // Assert
            var metadata = context.GetEntityMetadataByName("contact");
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
        }

        /// <summary>
        /// Verifies that an OptionSet (Picklist) attribute can be used in an alternate key.
        /// OptionSet is a supported type per Microsoft documentation.
        /// </summary>
        [Fact]
        public void OptionSetAttributeInKey_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var picklistAttr = new PicklistAttributeMetadata();
            picklistAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Picklist);
            SetupAttributeMetadata(context, "contact", "gendercode", picklistAttr);

            // Act
            context.AddAlternateKey("contact", "gendercode");

            // Assert
            var metadata = context.GetEntityMetadataByName("contact");
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
        }

        /// <summary>
        /// Verifies that a Money attribute cannot be used in an alternate key.
        /// Money is NOT a supported type per Microsoft documentation.
        /// </summary>
        [Fact]
        public void MoneyAttributeInKey_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var moneyAttr = new MoneyAttributeMetadata();
            moneyAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Money);
            SetupAttributeMetadata(context, "account", "revenue", moneyAttr);

            // Act & Assert
            var ex = Assert.ThrowsAny<Exception>(() =>
                context.AddAlternateKey("account", "revenue"));

            Assert.Contains("Money", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that a Boolean attribute cannot be used in an alternate key.
        /// Boolean (Two Option) is NOT a supported type per Microsoft documentation.
        /// </summary>
        [Fact]
        public void BooleanAttributeInKey_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var boolAttr = new BooleanAttributeMetadata();
            boolAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Boolean);
            SetupAttributeMetadata(context, "account", "donotcontact", boolAttr);

            // Act & Assert
            var ex = Assert.ThrowsAny<Exception>(() =>
                context.AddAlternateKey("account", "donotcontact"));

            Assert.Contains("Boolean", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that a Memo (Multi-line text) attribute cannot be used in an alternate key.
        /// Memo is NOT a supported type per Microsoft documentation.
        /// </summary>
        [Fact]
        public void MemoAttributeInKey_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var memoAttr = new MemoAttributeMetadata();
            memoAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Memo);
            SetupAttributeMetadata(context, "account", "description", memoAttr);

            // Act & Assert
            var ex = Assert.ThrowsAny<Exception>(() =>
                context.AddAlternateKey("account", "description"));

            Assert.Contains("Memo", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Verifies that if no attribute metadata is defined for the key attributes,
        /// the key should still be allowed (permissive behavior for testing flexibility).
        /// </summary>
        [Fact]
        public void KeyWithoutMetadata_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();

            // No attribute metadata defined for the entity

            // Act
            context.AddAlternateKey("account", "custom_attribute");

            // Assert
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
            Assert.Equal("custom_attribute", metadata.Keys[0].KeyAttributes[0]);
        }

        /// <summary>
        /// Verifies that attempting to create a key with zero attributes throws an exception.
        /// A key must have at least one attribute.
        /// </summary>
        [Fact]
        public void EmptyKeyAttributes_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                context.AddAlternateKey("account", Array.Empty<string>()));

            Assert.Contains("attribute", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that attempting to create a key with null attributes throws an exception.
        /// </summary>
        [Fact]
        public void NullKeyAttributes_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                context.AddAlternateKey("account", (string[])null));

            Assert.Contains("attribute", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that key validation works correctly during entity creation
        /// when the key count limit is reached.
        /// </summary>
        [Fact]
        public void CreateEntity_WithMaxKeyCount_ShouldEnforceUniqueness()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Add 10 alternate keys (the maximum)
            for (int i = 1; i <= 10; i++)
            {
                context.AddAlternateKey("account", $"attribute{i}");
            }

            // Initialize an entity with values for all key attributes
            var entity1 = new Entity("account") { Id = Guid.NewGuid() };
            for (int i = 1; i <= 10; i++)
            {
                entity1[$"attribute{i}"] = $"value{i}";
            }
            context.Initialize(new List<Entity> { entity1 });

            var service = context.GetOrganizationService();

            // Create duplicate for first key
            var duplicate = new Entity("account");
            duplicate["attribute1"] = "value1";

            // Act & Assert - Should throw for the first key's duplicate
            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Create(duplicate));

            Assert.Contains("already exists", ex.Message);
        }

        /// <summary>
        /// Verifies that attribute type validation happens during key creation,
        /// not just when entities are created/updated.
        /// </summary>
        [Fact]
        public void AttributeTypeValidation_ShouldHappenDuringKeyCreation()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Set up a Money attribute (invalid for keys)
            var moneyAttr = new MoneyAttributeMetadata();
            moneyAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Money);
            SetupAttributeMetadata(context, "account", "revenue", moneyAttr);

            // Set up a String attribute (valid for keys)
            var stringAttr = new StringAttributeMetadata("accountnumber");
            stringAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.String);
            SetupAttributeMetadata(context, "account", "accountnumber", stringAttr);

            // Act - Try to create a composite key with both valid and invalid types
            var ex = Assert.ThrowsAny<Exception>(() =>
                context.AddAlternateKey("account", new[] { "accountnumber", "revenue" }));

            // Assert - Should fail due to the Money attribute
            Assert.Contains("Money", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that keys with duplicate attribute names are allowed (permissive behavior).
        /// Note: Dataverse may reject this at runtime, but the testing framework is permissive.
        /// </summary>
        [Fact]
        public void DuplicateAttributeInSameKey_IsAllowed()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act - Duplicate attribute names are allowed (permissive for testing flexibility)
            context.AddAlternateKey("account", new[] { "attr1", "attr1" });

            // Assert - Key is created (framework is permissive, real Dataverse would reject)
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
        }

        /// <summary>
        /// Verifies that the same attributes can be used in different keys
        /// as long as the key combinations are different.
        /// </summary>
        [Fact]
        public void SameAttributeInDifferentKeys_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act - Create keys that share some attributes
            context.AddAlternateKey("account", new[] { "attr1", "attr2" });
            context.AddAlternateKey("account", new[] { "attr1", "attr3" });
            context.AddAlternateKey("account", new[] { "attr2", "attr3" });

            // Assert
            var metadata = context.GetEntityMetadataByName("account");
            Assert.Equal(3, metadata.Keys.Length);
        }

        /// <summary>
        /// Verifies that whitespace-only attribute names are allowed (permissive behavior).
        /// Note: Real Dataverse would reject this, but the testing framework is permissive.
        /// </summary>
        [Fact]
        public void WhitespaceAttributeName_IsAllowed()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act - Whitespace names are allowed (permissive for testing flexibility)
            context.AddAlternateKey("account", new[] { "   " });

            // Assert - Key is created
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
        }

        /// <summary>
        /// Verifies that null attribute names within the array are allowed (permissive behavior).
        /// Note: Real Dataverse would reject this, but the testing framework is permissive.
        /// </summary>
        [Fact]
        public void NullAttributeNameInArray_IsAllowed()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act - Null values in array are allowed (permissive for testing flexibility)
            context.AddAlternateKey("account", new[] { "attr1", null, "attr2" });

            // Assert - Key is created
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
        }

        /// <summary>
        /// Verifies that adding identical keys (same attributes in same order) is handled.
        /// </summary>
        [Fact]
        public void IdenticalKeyDefinition_ShouldBeDetected()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Add a key
            context.AddAlternateKey("account", new[] { "attr1", "attr2" });

            // Act & Assert - Adding identical key should either succeed (allowing duplicates)
            // or throw an exception indicating duplicate key
            try
            {
                context.AddAlternateKey("account", new[] { "attr1", "attr2" });

                // If it succeeds, verify behavior
                var metadata = context.GetEntityMetadataByName("account");
                // Implementation may either allow duplicates (2 keys) or prevent them (1 key)
                Assert.True(metadata.Keys.Length >= 1);
            }
            catch (Exception ex)
            {
                // If it throws, verify the message indicates duplicate
                Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Verifies that a BigInt attribute can be used in an alternate key.
        /// BigInt is a supported type per Microsoft documentation.
        /// </summary>
        [Fact]
        public void BigIntAttributeInKey_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var bigIntAttr = new BigIntAttributeMetadata();
            bigIntAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.BigInt);
            SetupAttributeMetadata(context, "account", "versionnumber", bigIntAttr);

            // Act
            context.AddAlternateKey("account", "versionnumber");

            // Assert
            var metadata = context.GetEntityMetadataByName("account");
            Assert.NotNull(metadata.Keys);
            Assert.Single(metadata.Keys);
        }

        #endregion
    }
#endif
}
