// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;

namespace NuGet.Credentials
{
    /// <summary>
    /// Provider that handles calling command line credential providers
    /// </summary>
    public class PluginCredentialProvider : ICredentialProvider
    {
        private readonly string _path;

        private readonly int _timeoutSeconds;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">Fully qualified plugin application path.</param>
        /// <param name="timeoutSeconds">Max timeout to wait for the plugin application
        /// to return credentials.</param>
        public PluginCredentialProvider(string path, int timeoutSeconds)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            _path = path;
            _timeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// Call the plugin credential provider application to acquire credentials.
        /// The request will be passed to the plugin on standard input as a json serialized
        /// PluginCredentialRequest.
        /// The plugin will return credentials as a json serialized PluginCredentialResponse.
        /// Valid credentials will be returned, or null if the provide cannot provide credentials
        /// for the given request.  If the plugin returns an Abort message, an exception will be thrown to
        /// fail the current request.
        /// </summary>
        /// <param name="uri">The uri of a web resource for which credentials are needed.</param>
        /// <param name="proxy">Ignored.  Proxy information will not be passed to plugins.</param>
        /// <param name="isProxyRequest">If true, the client is requesting credentials for a proxy.
        /// In this case, null will be returned, as plugins do not provide credentials for proxies.</param>
        /// <param name="isRetry">If true, credentials were previously supplied by this
        /// provider for the same uri.</param>
        /// <param name="nonInteractive">If true, the plugin must not prompt for credentials.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A credential object.  If </returns>
        public Task<ICredentials> Get(Uri uri, IWebProxy proxy, bool isProxyRequest, bool isRetry,
            bool nonInteractive, CancellationToken cancellationToken)
        {
            if(isProxyRequest)
            {
                return null;
            }

            PluginCredentialResponse response = null;
            try
            {
                var request = new PluginCredentialRequest
                {
                    Uri = uri.ToString(),
                    IsRetry = isRetry,
                    NonInteractive = nonInteractive
                };

                response = Execute(request, cancellationToken);
            }
            catch(PluginException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw PluginException.Create(_path, e);
            }

            if (response.Abort)
            {
                throw PluginException.CreateAbortMessage(_path, response.AbortMessage ?? string.Empty);
            }

            var result = response.IsValid ? new NetworkCredential(response.Username, response.Password) : null;
            var task = Task.FromResult((ICredentials)result);

            return task;
        }

        private PluginCredentialResponse Execute(PluginCredentialRequest request, CancellationToken cancellationToken)
        { 
            string requestString = string.Concat(JsonConvert.SerializeObject(request), Environment.NewLine);

            var startInfo = new ProcessStartInfo
            {
                FileName = _path,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ErrorDialog = false
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                throw PluginException.CreateNotStartedMessage(_path);
            }

            process.StandardInput.WriteLine(requestString);
            process.StandardInput.Flush();

            cancellationToken.Register(() => process.Kill());

            if (!process.WaitForExit(_timeoutSeconds * 1000))
            {
                throw PluginException.CreateTimeoutMessage(_path, _timeoutSeconds);
            }

            if(process.ExitCode > 0)
            {
                throw PluginException.CreateWrappedExceptionMessage(
                    _path,
                    process.ExitCode,
                    process.StandardOutput.ReadToEnd(),
                    process.StandardError.ReadToEnd());
            }

            var responseJson = process.StandardOutput.ReadToEnd();

            return JsonConvert.DeserializeObject<PluginCredentialResponse>(responseJson);
        }
    }
}
