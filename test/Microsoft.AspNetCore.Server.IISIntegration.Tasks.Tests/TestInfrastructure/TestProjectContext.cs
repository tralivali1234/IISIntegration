// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.DotNet.Cli.Utils;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tasks.Tests
{
    public class TestProjectContext : IDisposable
    {
        private readonly string _taskDir;

        public TestProjectContext(string rootDir, string taskDir)
        {
            RootDir = rootDir;
            PublishDir = Path.Combine(RootDir, "pub");
            _taskDir = taskDir;
        }

        public string RootDir { get; }
        public string PublishDir { get; }


        public void Initialize()
        {
            // TODO actually do this when .NET Core SDK starts working on msbuild
            // var restore = CreateDotNet("restore", new[] { RootDir }).Execute();
            // Assert.Equal(0, restore.ExitCode);
        }

        public void Publish()
        {
            var props = new[]
            {
                "TestTargetsSource", _taskDir, // where test projects can find src
                "_AspNetCoreIISIntegrationTasksFolder", AppContext.BaseDirectory + '/', // hack target into using the dll in the test output folder
                "PublishDir", PublishDir
            };

            // TODO use "dotnet publish"
            var publish = CreateDotNet("build3", new[] { "/t:Publish", GetMsBuildPropsArg(props) }).Execute();
            Assert.Equal(0, publish.ExitCode);
        }

        private string GetMsBuildPropsArg(string[] props)
        {
            Assert.Equal(0, props.Length % 2);
            var sb = new StringBuilder("/p:");
            var first = true;
            for (var i = 0; i < props.Length; i += 2)
            {
                if (!first)
                {
                    sb.Append(";");
                }
                first = false;
                sb.Append(props[i]).Append("=").Append(props[i + 1]);
            }
            return sb.ToString();
        }

        private ICommand CreateDotNet(string command, IEnumerable<string> args)
        {
            var path = new Muxer().MuxerPath;
            // uncomment line below to use a different version of dotnet.exe
            // path = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ".dotnet", "dotnet.exe");
            return Command.Create(path, new[] { command }.Concat(args))
                .WorkingDirectory(RootDir);
                // uncomment to debug
                // .ForwardStdOut()
                // .ForwardStdErr();
        }

        public XDocument GetPublishedWebConfig()
        {
            return XDocument.Load(Path.Combine(PublishDir, "web.config"));
        }

        public void Dispose()
        {
           Directory.Delete(RootDir, recursive: true);
        }
    }
}