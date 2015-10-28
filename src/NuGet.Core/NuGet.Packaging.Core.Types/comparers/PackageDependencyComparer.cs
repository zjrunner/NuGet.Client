﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using NuGet.Common;
using NuGet.Versioning;
using System.Linq;

namespace NuGet.Packaging.Core
{
    public class PackageDependencyComparer : IEqualityComparer<PackageDependency>
    {
        private readonly IVersionRangeComparer _versionRangeComparer;

        public PackageDependencyComparer()
            : this(VersionRangeComparer.Default)
        {
        }

        public PackageDependencyComparer(IVersionRangeComparer versionRangeComparer)
        {
            if (versionRangeComparer == null)
            {
                throw new ArgumentNullException("versionRangeComparer");
            }

            _versionRangeComparer = versionRangeComparer;
        }

        /// <summary>
        /// Default comparer
        /// Null ranges and the All range are treated as equal.
        /// </summary>
        public static readonly PackageDependencyComparer Default = new PackageDependencyComparer();

        public bool Equals(PackageDependency x, PackageDependency y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null)
                || ReferenceEquals(y, null))
            {
                return false;
            }

            var result = StringComparer.OrdinalIgnoreCase.Equals(x.Id, y.Id);

            if (result)
            {
                result = _versionRangeComparer.Equals(x.VersionRange ?? VersionRange.All, y.VersionRange ?? VersionRange.All);
            }

            if (result)
            {
                result = x.IncludeFlags.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .SequenceEqual(y.IncludeFlags.OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
            }

            if (result)
            {
                result = x.ExcludeFlags.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .SequenceEqual(y.ExcludeFlags.OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
            }

            return result;
        }

        public int GetHashCode(PackageDependency obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 0;
            }

            var combiner = new HashCodeCombiner();

            combiner.AddObject(obj.Id.ToUpperInvariant());

            // Treat null ranges and the All range as the same thing here
            if (obj.VersionRange != null
                && !obj.VersionRange.Equals(VersionRange.All))
            {
                combiner.AddObject(_versionRangeComparer.GetHashCode(obj.VersionRange));
            }

            foreach (var include in obj.IncludeFlags.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            {
                combiner.AddObject(include.ToLowerInvariant());
            }

            // separate the lists
            combiner.AddInt32(8);

            foreach (var exclude in obj.ExcludeFlags.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            {
                combiner.AddObject(exclude.ToLowerInvariant());
            }

            return combiner.CombinedHash;
        }
    }
}
