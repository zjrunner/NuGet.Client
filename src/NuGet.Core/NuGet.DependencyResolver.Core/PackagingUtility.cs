using NuGet.LibraryModel;

namespace NuGet.DependencyResolver
{
    public static class PackagingUtility
    {
        /// <summary>
        /// Convert a nuspec dependency to a library dependency.
        /// </summary>
        public static LibraryDependency GetLibraryDependencyFromNuspec(Packaging.Core.PackageDependency dependency)
        {
            var includeType = LibraryIncludeType.Default;

            if (dependency.Include.Count > 0)
            {
                includeType = LibraryIncludeType.Parse(dependency.Include);
            }

            if (dependency.Exclude.Count > 0)
            {
                includeType = includeType.Except(
                    LibraryIncludeType.Parse(dependency.Exclude));
            }

            var libraryDependency = new LibraryDependency
            {
                LibraryRange = new LibraryRange
                {
                    Name = dependency.Id,
                    VersionRange = dependency.VersionRange
                },
                IncludeType = includeType,
                SuppressParent = LibraryIncludeType.None
            };

            return libraryDependency;
        }
    }
}
