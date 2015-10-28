// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using NuGet.Versioning;
using System.Collections.Generic;

namespace NuGet.Packaging.Core
{
    /// <summary>
    /// Represents a package dependency Id and allowed version range.
    /// </summary>
    public class PackageDependency : IEquatable<PackageDependency>
    {
        private VersionRange _versionRange;

        public PackageDependency(string id)
            : this(id, VersionRange.All)
        {
        }

        public PackageDependency(string id, VersionRange versionRange)
            : this(id, versionRange, new List<string>(), new List<string>())
        {
        }

        public PackageDependency(
            string id,
            VersionRange versionRange,
            IReadOnlyList<string> includeFlags,
            IReadOnlyList<string> excludeFlags)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            if (includeFlags == null)
            {
                throw new ArgumentNullException(nameof(includeFlags));
            }

            if (excludeFlags == null)
            {
                throw new ArgumentNullException(nameof(excludeFlags));
            }

            Id = id;
            _versionRange = versionRange ?? VersionRange.All;
            IncludeFlags = includeFlags;
            ExcludeFlags = excludeFlags;
        }

        /// <summary>
        /// Dependency package Id
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Range of versions allowed for the depenency
        /// </summary>
        public VersionRange VersionRange
        {
            get { return _versionRange; }
        }

        /// <summary>
        /// Types to include from the dependency package.
        /// </summary>
        public IReadOnlyList<string> IncludeFlags { get; }

        /// <summary>
        /// Types to exclude from the dependency package.
        /// </summary>
        public IReadOnlyList<string> ExcludeFlags { get; }

        /// <summary>
        /// Sets the version range to also include prerelease versions
        /// </summary>
        public void SetIncludePrerelease()
        {
            _versionRange = VersionRange.SetIncludePrerelease(_versionRange, includePrerelease: true);
        }

        public bool Equals(PackageDependency other)
        {
            return PackageDependencyComparer.Default.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            var dependency = obj as PackageDependency;

            if (dependency != null)
            {
                return Equals(dependency);
            }

            return false;
        }

        /// <summary>
        /// Hash code from the default PackageDependencyComparer
        /// </summary>
        public override int GetHashCode()
        {
            return PackageDependencyComparer.Default.GetHashCode(this);
        }

        /// <summary>
        /// Id and Version range string
        /// </summary>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} {1}", Id, VersionRange.ToNormalizedString());
        }
    }
}
