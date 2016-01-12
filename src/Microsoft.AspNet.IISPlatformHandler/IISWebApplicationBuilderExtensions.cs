using System;

namespace Microsoft.AspNet.Hosting
{
    public static class IISWebApplicationBuilderExtensions
    {
        public static IWebApplicationBuilder UseIISPlatformHandler(this IWebApplicationBuilder webApplicationBuilder)
        {
            var httpPlatformPort = Environment.GetEnvironmentVariable("HTTP_PLATFORM_PORT");

            if (string.IsNullOrEmpty(httpPlatformPort))
            {
                return webApplicationBuilder;
            }

            return webApplicationBuilder.UseSetting(WebApplicationDefaults.ServerUrlsKey, $"http://localhost:{httpPlatformPort}/");
        }
    }
}
