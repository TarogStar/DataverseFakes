using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.TranslateQueryExpressionTests.OperatorTests.Strings
{
    /// <summary>
    /// Tests for advanced LIKE pattern matching including:
    /// - _ (single character wildcard)
    /// - [A-Z] (character ranges)
    /// - [ABC] (character sets)
    /// - [^ABC] (negated character sets)
    ///
    /// Addresses issue #509 - LIKE wildcards [X-Y] character ranges
    /// </summary>
    public class LikeAdvancedPatternTests
    {
        #region Single Character Wildcard Tests (_)

        [Fact]
        public void Like_with_single_char_wildcard_matches_test_and_text()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "test" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "text" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "tent" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "testing" }); // Should NOT match - too long

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "te_t");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(3, results.Count);
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "test");
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "text");
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "tent");
        }

        [Fact]
        public void Like_with_single_char_wildcard_is_case_insensitive()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "TEST" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Text" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "te_t");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void Like_with_multiple_single_char_wildcards()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "abc" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "xyz" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "abcd" }); // Should NOT match

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "___");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
        }

        #endregion

        #region Character Range Tests ([A-Z])

        [Fact]
        public void Like_with_digit_range_matches_numbers()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "1abc" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "9xyz" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "abc" }); // Should NOT match - no leading digit

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "[0-9]%");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "1abc");
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "9xyz");
        }

        [Fact]
        public void Like_with_letter_range_matches_uppercase_and_lowercase()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Apple" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "banana" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "123test" }); // Should NOT match - starts with digit

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "[a-z]%");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
        }

        #endregion

        #region Character Set Tests ([ABC])

        [Fact]
        public void Like_with_vowel_set_matches_vowel_prefix()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "apple" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "elephant" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "ice" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "orange" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "umbrella" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "banana" }); // Should NOT match

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "[aeiou]%");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(5, results.Count);
        }

        [Fact]
        public void Like_with_character_set_is_case_insensitive()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Apple" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "ELEPHANT" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Orange" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "[aeiou]%");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(3, results.Count);
        }

        #endregion

        #region Negated Set Tests ([^ABC])

        [Fact]
        public void Like_with_negated_digit_set_excludes_numbers()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "abc" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "xyz" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "123" }); // Should NOT match - starts with digit

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "[^0-9]%");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "abc");
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "xyz");
        }

        [Fact]
        public void Like_with_negated_vowel_set_excludes_vowels()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "banana" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "cherry" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "apple" }); // Should NOT match - starts with vowel
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "orange" }); // Should NOT match - starts with vowel

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "[^aeiou]%");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "banana");
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "cherry");
        }

        #endregion

        #region Mixed Pattern Tests

        [Fact]
        public void Like_with_uppercase_letter_and_trailing_digit_matches()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Test1" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "ABC9" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Hello5" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "1Test" }); // Should NOT match - starts with digit
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "TestA" }); // Should NOT match - ends with letter

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "[A-Z]%[0-9]");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(3, results.Count);
        }

        [Fact]
        public void Like_with_combined_underscore_and_percent()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "a1test" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "b2xyz" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "ab" }); // Matches: a=letter, b=single char, %=empty
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "a" }); // Should NOT match - only 1 char, needs at least 2
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "1atest" }); // Should NOT match - starts with digit

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "[a-z]_%");

            var results = service.RetrieveMultiple(qe).Entities;

            // Pattern [a-z]_% means: letter + any char + zero or more chars (minimum 2 chars total, starting with letter)
            Assert.Equal(3, results.Count);
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "a1test");
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "b2xyz");
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "ab");
        }

        [Fact]
        public void Like_with_exact_pattern_using_character_sets()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "A1" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Z9" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "A1B" }); // Should NOT match - too long
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "1A" }); // Should NOT match - wrong order

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "[A-Z][0-9]");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "A1");
            Assert.Contains(results, e => e.GetAttributeValue<string>("firstname") == "Z9");
        }

        #endregion

        #region Backward Compatibility Tests (% only patterns)

        [Fact]
        public void Like_with_simple_percent_still_works_startswith()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Jimmy" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "James" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Bob" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "J%");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void Like_with_simple_percent_still_works_endswith()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Jimmy" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Tommy" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "James" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "%my");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void Like_with_simple_percent_still_works_contains()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Jimmy" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "animation" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "Bob" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "%im%");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void Like_with_simple_percent_is_still_case_insensitive()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "JIMMY" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "jimmy" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "JIM%");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
        }

        #endregion

        #region NotLike Tests

        [Fact]
        public void NotLike_with_advanced_pattern_excludes_matches()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "1abc" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "abc" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "xyz" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.NotLike, "[0-9]%");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, results.Count);
            Assert.DoesNotContain(results, e => e.GetAttributeValue<string>("firstname") == "1abc");
        }

        #endregion

        #region ConvertLikePatternToRegex Unit Tests

        [Theory]
        [InlineData("te_t", "^te.t$")]
        [InlineData("%test%", "^.*test.*$")]
        [InlineData("[0-9]%", "^[0-9].*$")]
        [InlineData("[aeiou]test", "^[aeiou]test$")]
        [InlineData("[^0-9]%", "^[^0-9].*$")]
        [InlineData("[A-Z]%[0-9]", "^[A-Z].*[0-9]$")]
        [InlineData("test.value", "^test\\.value$")]
        [InlineData("test$value", "^test\\$value$")]
        [InlineData("test*value", "^test\\*value$")]
        public void ConvertLikePatternToRegex_produces_correct_patterns(string likePattern, string expectedRegex)
        {
            var result = XrmFakedContext.ConvertLikePatternToRegex(likePattern);
            Assert.Equal(expectedRegex, result);
        }

        [Fact]
        public void ConvertLikePatternToRegex_handles_empty_pattern()
        {
            var result = XrmFakedContext.ConvertLikePatternToRegex("");
            Assert.Equal("^$", result);
        }

        [Fact]
        public void ConvertLikePatternToRegex_handles_null_pattern()
        {
            var result = XrmFakedContext.ConvertLikePatternToRegex(null);
            Assert.Equal("^$", result);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Like_with_null_attribute_does_not_throw()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var contact = new Contact { Id = Guid.NewGuid() };
            contact["firstname"] = null;
            service.Create(contact);

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "test" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "te_t");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Single(results);
        }

        [Fact]
        public void Like_with_special_regex_chars_in_literal_part_works()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "test.value" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "testXvalue" }); // Should NOT match

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(true)
      };
      qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "test.%");

            var results = service.RetrieveMultiple(qe).Entities;

            Assert.Single(results);
            Assert.Equal("test.value", results[0].GetAttributeValue<string>("firstname"));
        }

        #endregion
    }
}
