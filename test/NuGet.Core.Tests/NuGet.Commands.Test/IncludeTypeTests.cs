using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using NuGet.Test.Utility;
using Xunit;
using NuGet.LibraryModel;

namespace NuGet.Commands.Test
{
    public class IncludeTypeTests : IDisposable
    {
        [Fact]
        public void IncludeType_ProjectJsonDefaultFlags()
        {
            // Arrange
            JObject configJson = new JObject();
            var packageA = new JObject();
            var dependencies = new JObject();
            var frameworks = new JObject();
            var net46 = new JObject();
            frameworks.Add("net46", net46);
            configJson.Add("frameworks", frameworks);
            configJson.Add("dependencies", dependencies);
            dependencies.Add("packageA", packageA);
            packageA.Add("version", "1.0.0");

            // Act
            var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", "project.json");
            var result = spec.Dependencies.Single();

            // Assert
            Assert.True(result.HasFlag(LibraryIncludeTypeFlag.ContentFiles));
            Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Build));
            Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Native));
            Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Runtime));
            Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Compile));
            Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Runtime));
            Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Dependencies));
        }

        [Fact]
        public void IncludeType_ProjectJsonIncludeRuntimeOnly()
        {
            // Arrange
            JObject configJson = new JObject();
            var packageA = new JObject();
            var dependencies = new JObject();
            var frameworks = new JObject();
            var net46 = new JObject();
            frameworks.Add("net46", net46);
            configJson.Add("frameworks", frameworks);
            configJson.Add("dependencies", dependencies);
            dependencies.Add("packageA", packageA);
            packageA.Add("version", "1.0.0");
            packageA.Add("includeFlags", "Runtime");

            // Act
            var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", "project.json");
            var result = spec.Dependencies.Single();

            // Assert
            Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Runtime));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.ContentFiles));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Build));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Native));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Compile));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Dependencies));
        }

        [Fact]
        public void IncludeType_ProjectJsonIncludeContentFilesOnly()
        {
            // Arrange
            JObject configJson = new JObject();
            var packageA = new JObject();
            var dependencies = new JObject();
            var frameworks = new JObject();
            var net46 = new JObject();
            frameworks.Add("net46", net46);
            configJson.Add("frameworks", frameworks);
            configJson.Add("dependencies", dependencies);
            dependencies.Add("packageA", packageA);
            packageA.Add("version", "1.0.0");
            packageA.Add("includeFlags", "contentFiles");

            // Act
            var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", "project.json");
            var result = spec.Dependencies.Single();

            // Assert
            Assert.True(result.HasFlag(LibraryIncludeTypeFlag.ContentFiles));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Runtime));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Build));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Native));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Runtime));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Compile));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Runtime));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Dependencies));
        }

        [Fact]
        public async Task IncludeType_IncludeRuntimeOnly()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            JObject configJson = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageA"": {
                        ""version"": ""1.0.0"",
                        ""includeFlags"": ""Runtime""
                    }
                },
                ""frameworks"": {
                ""_FRAMEWORK_"": {}
                }
            }".Replace("_FRAMEWORK_", framework));

            // Act
            var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", "project.json");

            var result = await StandardSetup(framework, logger, configJson);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var targetLibrary = target.Libraries.FirstOrDefault(lib => lib.Name == "packageA");

            var packageAType = result.GetAllInstalled().FirstOrDefault(package => package.Name == "packageA").Type;

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.True(IsEmptyFolder(targetLibrary.ContentFiles));
            Assert.True(IsEmptyFolder(targetLibrary.NativeLibraries));
            Assert.Equal(1, targetLibrary.RuntimeAssemblies.Count);
            Assert.Equal(1, targetLibrary.FrameworkAssemblies.Count);
            Assert.Equal(0, targetLibrary.Dependencies.Count);
            Assert.True(IsEmptyFolder(targetLibrary.CompileTimeAssemblies));
        }

        [Fact]
        public async Task IncludeType_IncludeAllIncludesEverything()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            JObject configJson = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageA"": {
                        ""version"": ""1.0.0"",
                        ""includeFlags"": ""All""
                    }
                },
                ""frameworks"": {
                ""_FRAMEWORK_"": {}
                }
            }".Replace("_FRAMEWORK_", framework));

            // Act
            var result = await StandardSetup(framework, logger, configJson);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var targetLibrary = target.Libraries.FirstOrDefault(lib => lib.Name == "packageA");

            var packageAType = result.GetAllInstalled().FirstOrDefault(package => package.Name == "packageA").Type;

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(1, targetLibrary.ContentFiles.Count);
            Assert.Equal(1, targetLibrary.NativeLibraries.Count);
            Assert.Equal(1, targetLibrary.RuntimeAssemblies.Count);
            Assert.Equal(1, targetLibrary.FrameworkAssemblies.Count);
            Assert.Equal(1, targetLibrary.Dependencies.Count);
            Assert.Equal(1, targetLibrary.CompileTimeAssemblies.Count);
            Assert.Equal("Package", packageAType);
        }

        [Fact]
        public async Task IncludeType_NoTypeSetVerifyAllItemsIncludedByDefault()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            // Act
            var result = await StandardSetup(framework, logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var targetLibrary = target.Libraries.FirstOrDefault(lib => lib.Name == "packageA");

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(1, targetLibrary.ContentFiles.Count);
            Assert.Equal(1, targetLibrary.NativeLibraries.Count);
            Assert.Equal(1, targetLibrary.RuntimeAssemblies.Count);
            Assert.Equal(1, targetLibrary.FrameworkAssemblies.Count);
            Assert.Equal(1, targetLibrary.Dependencies.Count);
            Assert.Equal(1, targetLibrary.CompileTimeAssemblies.Count);
        }

        private async Task<RestoreResult> StandardSetup(
            string framework,
            NuGet.Logging.ILogger logger)
        {
            return await StandardSetup(framework, logger, null);
        }

        private async Task<RestoreResult> StandardSetup(
            string framework,
            NuGet.Logging.ILogger logger,
            JObject configJson)
        {
            // Arrange
            var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
            _testFolders.Add(workingDir);

            var repository = Path.Combine(workingDir, "repository");
            Directory.CreateDirectory(repository);
            var projectDir = Path.Combine(workingDir, "project");
            Directory.CreateDirectory(projectDir);
            var packagesDir = Path.Combine(workingDir, "packages");
            Directory.CreateDirectory(packagesDir);
            var testProjectDir = Path.Combine(projectDir, "TestProject");
            Directory.CreateDirectory(testProjectDir);

            CreateFullPackage(repository);
            CreateDependencyB(repository);

            var sources = new List<PackageSource>();
            sources.Add(new PackageSource(repository));

            if (configJson == null)
            {
                configJson = JObject.Parse(@"{
                  ""dependencies"": {
                    ""packageA"": ""1.0.0""
                  },
                  ""frameworks"": {
                    ""_FRAMEWORK_"": {}
                  }
                }".Replace("_FRAMEWORK_", framework));
            }

            var specPath = Path.Combine(projectDir, "TestProject", "project.json");
            var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", specPath);

            var request = new RestoreRequest(spec, sources, packagesDir);

            request.LockFilePath = Path.Combine(projectDir, "project.lock.json");

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            return result;
        }

        private static FileInfo CreateFullPackage(string repositoryDir)
        {
            var file = new FileInfo(Path.Combine(repositoryDir, "packageA.1.0.0.nupkg"));

            using (var zip = new ZipArchive(File.Create(file.FullName), ZipArchiveMode.Create))
            {
                zip.AddEntry("contentFiles/any/any/config.xml", new byte[] { 0 });
                zip.AddEntry("contentFiles/cs/net45/code.cs", new byte[] { 0 });
                zip.AddEntry("lib/net45/a.dll", new byte[] { 0 });
                zip.AddEntry("build/net45/packageA.targets", @"<targets />", Encoding.UTF8);
                zip.AddEntry("native/net45/a.dll", new byte[] { 0 });
                zip.AddEntry("tools/a.exe", new byte[] { 0 });

                zip.AddEntry("packageA.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
                        <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
                        <metadata>
                            <id>packageA</id>
                            <version>1.0.0</version>
                            <title />
                            <frameworkAssemblies>
                                <frameworkAssembly assemblyName=""System.Runtime"" />
                            </frameworkAssemblies>
                            <dependencies>
                                <group>
                                    <dependency id=""packageB"" version=""1.0.0"" />
                                </group>
                            </dependencies>
                            <contentFiles>
                                <files include=""cs/net45/config/config.xml"" buildAction=""none"" />
                                <files include=""cs/net45/config/config.xml"" copyToOutput=""true"" flatten=""false"" />
                                <files include=""cs/net45/images/image.jpg"" buildAction=""embeddedresource"" />
                            </contentFiles>
                        </metadata>
                        </package>", Encoding.UTF8);
            }

            return file;
        }

        private static FileInfo CreateDependencyB(string repositoryDir)
        {
            var file = new FileInfo(Path.Combine(repositoryDir, "packageB.1.0.0.nupkg"));

            using (var zip = new ZipArchive(File.Create(file.FullName), ZipArchiveMode.Create))
            {
                zip.AddEntry("contentFiles/any/any/config.xml", new byte[] { 0 });
                zip.AddEntry("contentFiles/lib/net45/a.dll", new byte[] { 0 });

                zip.AddEntry("packageB.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
                        <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
                        <metadata>
                            <id>packageB</id>
                            <version>1.0.0</version>
                            <title />
                        </metadata>
                        </package>", Encoding.UTF8);
            }

            return file;
        }

        private bool IsEmptyFolder(IList<LockFileItem> group)
        {
            return group.SingleOrDefault()?.Path.EndsWith("/_._") == true;
        }

        public void Dispose()
        {
            // Clean up
            foreach (var folder in _testFolders)
            {
                TestFileSystemUtility.DeleteRandomTestFolders(folder);
            }
        }

        private ConcurrentBag<string> _testFolders = new ConcurrentBag<string>();
    }
}
