// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tasks.Tests
{
    public class EndToEndPublishTests : IClassFixture<MsBuildProjectFixture>
    {
        private MsBuildProjectFixture _fixture;

        public EndToEndPublishTests(MsBuildProjectFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void PublishForIIS_modifies_web_config_for_portable_app()
        {
            var testContext = _fixture.CreateTestProject("IHasWebConfig");
            testContext.Initialize();

            testContext.Publish();

            var aspNetCoreElement = testContext.GetPublishedWebConfig()
                .Descendants("aspNetCore")
                .Single();

            Assert.Equal(@"dotnet", (string)aspNetCoreElement.Attribute("processPath"));
            Assert.Equal(@".\IHasWebConfig.dll", (string)aspNetCoreElement.Attribute("arguments"));
            Assert.Equal(@"1234", (string)aspNetCoreElement.Attribute("startupTimeLimit"));
        }

        [Fact]
        public void PublishForIIS_creates_webConfig()
        {
            var testContext = _fixture.CreateTestProject("IHasNoWebConfig");
            testContext.Initialize();
            Assert.False(File.Exists(Path.Combine(testContext.PublishDir, "web.config")));

            testContext.Publish();

            Assert.True(File.Exists(Path.Combine(testContext.PublishDir, "web.config")));
            var aspNetCoreElement = testContext.GetPublishedWebConfig()
                .Descendants("aspNetCore")
                .Single();
            Assert.Equal(@"dotnet", (string)aspNetCoreElement.Attribute("processPath"));
            Assert.Equal(@".\IHasNoWebConfig.dll", (string)aspNetCoreElement.Attribute("arguments"));
        }

        [Fact]
        public void PublishForIIS_does_not_run_on_libraries()
        {
            var testContext = _fixture.CreateTestProject("IAmLibrary");
            testContext.Initialize();

            testContext.Publish();

            Assert.False(File.Exists(Path.Combine(testContext.PublishDir, "web.config")));
        }
    }
}
