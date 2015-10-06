// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Test.TestExtensions.TestablePluginCredentialProvider
{
    public class TestCredentialRequest
    {
        public string Uri { get; set; }

        public bool NonInteractive { get; set; }

        public bool IsRetry { get; set; }

        public string ResponseUsername { get; set; }

        public string ResponsePassword { get; set; }

        public string ResponseExitCode { get; set; }

        public string ResponseShouldThrow { get; set; }

        public string ResponseShouldAbort { get; set; }

        public string ResponseAbortMessage { get; set; }

        public string ResponseDelaySeconds { get; set; }

    }
}
