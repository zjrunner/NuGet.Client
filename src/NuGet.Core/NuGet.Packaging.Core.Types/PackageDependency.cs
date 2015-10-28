// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Globalization;

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
            IReadOnlyList<string> include,
            IReadOnlyList<string> exclude)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            if (include == null)
            {
                throw new ArgumentNullException(nameof(include));
            }

            if (exclude == null)
            {
                throw new ArgumentNullException(nameof(exclude));
            }

            Id = id;
            _versionRange = versionRange ?? VersionRange.All;
            Include = include;
            Exclude = exclude;
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
        public IReadOnlyList<string> Include { get; }

        /// <summary>
        /// Types to exclude from the dependency package.
        /// </summary>
        public IReadOnlyList<string> Exclude { get; }

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
