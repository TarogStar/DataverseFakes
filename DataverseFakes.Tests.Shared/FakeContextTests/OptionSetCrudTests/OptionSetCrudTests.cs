using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.ServiceModel;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.OptionSetCrudTests
{
    /// <summary>
    /// Comprehensive tests for OptionSet CRUD operations (CreateOptionSetRequest,
    /// UpdateOptionSetRequest, DeleteOptionSetRequest).
    /// </summary>
    public class OptionSetCrudTests
    {
        #region CreateOptionSetRequest Tests

        [Fact]
        public void CreateGlobalOptionSet_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata
            {
                Name = "new_teststatus",
                DisplayName = new Label("Test Status", 1033),
                Description = new Label("A test global option set", 1033),
                IsGlobal = true,
                OptionSetType = OptionSetType.Picklist
            };

            var request = new CreateOptionSetRequest
            {
                OptionSet = optionSet
            };

            // Act
            var response = (CreateOptionSetResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var metadataId = (Guid)response.Results["MetadataId"];
            Assert.NotEqual(Guid.Empty, metadataId);
            Assert.True(context.OptionSetValuesMetadata.ContainsKey("new_teststatus"));
            Assert.Equal("new_teststatus", context.OptionSetValuesMetadata["new_teststatus"].Name);
        }

        [Fact]
        public void CreateOptionSetWithOptions_ShouldStoreOptions()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata
            {
                Name = "new_priority",
                DisplayName = new Label("Priority", 1033)
            };

            optionSet.Options.Add(new OptionMetadata(new Label("Low", 1033), 1));
            optionSet.Options.Add(new OptionMetadata(new Label("Medium", 1033), 2));
            optionSet.Options.Add(new OptionMetadata(new Label("High", 1033), 3));

            var request = new CreateOptionSetRequest
            {
                OptionSet = optionSet
            };

            // Act
            var response = (CreateOptionSetResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.True(context.OptionSetValuesMetadata.ContainsKey("new_priority"));

            var storedOptionSet = context.OptionSetValuesMetadata["new_priority"];
            Assert.Equal(3, storedOptionSet.Options.Count);
            Assert.Equal("Low", storedOptionSet.Options[0].Label.LocalizedLabels[0].Label);
            Assert.Equal(1, storedOptionSet.Options[0].Value);
            Assert.Equal("Medium", storedOptionSet.Options[1].Label.LocalizedLabels[0].Label);
            Assert.Equal(2, storedOptionSet.Options[1].Value);
            Assert.Equal("High", storedOptionSet.Options[2].Label.LocalizedLabels[0].Label);
            Assert.Equal(3, storedOptionSet.Options[2].Value);
        }

        [Fact]
        public void CreateDuplicateOptionSet_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata { Name = "new_existingoptionset" };
            context.OptionSetValuesMetadata.Add("new_existingoptionset", optionSet);

            var request = new CreateOptionSetRequest
            {
                OptionSet = new OptionSetMetadata { Name = "new_existingoptionset" }
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void CreateOptionSetWithNullName_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateOptionSetRequest
            {
                OptionSet = new OptionSetMetadata { Name = null }
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void CreateOptionSetWithEmptyName_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateOptionSetRequest
            {
                OptionSet = new OptionSetMetadata { Name = "" }
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void CreateOptionSetWithNullOptionSet_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateOptionSetRequest
            {
                OptionSet = null
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Theory]
        [InlineData("new_simpleoptionset")]
        [InlineData("prefix_optionsetname")]
        [InlineData("new_with_underscores_123")]
        public void CreateOptionSetWithVariousNames_ShouldSucceed(string optionSetName)
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateOptionSetRequest
            {
                OptionSet = new OptionSetMetadata { Name = optionSetName }
            };

            // Act
            var response = (CreateOptionSetResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.True(context.OptionSetValuesMetadata.ContainsKey(optionSetName));
        }

        #endregion

        #region UpdateOptionSetRequest Tests

        [Fact]
        public void UpdateOptionSetDisplayName_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var existingOptionSet = new OptionSetMetadata
            {
                Name = "new_status",
                DisplayName = new Label("Old Display Name", 1033)
            };
            context.OptionSetValuesMetadata.Add("new_status", existingOptionSet);

            var updatedOptionSet = new OptionSetMetadata
            {
                Name = "new_status",
                DisplayName = new Label("New Display Name", 1033)
            };

            var request = new UpdateOptionSetRequest
            {
                OptionSet = updatedOptionSet
            };

            // Act
            var response = (UpdateOptionSetResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var retrievedOptionSet = context.OptionSetValuesMetadata["new_status"];
            Assert.Equal("New Display Name", retrievedOptionSet.DisplayName.LocalizedLabels[0].Label);
        }

        [Fact]
        public void UpdateNonExistentOptionSet_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateOptionSetRequest
            {
                OptionSet = new OptionSetMetadata
                {
                    Name = "nonexistent_optionset",
                    DisplayName = new Label("Some Name", 1033)
                }
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateOptionSetOptions_ShouldMergeOptions()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var existingOptionSet = new OptionSetMetadata { Name = "new_category" };
            existingOptionSet.Options.Add(new OptionMetadata(new Label("Original Option", 1033), 1));
            context.OptionSetValuesMetadata.Add("new_category", existingOptionSet);

            var updatedOptionSet = new OptionSetMetadata { Name = "new_category" };
            updatedOptionSet.Options.Add(new OptionMetadata(new Label("New Option A", 1033), 10));
            updatedOptionSet.Options.Add(new OptionMetadata(new Label("New Option B", 1033), 20));

            var request = new UpdateOptionSetRequest
            {
                OptionSet = updatedOptionSet
            };

            // Act
            var response = (UpdateOptionSetResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var retrievedOptionSet = context.OptionSetValuesMetadata["new_category"];
            Assert.Equal(2, retrievedOptionSet.Options.Count);
            Assert.Equal("New Option A", retrievedOptionSet.Options[0].Label.LocalizedLabels[0].Label);
            Assert.Equal(10, retrievedOptionSet.Options[0].Value);
            Assert.Equal("New Option B", retrievedOptionSet.Options[1].Label.LocalizedLabels[0].Label);
            Assert.Equal(20, retrievedOptionSet.Options[1].Value);
        }

        [Fact]
        public void UpdateOptionSetDescription_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var existingOptionSet = new OptionSetMetadata
            {
                Name = "new_type",
                Description = new Label("Old Description", 1033)
            };
            context.OptionSetValuesMetadata.Add("new_type", existingOptionSet);

            var updatedOptionSet = new OptionSetMetadata
            {
                Name = "new_type",
                Description = new Label("New Description", 1033)
            };

            var request = new UpdateOptionSetRequest
            {
                OptionSet = updatedOptionSet
            };

            // Act
            var response = (UpdateOptionSetResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var retrievedOptionSet = context.OptionSetValuesMetadata["new_type"];
            Assert.Equal("New Description", retrievedOptionSet.Description.LocalizedLabels[0].Label);
        }

        [Fact]
        public void UpdateOptionSetWithNullOptionSet_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateOptionSetRequest
            {
                OptionSet = null
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateOptionSetWithEmptyName_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateOptionSetRequest
            {
                OptionSet = new OptionSetMetadata { Name = "" }
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateOptionSetPreservesUnchangedProperties()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var existingOptionSet = new OptionSetMetadata
            {
                Name = "new_keepprops",
                DisplayName = new Label("Original Display Name", 1033),
                Description = new Label("Original Description", 1033)
            };
            existingOptionSet.Options.Add(new OptionMetadata(new Label("Option 1", 1033), 1));
            context.OptionSetValuesMetadata.Add("new_keepprops", existingOptionSet);

            // Only update DisplayName, leave Description and Options unchanged
            var updatedOptionSet = new OptionSetMetadata
            {
                Name = "new_keepprops",
                DisplayName = new Label("Updated Display Name", 1033)
            };

            var request = new UpdateOptionSetRequest
            {
                OptionSet = updatedOptionSet
            };

            // Act
            service.Execute(request);

            // Assert
            var retrievedOptionSet = context.OptionSetValuesMetadata["new_keepprops"];
            Assert.Equal("Updated Display Name", retrievedOptionSet.DisplayName.LocalizedLabels[0].Label);
            Assert.Equal("Original Description", retrievedOptionSet.Description.LocalizedLabels[0].Label);
            Assert.Single(retrievedOptionSet.Options);
            Assert.Equal("Option 1", retrievedOptionSet.Options[0].Label.LocalizedLabels[0].Label);
        }

        #endregion

        #region DeleteOptionSetRequest Tests

        [Fact]
        public void DeleteOptionSet_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata { Name = "new_tobedeleted" };
            context.OptionSetValuesMetadata.Add("new_tobedeleted", optionSet);

            var request = new DeleteOptionSetRequest
            {
                Name = "new_tobedeleted"
            };

            // Act
            var response = (DeleteOptionSetResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.False(context.OptionSetValuesMetadata.ContainsKey("new_tobedeleted"));
        }

        [Fact]
        public void DeleteNonExistentOptionSet_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new DeleteOptionSetRequest
            {
                Name = "nonexistent_optionset"
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void VerifyDeletedOptionSetNotRetrievable()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata { Name = "new_deletedoptionset" };
            context.OptionSetValuesMetadata.Add("new_deletedoptionset", optionSet);

            // First, delete the option set
            var deleteRequest = new DeleteOptionSetRequest
            {
                Name = "new_deletedoptionset"
            };
            service.Execute(deleteRequest);

            // Now, try to retrieve it
            var retrieveRequest = new RetrieveOptionSetRequest
            {
                Name = "new_deletedoptionset"
            };

            // Act & Assert - Should throw because the option set no longer exists
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(retrieveRequest));
        }

        [Fact]
        public void DeleteOptionSetWithNullName_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new DeleteOptionSetRequest
            {
                Name = null
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void DeleteOptionSetWithEmptyName_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new DeleteOptionSetRequest
            {
                Name = ""
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void DeleteOptionSetDoesNotAffectOtherOptionSets()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet1 = new OptionSetMetadata { Name = "new_optionset1" };
            var optionSet2 = new OptionSetMetadata { Name = "new_optionset2" };
            var optionSet3 = new OptionSetMetadata { Name = "new_optionset3" };

            context.OptionSetValuesMetadata.Add("new_optionset1", optionSet1);
            context.OptionSetValuesMetadata.Add("new_optionset2", optionSet2);
            context.OptionSetValuesMetadata.Add("new_optionset3", optionSet3);

            // Act - Delete optionset2
            var request = new DeleteOptionSetRequest { Name = "new_optionset2" };
            service.Execute(request);

            // Assert - Other option sets should still exist
            Assert.True(context.OptionSetValuesMetadata.ContainsKey("new_optionset1"));
            Assert.False(context.OptionSetValuesMetadata.ContainsKey("new_optionset2"));
            Assert.True(context.OptionSetValuesMetadata.ContainsKey("new_optionset3"));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void CreateThenUpdateThenDeleteOptionSet_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create
            var createRequest = new CreateOptionSetRequest
            {
                OptionSet = new OptionSetMetadata
                {
                    Name = "new_lifecycle",
                    DisplayName = new Label("Initial Name", 1033)
                }
            };
            var createResponse = (CreateOptionSetResponse)service.Execute(createRequest);
            Assert.NotNull(createResponse);
            Assert.True(context.OptionSetValuesMetadata.ContainsKey("new_lifecycle"));

            // Update
            var updateRequest = new UpdateOptionSetRequest
            {
                OptionSet = new OptionSetMetadata
                {
                    Name = "new_lifecycle",
                    DisplayName = new Label("Updated Name", 1033)
                }
            };
            var updateResponse = (UpdateOptionSetResponse)service.Execute(updateRequest);
            Assert.NotNull(updateResponse);
            Assert.Equal("Updated Name", context.OptionSetValuesMetadata["new_lifecycle"].DisplayName.LocalizedLabels[0].Label);

            // Delete
            var deleteRequest = new DeleteOptionSetRequest
            {
                Name = "new_lifecycle"
            };
            var deleteResponse = (DeleteOptionSetResponse)service.Execute(deleteRequest);
            Assert.NotNull(deleteResponse);
            Assert.False(context.OptionSetValuesMetadata.ContainsKey("new_lifecycle"));
        }

        [Fact]
        public void CreateAndRetrieveOptionSet_ShouldReturnSameData()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata
            {
                Name = "new_retrievable",
                DisplayName = new Label("Retrievable Option Set", 1033),
                Description = new Label("This option set can be retrieved", 1033)
            };
            optionSet.Options.Add(new OptionMetadata(new Label("Value A", 1033), 100));
            optionSet.Options.Add(new OptionMetadata(new Label("Value B", 1033), 200));

            var createRequest = new CreateOptionSetRequest { OptionSet = optionSet };
            service.Execute(createRequest);

            // Act
            var retrieveRequest = new RetrieveOptionSetRequest { Name = "new_retrievable" };
            var retrieveResponse = (RetrieveOptionSetResponse)service.Execute(retrieveRequest);

            // Assert
            var retrievedOptionSet = (OptionSetMetadata)retrieveResponse.OptionSetMetadata;
            Assert.Equal("new_retrievable", retrievedOptionSet.Name);
            Assert.Equal("Retrievable Option Set", retrievedOptionSet.DisplayName.LocalizedLabels[0].Label);
            Assert.Equal("This option set can be retrieved", retrievedOptionSet.Description.LocalizedLabels[0].Label);
            Assert.Equal(2, retrievedOptionSet.Options.Count);
            Assert.Equal("Value A", retrievedOptionSet.Options[0].Label.LocalizedLabels[0].Label);
            Assert.Equal(100, retrievedOptionSet.Options[0].Value);
            Assert.Equal("Value B", retrievedOptionSet.Options[1].Label.LocalizedLabels[0].Label);
            Assert.Equal(200, retrievedOptionSet.Options[1].Value);
        }

        #endregion
    }
}
