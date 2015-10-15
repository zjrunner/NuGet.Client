// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace NuGet.PackageManagement
{
    public class FolderNode : NodeBase
    {
        public string Name
        {
            get;
        }

        // The unique name of the corresponding DTE project
        public string UniqueName
        {
            get;
        }

        public IEnumerable<NodeBase> Children
        {
            get;
        }

        public FolderNode(
            string name, 
            string uniqueName, 
            IEnumerable<NodeBase> children)
        {
            Name = name;
            UniqueName = uniqueName;
            Children = children;
        }
    }
}