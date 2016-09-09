// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.ProjectModel.FileSystemGlobbing;
using Microsoft.DotNet.ProjectModel.FileSystemGlobbing.Abstractions;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tasks.Tests
{
    public class MsBuildProjectFixture : IDisposable
    {
        private Stack<IDisposable> _disposables = new Stack<IDisposable>();
        private string _templateRoot = Path.Combine(AppContext.BaseDirectory, "TestProjects");
        private string _taskRoot = Path.Combine(AppContext.BaseDirectory, "src/Microsoft.AspNetCore.Server.IISIntegration.Tasks/build/netstandard1.0/");

        public TestProjectContext CreateTestProject(string templateName)
        {
            var projectTemplate = Path.Combine(_templateRoot, templateName);
            var targetDir = Path.Combine(Path.GetTempPath(), "iistests", Guid.NewGuid().ToString(), templateName);
            Directory.CreateDirectory(targetDir);

            var matcher = new Matcher().AddInclude("**/*");
            foreach (var file in matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(projectTemplate))).Files)
            {
                var src = Path.Combine(projectTemplate, file.Stem);
                var target = Path.Combine(targetDir, file.Stem);
                if (Path.GetFileName(file.Stem).EndsWith("project.json.ignore"))
                {
                    target = Path.Combine(targetDir, file.Stem.Substring(0, file.Stem.Length - ".ignore".Length));
                }
                File.Copy(src, target);
            }

            var context = new TestProjectContext(targetDir, _taskRoot);
            _disposables.Push(context);
            return context;
        }

        public void Dispose()
        {
            while (_disposables.Count > 0)
            {
                _disposables.Pop().Dispose();
            }
        }
    }
}