﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    public class IISMiddleware
    {
        private const string MSAspNetCoreClientCert = "MS-ASPNETCORE-CLIENTCERT";
        private const string MSAspNetCoreToken = "MS-ASPNETCORE-TOKEN";

        private readonly RequestDelegate _next;
        private readonly IISOptions _options;
        private readonly ILogger _logger;
        private readonly string _pairingToken;

        public IISMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IISOptions> options, string pairingToken, IEnumerable<IAuthenticationSchemeProvider> authentication)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (string.IsNullOrEmpty(pairingToken))
            {
                throw new ArgumentException("Missing or empty pairing token.");
            }

            _next = next;
            _options = options.Value;

            if (_options.ForwardWindowsAuthentication)
            {
                var auth = authentication.FirstOrDefault();
                if (auth == null)
                {
                    throw new InvalidOperationException("IServiceCollection.AddAuthentication() is required to use Authentication.");
                }

                auth.AddScheme(new AuthenticationScheme("Negotiate", typeof(AuthenticationHandler), new Dictionary<string, object>()));
            }

            _pairingToken = pairingToken;
            _logger = loggerFactory.CreateLogger<IISMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!string.Equals(_pairingToken, httpContext.Request.Headers[MSAspNetCoreToken], StringComparison.Ordinal))
            {
                _logger.LogError($"'{MSAspNetCoreToken}' does not match the expected pairing token '{_pairingToken}', request rejected.");
                httpContext.Response.StatusCode = 400;
                return;
            }

            if (Debugger.IsAttached && string.Equals("DEBUG", httpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                // The Visual Studio debugger tooling sends a DEBUG request to make IIS & AspNetCoreModule launch the process
                // so the debugger can attach. Filter out this request from the app.
                return;
            }

            if (_options.ForwardClientCertificate)
            {
                var header = httpContext.Request.Headers[MSAspNetCoreClientCert];
                if (!StringValues.IsNullOrEmpty(header))
                {
                    httpContext.Features.Set<ITlsConnectionFeature>(new ForwardedTlsConnectionFeature(_logger, header));
                }
            }

            await _next(httpContext);
        }
    }
}
