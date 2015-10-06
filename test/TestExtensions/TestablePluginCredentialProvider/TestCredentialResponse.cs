// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Test.TestExtensions.TestablePluginCredentialProvider
{
    public class TestCredentialResponse
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public bool Abort { get; set; }

        public string AbortMessage { get; set; }

    }
}
