using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Common;
using NuGet.ProjectManagement;

namespace NuGet.CommandLine
{
    public class TransitiveRestoreSolutionContext
    {
        public IReadOnlyList<TransitiveRestoreProjectContext> ProjectContexts { get; }

        public TransitiveRestoreSolutionContext(string packagesDirectory,
            string projectFile,
            Func<string, IEnumerable<string>> getProjectReferences) :
            this(packagesDirectory, new string[] { projectFile }, getProjectReferences, false)
        {
        }

        public TransitiveRestoreSolutionContext(string packagesDirectory,
            IEnumerable<string> inputFiles,
            Func<string, IEnumerable<string>> getProjectReferences,
            bool useLockFiles)
        {
            var contexts = new List<TransitiveRestoreProjectContext>();
            ProjectContexts = contexts;

            foreach (var inputFile in inputFiles)
            {
                if (ProjectHelper.UnsupportedProjectExtensions.Contains(Path.GetExtension(inputFile)))
                {
                    // Unsupported projects such as DNX's .xproj are a noop and should
                    // be treated as a success.
                    continue;
                }

                var context = new TransitiveRestoreProjectContext()
                {
                    InputFileName = Path.GetFileName(inputFile),
                    ProjectDirectory = Path.GetDirectoryName(Path.GetFullPath(inputFile)),
                    IsStandaloneProjectJson = BuildIntegratedProjectUtility.IsProjectConfig(inputFile),
                    PackagesDirectory = packagesDirectory,
                };

                contexts.Add(context);

                // Determine the type of the input and restore it appropriately
                // Inputs can be: project.json files or msbuild project files
                if (context.IsStandaloneProjectJson)
                {
                    // Restore a project.json file using the directory as the Id
                    context.ProjectJsonPath = inputFile;
                    context.ProjectName = Path.GetFileName(context.ProjectDirectory);
                }
                else
                {
                    context.ProjectName = Path.GetFileNameWithoutExtension(inputFile);
                    context.ProjectJsonPath = BuildIntegratedProjectUtility.GetProjectConfigPath(context.ProjectDirectory,
                                                                                                 context.ProjectName);

                    // For known project types that support the msbuild p2p reference task find all project references.
                    if (!useLockFiles && MsBuildUtility.IsMsBuildBasedProject(inputFile))
                    {
                        // Restore a .csproj or other msbuild project file using the
                        // file name without the extension as the Id
                        context.ProjectReferences = getProjectReferences(inputFile);
                    }
                }
            }
        }
    }
}
