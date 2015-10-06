using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGetVSExtension
{
    public class SleepingCredentialProvider : NuGet.Credentials.ICredentialProvider
    {
        public static int CallCount {get;set;}

    public static int ActiveCount { get; set; }

        public SleepingCredentialProvider(int delaySeconds)
        {
            DelaySeconds = delaySeconds;
        }

        public int DelaySeconds {get;}


    public async Task<ICredentials> Get(Uri uri, IWebProxy proxy, bool isProxyRequest, bool isRetry,
        bool nonInteractive, CancellationToken cancellationToken)
        {
            CallCount++;
            ActiveCount++;
            await Task.Delay(1000 * DelaySeconds, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            ActiveCount--;
            return null;
        }
    }
}
