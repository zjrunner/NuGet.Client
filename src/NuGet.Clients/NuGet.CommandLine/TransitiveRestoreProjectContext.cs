using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Common;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;

namespace NuGet.CommandLine
{
    public class TransitiveRestoreProjectContext
    {
        public string InputFileName { get; set; }
        public string ProjectDirectory { get; set; }
        public bool IsStandaloneProjectJson { get; set; }
        public string ProjectJsonPath { get; set; }
        public string ProjectName { get; set; }
        public Func<IEnumerable<string>> ProjectReferencesFactory = () => Enumerable.Empty<string>();
        public string PackagesDirectory { get; set; }

        /// <summary>
        /// Indicates that the user wants to treat the lock file as locked regadless of the file content
        /// </summary>
        public bool Lock { get; set; }

        public string RootDirectory
        {
            get
            {
                var rootDirectory = PackageSpecResolver.ResolveRootDirectory(InputFileName);

                return rootDirectory;
            }
        }

        public string LockFilePath
        {
            get
            {
                var lockFilePath = BuildIntegratedProjectUtility.GetLockFilePath(ProjectJsonPath);
                return lockFilePath;
            }
        }

        public LockFile GetLockFile(IConsole console)
        {
            var lockFile = BuildIntegratedRestoreUtility.GetLockFile(LockFilePath, console);

            // Force a lock to speed up restore
            if (Lock)
            {
                lockFile.IsLocked = true;
            }

            return lockFile;
        }
    }
}
