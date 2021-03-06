﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal;
using Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlRewrite
{
    public class InputParserTests
    {
        [Fact]
        public void InputParser_ParseLiteralString()
        {
            var testString = "hello/hey/what";
            var result = new InputParser().ParseInputString(testString);
            Assert.Equal(1, result.PatternSegments.Count);
        }

        [Theory]
        [InlineData("foo/bar/{R:1}/what", 3)]
        [InlineData("foo/{R:1}", 2)]
        [InlineData("foo/{R:1}/{C:2}", 4)]
        [InlineData("foo/{R:1}{C:2}", 3)]
        [InlineData("foo/", 1)]
        public void InputParser_ParseStringWithBackReference(string testString, int expected)
        {
            var result = new InputParser().ParseInputString(testString);
            Assert.Equal(expected, result.PatternSegments.Count);
        }

        // Test actual evaluation of the types, verifying the correct string comes from the evalation
        [Theory]
        [InlineData("hey/hello/what", "hey/hello/what")]
        [InlineData("hey/{R:1}/what", "hey/foo/what")]
        [InlineData("hey/{R:2}/what", "hey/bar/what")]
        [InlineData("hey/{R:3}/what", "hey/baz/what")]
        [InlineData("hey/{C:1}/what", "hey/foo/what")]
        [InlineData("hey/{C:2}/what", "hey/bar/what")]
        [InlineData("hey/{C:3}/what", "hey/baz/what")]
        [InlineData("hey/{R:1}/{C:1}", "hey/foo/foo")]
        public void EvaluateBackReferenceRule(string testString, string expected)
        {
            var middle = new InputParser().ParseInputString(testString);
            var result = middle.Evaluate(CreateTestRewriteContext(), CreateTestRuleMatch(), CreateTestCondMatch());
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("hey/{ToLower:HEY}", "hey/hey")]
        [InlineData("hey/{ToLower:{R:1}}", "hey/foo")]
        [InlineData("hey/{ToLower:{C:1}}", "hey/foo")]
        [InlineData("hey/{ToLower:{C:1}/what}", "hey/foo/what")]
        [InlineData("hey/ToLower:/what", "hey/ToLower:/what")]
        public void EvaluatToLowerRule(string testString, string expected)
        {
            var middle = new InputParser().ParseInputString(testString);
            var result = middle.Evaluate(CreateTestRewriteContext(), CreateTestRuleMatch(), CreateTestCondMatch());
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("hey/{UrlEncode:<hey>}", "hey/%3Chey%3E")]
        public void EvaluatUriEncodeRule(string testString, string expected)
        {
            var middle = new InputParser().ParseInputString(testString);
            var result = middle.Evaluate(CreateTestRewriteContext(), CreateTestRuleMatch(), CreateTestCondMatch());
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("{")]
        [InlineData("{:}")]
        [InlineData("{R:")]
        [InlineData("{R:1")]
        [InlineData("{R:A}")]
        [InlineData("{R:10}")]
        [InlineData("{R:-1}")]
        [InlineData("{foo:1")]
        [InlineData("{UrlEncode:{R:}}")]
        [InlineData("{UrlEncode:{R:1}")]
        [InlineData("{HTTPS")]
        public void FormatExceptionsOnBadSyntax(string testString)
        {
            Assert.Throws<FormatException>(() => new InputParser().ParseInputString(testString));
        }

        private RewriteContext CreateTestRewriteContext()
        {

            var context = new DefaultHttpContext();
            return new RewriteContext { HttpContext = context, StaticFileProvider = null };
        }

        private MatchResults CreateTestRuleMatch()
        {
            var match = Regex.Match("foo/bar/baz", "(.*)/(.*)/(.*)");
            return new MatchResults { BackReference = match.Groups, Success = match.Success };
        }

        private MatchResults CreateTestCondMatch()
        {
            var match = Regex.Match("foo/bar/baz", "(.*)/(.*)/(.*)");
            return new MatchResults { BackReference = match.Groups, Success = match.Success };
        }
    }
}
