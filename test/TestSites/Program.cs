// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Hosting;

namespace TestSites
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var hostingConfiguration = WebApplicationConfiguration.GetDefault(args);
            var application = new WebApplicationBuilder()
                .UseIISPlatformHandler()
                .UseConfiguration(hostingConfiguration)
                .UseStartup("TestSites")
                .Build();

            application.Run();
        }
    }
}
