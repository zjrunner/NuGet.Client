﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace NuGet.LibraryModel
{
    public class LibraryDependency
    {
        public LibraryRange LibraryRange { get; set; }

        public LibraryDependencyType Type { get; set; } = LibraryDependencyType.Default;

        public LibraryIncludeType IncludeType { get; set; } = LibraryIncludeType.Default;

        public LibraryIncludeType ParentExcludeType { get; set; } = LibraryIncludeType.None;

        public string Name
        {
            get { return LibraryRange.Name; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(LibraryRange);
            sb.Append(" ");
            sb.Append(Type);
            sb.Append(" ");
            sb.Append(IncludeType);
            return sb.ToString();
        }

        /// <summary>
        /// Type property flag
        /// </summary>
        public bool HasFlag(LibraryDependencyTypeFlag flag)
        {
            return Type.Contains(flag);
        }

        /// <summary>
        /// Include/Exclude property flag
        /// </summary>
        public bool HasFlag(LibraryIncludeTypeFlag flag)
        {
            return IncludeType.Contains(flag);
        }

        /// <summary>
        /// parentExclude property flag
        /// </summary>
        public bool HasParentExcludeFlag(LibraryIncludeTypeFlag flag)
        {
            return ParentExcludeType.Contains(flag);
        }
    }
}
