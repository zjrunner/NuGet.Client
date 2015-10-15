// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.ProjectManagement;

namespace NuGet.PackageManagement
{
    public class ProjectNode : NodeBase
    {
        public NuGetProject Project
        {
            get;
        }

        public ProjectNode(NuGetProject project)
        {
            Project = project;
        }
    }
}