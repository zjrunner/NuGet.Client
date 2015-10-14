using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NuGet.Client;
using NuGet.ContentModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.ProjectModel;
using NuGet.Repositories;

namespace NuGet.Commands
{
    internal static class LockFileUtils
    {
        public static LockFileTargetLibrary CreateLockFileTargetLibrary(
            LockFileLibrary library,
            LocalPackageInfo package,
            RestoreTargetGraph targetGraph,
            VersionFolderPathResolver defaultPackagePathResolver,
            string correctedPackageName)
        {
            return CreateLockFileTargetLibrary(library, package, targetGraph, defaultPackagePathResolver, correctedPackageName, targetFrameworkOverride: null);
        }

        public static LockFileTargetLibrary CreateLockFileTargetLibrary(
            LockFileLibrary library,
            LocalPackageInfo package,
            RestoreTargetGraph targetGraph,
            VersionFolderPathResolver defaultPackagePathResolver,
            string correctedPackageName,
            NuGetFramework targetFrameworkOverride)
        {
            var lockFileLib = new LockFileTargetLibrary();

            var framework = targetFrameworkOverride ?? targetGraph.Framework;
            var runtimeIdentifier = targetGraph.RuntimeIdentifier;

            // package.Id is read from nuspec and it might be in wrong casing.
            // correctedPackageName should be the package name used by dependency graph and
            // it has the correct casing that runtime needs during dependency resolution.
            lockFileLib.Name = correctedPackageName ?? package.Id;
            lockFileLib.Version = package.Version;

            IList<string> files;
            var contentItems = new ContentItemCollection();
            HashSet<string> referenceFilter = null;

            // If the previous LockFileLibrary was given, use that to find the file list. Otherwise read the nupkg.
            if (library == null)
            {
                using (var nupkgStream = File.OpenRead(package.ZipPath))
                {
                    var packageReader = new PackageReader(nupkgStream);
                    if (Path.DirectorySeparatorChar != '/')
                    {
                        files = packageReader
                            .GetFiles()
                            .Select(p => p.Replace(Path.DirectorySeparatorChar, '/'))
                            .ToList();
                    }
                    else
                    {
                        files = packageReader
                            .GetFiles()
                            .ToList();
                    }
                }
            }
            else
            {
                if (Path.DirectorySeparatorChar != '/')
                {
                    files = library.Files.Select(p => p.Replace(Path.DirectorySeparatorChar, '/')).ToList();
                }
                else
                {
                    files = library.Files;
                }
            }

            contentItems.Load(files);

            NuspecReader nuspec = null;

            var nuspecPath = defaultPackagePathResolver.GetManifestFilePath(package.Id, package.Version);

            if (File.Exists(nuspecPath))
            {
                using (var stream = File.OpenRead(nuspecPath))
                {
                    nuspec = new NuspecReader(stream);
                }
            }
            else
            {
                var dir = defaultPackagePathResolver.GetPackageDirectory(package.Id, package.Version);
                var folderReader = new PackageFolderReader(dir);

                using (var stream = folderReader.GetNuspec())
                {
                    nuspec = new NuspecReader(stream);
                }
            }

            var dependencySet = nuspec
                .GetDependencyGroups()
                .GetNearest(framework);

            if (dependencySet != null)
            {
                var set = dependencySet.Packages;

                if (set != null)
                {
                    lockFileLib.Dependencies = set.ToList();
                }
            }

            var referenceSet = nuspec.GetReferenceGroups().GetNearest(framework);
            if (referenceSet != null)
            {
                referenceFilter = new HashSet<string>(referenceSet.Items, StringComparer.OrdinalIgnoreCase);
            }

            // TODO: Remove this when we do #596
            // ASP.NET Core isn't compatible with generic PCL profiles
            if (!string.Equals(framework.Framework, FrameworkConstants.FrameworkIdentifiers.AspNetCore, StringComparison.OrdinalIgnoreCase)
                &&
                !string.Equals(framework.Framework, FrameworkConstants.FrameworkIdentifiers.DnxCore, StringComparison.OrdinalIgnoreCase))
            {
                var frameworkAssemblies = nuspec.GetFrameworkReferenceGroups().GetNearest(framework);
                if (frameworkAssemblies != null)
                {
                    foreach (var assemblyReference in frameworkAssemblies.Items)
                    {
                        lockFileLib.FrameworkAssemblies.Add(assemblyReference);
                    }
                }
            }

            var nativeCriteria = targetGraph.Conventions.Criteria.ForRuntime(targetGraph.RuntimeIdentifier);
            var managedCriteria = targetGraph.Conventions.Criteria.ForFrameworkAndRuntime(framework, targetGraph.RuntimeIdentifier);

            var compileGroup = contentItems.FindBestItemGroup(managedCriteria, targetGraph.Conventions.Patterns.CompileAssemblies, targetGraph.Conventions.Patterns.RuntimeAssemblies);

            if (compileGroup != null)
            {
                lockFileLib.CompileTimeAssemblies = compileGroup.Items.Select(t => new LockFileItem(t.Path)).ToList();
            }

            var runtimeGroup = contentItems.FindBestItemGroup(managedCriteria, targetGraph.Conventions.Patterns.RuntimeAssemblies);
            if (runtimeGroup != null)
            {
                lockFileLib.RuntimeAssemblies = runtimeGroup.Items.Select(p => new LockFileItem(p.Path)).ToList();
            }

            var resourceGroup = contentItems.FindBestItemGroup(managedCriteria, targetGraph.Conventions.Patterns.ResourceAssemblies);
            if (resourceGroup != null)
            {
                lockFileLib.ResourceAssemblies = resourceGroup.Items.Select(ToResourceLockFileItem).ToList();
            }

            var nativeGroup = contentItems.FindBestItemGroup(nativeCriteria, targetGraph.Conventions.Patterns.NativeLibraries);
            if (nativeGroup != null)
            {
                lockFileLib.NativeLibraries = nativeGroup.Items.Select(p => new LockFileItem(p.Path)).ToList();
            }

            // Shared content items
            var sharedCriteria = targetGraph.Conventions.Criteria.ForFramework(framework);

            var sharedContentGroups = contentItems.FindItemGroups(targetGraph.Conventions.Patterns.SharedContentFiles);

            // Multiple groups can match the same framework, find all of them
            var sharedGroupsForFramwork = GetContentGroupsForFramework(
                lockFileLib, 
                framework, 
                sharedContentGroups);

            lockFileLib.SharedContentGroups = GetSharedLockFileGroups(framework, nuspec, sharedGroupsForFramwork);

            // COMPAT: Support lib/contract so older packages can be consumed
            var contractPath = "lib/contract/" + package.Id + ".dll";
            var hasContract = files.Any(path => path == contractPath);
            var hasLib = lockFileLib.RuntimeAssemblies.Any();

            if (hasContract
                && hasLib
                && !framework.IsDesktop())
            {
                lockFileLib.CompileTimeAssemblies.Clear();
                lockFileLib.CompileTimeAssemblies.Add(new LockFileItem(contractPath));
            }

            // Apply filters from the <references> node in the nuspec
            if (referenceFilter != null)
            {
                // Remove anything that starts with "lib/" and is NOT specified in the reference filter.
                // runtimes/* is unaffected (it doesn't start with lib/)
                lockFileLib.RuntimeAssemblies = lockFileLib.RuntimeAssemblies.Where(p => !p.Path.StartsWith("lib/") || referenceFilter.Contains(Path.GetFileName(p.Path))).ToList();
                lockFileLib.CompileTimeAssemblies = lockFileLib.CompileTimeAssemblies.Where(p => !p.Path.StartsWith("lib/") || referenceFilter.Contains(Path.GetFileName(p.Path))).ToList();
            }

            return lockFileLib;
        }

