using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DelegationSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            var client = new HttpClient(new HttpClientHandler()
            {
                UseDefaultCredentials = true
            });

            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    await context.Response.WriteAsync(ex.ToString());
                }
            });
            
            app.Run(async (context) =>
            {
                context.Response.ContentType = "text/plain";

                if (context.Request.Path == "/user")
                {
                    try
                    {
                        await context.Response.WriteAsync(context.User.Identity.Name ?? "(none)");
                    }
                    catch (Exception ex)
                    {
                        await context.Response.WriteAsync("Request failed: " + ex.ToString() + Environment.NewLine);
                    }
                    return;
                }

                await context.Response.WriteAsync("Hello World - " + DateTimeOffset.Now + Environment.NewLine);
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Address:" + Environment.NewLine);
                await context.Response.WriteAsync("Scheme: " + context.Request.Scheme + Environment.NewLine);
                await context.Response.WriteAsync("Host: " + context.Request.Headers["Host"] + Environment.NewLine);
                await context.Response.WriteAsync("PathBase: " + context.Request.PathBase.Value + Environment.NewLine);
                await context.Response.WriteAsync("Path: " + context.Request.Path.Value + Environment.NewLine);
                await context.Response.WriteAsync("Query: " + context.Request.QueryString.Value + Environment.NewLine);
                await context.Response.WriteAsync(Environment.NewLine);

                var currentUser = WindowsIdentity.GetCurrent();

                await context.Response.WriteAsync("Current User: " + currentUser.Name + Environment.NewLine);
                try
                {
                    await context.Response.WriteAsync("AuthenticationType: " + currentUser.AuthenticationType + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    await context.Response.WriteAsync(ex.ToString());
                }
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Logged in User: " + context.User.Identity.Name + Environment.NewLine);
                try
                {
                    await context.Response.WriteAsync("AuthenticationType: " + context.User.Identity.AuthenticationType + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    await context.Response.WriteAsync(ex.ToString());
                }
                await context.Response.WriteAsync(Environment.NewLine);

                var winIdentity = context.User.Identity as WindowsIdentity;
                if (winIdentity == null)
                {
                    await context.Response.WriteAsync("Not a windows identity." + Environment.NewLine);
                    await context.Response.WriteAsync(Environment.NewLine);
                    return;
                }

                await context.Response.WriteAsync("Impersonation level: " + winIdentity.ImpersonationLevel + Environment.NewLine);
                await context.Response.WriteAsync(Environment.NewLine);

                try
                {
                    await context.Response.WriteAsync("Un-Delegated Request..." + Environment.NewLine);

                    var user = await client.GetStringAsync($"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/user");

                    await context.Response.WriteAsync("Un-Delegated User: " + user + Environment.NewLine);
                    await context.Response.WriteAsync(Environment.NewLine);

// #if NET46
                    // https://github.com/dotnet/cli/issues/1365#issuecomment-184822817
                    using (winIdentity.Impersonate())
                    {
                        await context.Response.WriteAsync("Impersonated user: " + WindowsIdentity.GetCurrent().Name + Environment.NewLine);
                        await context.Response.WriteAsync("AuthenticationType: " + WindowsIdentity.GetCurrent().AuthenticationType + Environment.NewLine);
                        await context.Response.WriteAsync("ImpersionationLevel: " + WindowsIdentity.GetCurrent().ImpersonationLevel + Environment.NewLine);

                        await context.Response.WriteAsync("Delegated Request..." + Environment.NewLine);

                        user = await client.GetStringAsync($"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/user");

                        await context.Response.WriteAsync("Delegated User: " + user + Environment.NewLine);
                        await context.Response.WriteAsync(Environment.NewLine);
                        
                        using (var p = Process.Start(
                            new ProcessStartInfo("whoami.exe")
                            {
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                            }))
                        {
                            user = p.StandardOutput.ReadToEnd();
                            await context.Response.WriteAsync("WhoAmI: " + user + Environment.NewLine);
                        }
                    }
// #else // Core/NetStandard 1.0
                    await WindowsIdentity.RunImpersonated<Task>(winIdentity.AccessToken, async () =>
                    {
                        await context.Response.WriteAsync("Impersonated user: " + WindowsIdentity.GetCurrent().Name + Environment.NewLine);
                        await context.Response.WriteAsync("AuthenticationType: " + WindowsIdentity.GetCurrent().AuthenticationType + Environment.NewLine);
                        await context.Response.WriteAsync("ImpersionationLevel: " + WindowsIdentity.GetCurrent().ImpersonationLevel + Environment.NewLine);

                        await context.Response.WriteAsync("Delegated Request..." + Environment.NewLine);

                        user = await client.GetStringAsync($"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/user");

                        await context.Response.WriteAsync("Delegated User: " + user + Environment.NewLine);
                        await context.Response.WriteAsync(Environment.NewLine);

                        using (var p = Process.Start(
                            new ProcessStartInfo("whoami.exe")
                            {
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                            }))
                        {
                            user = p.StandardOutput.ReadToEnd();
                            await context.Response.WriteAsync("WhoAmI: " + user + Environment.NewLine);
                        }
                    });
// #endif
                }
                catch (Exception ex)
                {
                    await context.Response.WriteAsync("Request failed: " + ex.ToString() + Environment.NewLine);
                }
            });
        }
    }
}
