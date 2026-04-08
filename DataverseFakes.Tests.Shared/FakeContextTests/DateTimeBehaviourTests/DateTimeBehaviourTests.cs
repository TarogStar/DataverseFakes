#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013

using Crm;
using FakeXrmEasy.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests
{
    public class DateTimeBehaviourTests
    {
        [Fact]
        public void When_RetrieveMultiple_with_DateTime_Field_Behaviour_set_to_DateOnly_result_is_Time_Part_is_Zero()
        {
            var ctx = new XrmFakedContext
            {
                DateBehaviour = new Dictionary<string, Dictionary<string, DateTimeAttributeBehavior>>
                {
                    {
                        "contact", new Dictionary<string, DateTimeAttributeBehavior>
                        {
                            { "birthdate", DateTimeAttributeBehavior.DateOnly }
                        }
                    }
                }
            };

            ctx.Initialize(new List<Entity>
            {
                new Contact
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc),
                    BirthDate = new DateTime(2000, 1, 1, 23, 0, 0, DateTimeKind.Utc)
                }
            });

            var service = ctx.GetOrganizationService();

            var query = new QueryExpression(Contact.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("createdon", "birthdate")
            };

            var entity = service.RetrieveMultiple(query).Entities.Cast<Contact>().First();

            Assert.Equal(new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc), entity.CreatedOn);
            // DateOnly fields should return DateTimeKind.Unspecified to match Dataverse behavior
            Assert.Equal(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), entity.BirthDate);
            Assert.Equal(DateTimeKind.Unspecified, entity.BirthDate.Value.Kind);
        }

        [Fact]
        public void When_RetrieveMultiple_with_DateTime_Field_Behaviour_set_to_UserLocal_result_is_Time_Part_is_Kept()
        {
            var ctx = new XrmFakedContext
            {
                DateBehaviour = new Dictionary<string, Dictionary<string, DateTimeAttributeBehavior>>
                {
                    {
                        "contact", new Dictionary<string, DateTimeAttributeBehavior>
                        {
                            { "birthdate", DateTimeAttributeBehavior.UserLocal }
                        }
                    }
                }
            };

            ctx.Initialize(new List<Entity>
            {
                new Contact
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc),
                    BirthDate = new DateTime(2000, 1, 1, 23, 0, 0, DateTimeKind.Utc)
                }
            });

            var service = ctx.GetOrganizationService();

            var query = new QueryExpression(Contact.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("createdon", "birthdate")
            };

            var entity = service.RetrieveMultiple(query).Entities.Cast<Contact>().First();

            Assert.Equal(new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc), entity.CreatedOn);
            // UserLocal fields should return DateTimeKind.Utc
            Assert.Equal(new DateTime(2000, 1, 1, 23, 0, 0, DateTimeKind.Utc), entity.BirthDate);
            Assert.Equal(DateTimeKind.Utc, entity.BirthDate.Value.Kind);
        }

        [Fact]
        public void When_Retrieve_with_DateTime_Field_Behaviour_set_to_DateOnly_result_is_Time_Part_is_Zero()
        {
            var ctx = new XrmFakedContext
            {
                DateBehaviour = new Dictionary<string, Dictionary<string, DateTimeAttributeBehavior>>
                {
                    {
                        "contact", new Dictionary<string, DateTimeAttributeBehavior>
                        {
                            { "birthdate", DateTimeAttributeBehavior.DateOnly }
                        }
                    }
                }
            };

            var id = Guid.NewGuid();

            ctx.Initialize(new List<Entity>
            {
                new Contact
                {
                    Id = id,
                    ["createdon"] = new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc),
                    BirthDate = new DateTime(2000, 1, 1, 23, 0, 0, DateTimeKind.Utc)
                }
            });

            var service = ctx.GetOrganizationService();

            var entity = service.Retrieve("contact", id, new ColumnSet("createdon", "birthdate")).ToEntity<Contact>();

            Assert.Equal(new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc), entity.CreatedOn);
            // DateOnly fields should return DateTimeKind.Unspecified to match Dataverse behavior
            Assert.Equal(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), entity.BirthDate);
            Assert.Equal(DateTimeKind.Unspecified, entity.BirthDate.Value.Kind);
        }

        [Fact]
        public void When_Retrieve_with_DateTime_Field_Behaviour_set_to_UserLocal_result_is_Time_Part_is_Kept()
        {
            var ctx = new XrmFakedContext
            {
                DateBehaviour = new Dictionary<string, Dictionary<string, DateTimeAttributeBehavior>>
                {
                    {
                        "contact", new Dictionary<string, DateTimeAttributeBehavior>
                        {
                            { "birthdate", DateTimeAttributeBehavior.UserLocal }
                        }
                    }
                }
            };

            var id = Guid.NewGuid();

            ctx.Initialize(new List<Entity>
            {
                new Contact
                {
                    Id = id,
                    ["createdon"] = new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc),
                    BirthDate = new DateTime(2000, 1, 1, 23, 0, 0, DateTimeKind.Utc)
                }
            });

            var service = ctx.GetOrganizationService();

            var entity = service.Retrieve("contact", id, new ColumnSet("createdon", "birthdate")).ToEntity<Contact>();

            Assert.Equal(new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc), entity.CreatedOn);
            // UserLocal fields should return DateTimeKind.Utc
            Assert.Equal(new DateTime(2000, 1, 1, 23, 0, 0, DateTimeKind.Utc), entity.BirthDate);
            Assert.Equal(DateTimeKind.Utc, entity.BirthDate.Value.Kind);
        }

        [Fact]
        public void When_Retrieve_with_DateTime_Field_Behaviour_set_to_TimeZoneIndependent_result_has_Unspecified_Kind()
        {
            var ctx = new XrmFakedContext
            {
                DateBehaviour = new Dictionary<string, Dictionary<string, DateTimeAttributeBehavior>>
                {
                    {
                        "contact", new Dictionary<string, DateTimeAttributeBehavior>
                        {
                            { "birthdate", DateTimeAttributeBehavior.TimeZoneIndependent }
                        }
                    }
                }
            };

            var id = Guid.NewGuid();

            ctx.Initialize(new List<Entity>
            {
                new Contact
                {
                    Id = id,
                    ["createdon"] = new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc),
                    BirthDate = new DateTime(2000, 1, 1, 14, 30, 0, DateTimeKind.Utc)
                }
            });

            var service = ctx.GetOrganizationService();

            var entity = service.Retrieve("contact", id, new ColumnSet("createdon", "birthdate")).ToEntity<Contact>();

            Assert.Equal(new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc), entity.CreatedOn);
            // TimeZoneIndependent fields should return DateTimeKind.Unspecified to match Dataverse behavior
            Assert.Equal(new DateTime(2000, 1, 1, 14, 30, 0, DateTimeKind.Unspecified), entity.BirthDate);
            Assert.Equal(DateTimeKind.Unspecified, entity.BirthDate.Value.Kind);
        }

        [Fact]
        public void When_RetrieveMultiple_with_DateTime_Field_Behaviour_set_to_TimeZoneIndependent_result_has_Unspecified_Kind()
        {
            var ctx = new XrmFakedContext
            {
                DateBehaviour = new Dictionary<string, Dictionary<string, DateTimeAttributeBehavior>>
                {
                    {
                        "contact", new Dictionary<string, DateTimeAttributeBehavior>
                        {
                            { "birthdate", DateTimeAttributeBehavior.TimeZoneIndependent }
                        }
                    }
                }
            };

            ctx.Initialize(new List<Entity>
            {
                new Contact
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc),
                    BirthDate = new DateTime(2000, 1, 1, 14, 30, 0, DateTimeKind.Utc)
                }
            });

            var service = ctx.GetOrganizationService();

            var query = new QueryExpression(Contact.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("createdon", "birthdate")
            };

            var entity = service.RetrieveMultiple(query).Entities.Cast<Contact>().First();

            Assert.Equal(new DateTime(2017, 1, 1, 1, 0, 0, DateTimeKind.Utc), entity.CreatedOn);
            // TimeZoneIndependent fields should return DateTimeKind.Unspecified to match Dataverse behavior
            Assert.Equal(new DateTime(2000, 1, 1, 14, 30, 0, DateTimeKind.Unspecified), entity.BirthDate);
            Assert.Equal(DateTimeKind.Unspecified, entity.BirthDate.Value.Kind);
        }
    }
}

#endif