        /// <summary>
        /// Get all content groups that have the nearest TxM
        /// </summary>
        private static List<ContentItemGroup> GetContentGroupsForFramework(
            LockFileTargetLibrary lockFileLib,
            NuGetFramework framework,
            IEnumerable<ContentItemGroup> contentGroups)
        {
            var groups = new List<ContentItemGroup>();

            // Find all unique frameworks
            var frameworks = contentGroups.Select(
                group =>
                    (NuGetFramework)group.Properties[ManagedCodeConventions.PropertyNames.TargetFrameworkMoniker])
                .Distinct()
                .ToList();

            // Find the best framework
            var nearestFramework =
                NuGetFrameworkUtility.GetNearest<NuGetFramework>(frameworks, framework, item => item);

            // If a compatible framework exists get all groups with that framework
            if (nearestFramework != null)
            {
                var sharedGroupsWithSameFramework = contentGroups.Where(
                    group =>
                    nearestFramework.Equals(
                        (NuGetFramework)group.Properties[ManagedCodeConventions.PropertyNames.TargetFrameworkMoniker])
                    );

                groups.AddRange(sharedGroupsWithSameFramework);
            }

            return groups;
        }

        /// <summary>
        /// Apply build actions from the nuspec to items from the shared folder.
        /// </summary>
        private static List<LockFileItemGroup> GetSharedLockFileGroups(
            NuGetFramework framework,
            NuspecReader nuspec,
            List<ContentItemGroup> sharedContentGroups)
        {
            var sharedLockFileGroups = new List<LockFileItemGroup>();

            // Read the shared section of the nuspec
            var nuspecSharedFiles = nuspec.GetSharedFiles().ToList();

            foreach (var group in sharedContentGroups)
            {
                var sharedLockFileItems = new List<LockFileItem>();

                // Create lock file entries for each item in the shared folder
                foreach (var item in group.Items)
                {
                    // defaults
                    var action = PackagingConstants.SharedContentDefaultBuildAction;
                    var copyToOutput = false;
                    var flatten = false;

                    foreach (var filesEntry in nuspecSharedFiles)
                    {
                        // TODO: add globbing
                        if (item.Path.Equals(filesEntry.Include, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!string.IsNullOrEmpty(filesEntry.BuildAction))
                            {
                                action = filesEntry.BuildAction;
                            }

                            if (filesEntry.CopyToOutput.HasValue)
                            {
                                copyToOutput = filesEntry.CopyToOutput.Value;
                            }

                            if (filesEntry.Flatten.HasValue)
                            {
                                flatten = filesEntry.Flatten.Value;
                            }
                        }
                    }

                    // Add attributes to the lock file item
                    var lockFileItem = new LockFileItem(item.Path);

                    // Do not write out properties for _._
                    if (!item.Path.EndsWith("/_._", StringComparison.Ordinal))
                    {
                        lockFileItem.Properties.Add("buildAction", action);
                        lockFileItem.Properties.Add("copyToOutput", copyToOutput.ToString());

                        if (copyToOutput)
                        {
                            string destination = null;

                            if (flatten)
                            {
                                destination = Path.GetFileName(lockFileItem.Path);
                            }
                            else
                            {
                                // Find path relative to the TxM
                                // Ex: shared/cs/net45/config/config.xml -> config/config.xml
                                destination = GetSharedPathRelativeToFrameworkFolder(item.Path);
                            }

                            lockFileItem.Properties.Add("outputPath", destination);
                        }

                        // Add the pp transform file if one exists
                        if (lockFileItem.Path.EndsWith(".pp", StringComparison.OrdinalIgnoreCase))
                        {
                            var destination = lockFileItem.Path.Substring(0, lockFileItem.Path.Length - 3);
                            destination = GetSharedPathRelativeToFrameworkFolder(destination);

                            lockFileItem.Properties.Add("ppOutputPath", destination);
                        }
                    }

                    sharedLockFileItems.Add(lockFileItem);
                }

                // Create a code language specific group for the items
                var lockFileGroup = new LockFileItemGroup(
                        ManagedCodeConventions.PropertyNames.CodeLanguage,
                        (string)group.Properties[ManagedCodeConventions.PropertyNames.CodeLanguage],
                        sharedLockFileItems
                    );

                sharedLockFileGroups.Add(lockFileGroup);
            }

            return sharedLockFileGroups;
        }

        // Find path relative to the TxM
        // Ex: shared/cs/net45/config/config.xml -> config/config.xml
        // Ex: shared/any/any/config/config.xml -> config/config.xml
        private static string GetSharedPathRelativeToFrameworkFolder(string itemPath)
        {
            var parts = itemPath.Split('/');

            if (parts.Length > 3)
            {
                return string.Join("/", parts.Skip(3));
            }

            Debug.Fail("Unable to get relative path: " + itemPath);
            return itemPath;
        }

        private static bool HasItems(ContentItemGroup compileGroup)
        {
            return (compileGroup != null && compileGroup.Items.Any());
        }

        private static LockFileItem ToResourceLockFileItem(ContentItem item)
        {
            return new LockFileItem(item.Path)
            {
                Properties =
                {
                    { "locale", item.Properties["locale"].ToString()}
                }
            };
        }

    }
}
