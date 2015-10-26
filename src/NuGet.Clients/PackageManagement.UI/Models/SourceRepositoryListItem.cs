// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.PackageManagement.UI.Models
{
    internal class SourceRepositoryListItem
    {
        public SourceRepositoryListItem(string name, string id)
        {
            SourceName = name;
            Id = id;
        }

        public string SourceName { get; }

        public string Id { get; }
    }
}
