using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using NuGet.Test.Utility;
using Xunit;

namespace NuGet.Commands.Test
{
    public class SharedContentTests : IDisposable
    {
        [Fact]
        public async Task SharedContent_DefaultActionsWithNoNuspecSettings()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
            _testFolders.Add(workingDir);

            var repository = Path.Combine(workingDir, "repository");
            Directory.CreateDirectory(repository);
            var projectDir = Path.Combine(workingDir, "project");
            Directory.CreateDirectory(projectDir);
            var packagesDir = Path.Combine(workingDir, "packages");
            Directory.CreateDirectory(packagesDir);

            // Create a shared content package with no nuspec
            CreateSharedContentPackageWithNoNuspecSettings(repository);

            var sources = new List<PackageSource>();
            sources.Add(new PackageSource(repository));

            var configJson = JObject.Parse(@"{
                ""dependencies"": {
                ""packageA"": ""1.0.0""
                },
                ""frameworks"": {
                ""_FRAMEWORK_"": {}
                }
            }".Replace("_FRAMEWORK_", framework));

            var specPath = Path.Combine(projectDir, "TestProject", "project.json");
            var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", specPath);

            var request = new RestoreRequest(spec, sources, packagesDir);
            request.MaxDegreeOfConcurrency = 1;
            request.LockFilePath = Path.Combine(projectDir, "project.lock.json");

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var utilItem = target.Libraries.Single().SharedContent
                .Single(item => item.Path == "shared/cs/net45/code/util.cs.pp");

            var configItem = target.Libraries.Single().SharedContent
                .Single(item => item.Path == "shared/cs/net45/config/config.xml");

            var imageItem = target.Libraries.Single().SharedContent
                .Single(item => item.Path == "shared/cs/net45/images/image.jpg");

            var utilFSItem = target.Libraries.Single().SharedContent
                .Single(item => item.Path == "shared/fs/net45/code/util.fs.pp");

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(4, utilItem.Properties.Count);
            Assert.Equal("compile", utilItem.Properties["buildAction"]);
            Assert.Equal("False", utilItem.Properties["copyToOutput"]);
            Assert.Equal("code/util.cs", utilItem.Properties["ppOutputPath"]);
            Assert.Equal(3, configItem.Properties.Count);
            Assert.Equal("compile", configItem.Properties["buildAction"]);
            Assert.Equal("False", configItem.Properties["copyToOutput"]);
            Assert.Equal(3, imageItem.Properties.Count);
            Assert.Equal("compile", imageItem.Properties["buildAction"]);
            Assert.Equal("False", imageItem.Properties["copyToOutput"]);
            Assert.Equal(4, utilFSItem.Properties.Count);
            Assert.Equal("compile", utilFSItem.Properties["buildAction"]);
            Assert.Equal("False", utilFSItem.Properties["copyToOutput"]);
            Assert.Equal("code/util.fs", utilFSItem.Properties["ppOutputPath"]);
        }

        [Fact(Skip = "Not working yet")]
        public async Task SharedContent_TurnOffSharedContent()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            JObject configJson = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageA"": { 
                        ""version"": ""1.0.0"",
                        ""type"": ""excludeSharedContent""
                    }
                },
                ""frameworks"": {
                ""_FRAMEWORK_"": {}
                }
            }".Replace("_FRAMEWORK_", framework));

            // Act
            var result = await StandardSetup(framework, logger, configJson);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var count = target.Libraries.Single().SharedContent.Count;

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task SharedContent_EmptyFolder()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "win8";

            // Act
            var result = await StandardSetup(framework, logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var item = target.Libraries.Single().SharedContent
                .Single();

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(0, item.Properties.Count);
        }

        [Fact]
        public async Task SharedContent_CopyToOutputSettings()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            // Act
            var result = await StandardSetup(framework, logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var helperCsItem = target.Libraries.Single().SharedContent
                .Single(item => item.Path == "shared/cs/net45/config/config.xml");

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(4, helperCsItem.Properties.Count);
            Assert.Equal("none", helperCsItem.Properties["buildAction"]);
            Assert.Equal("True", helperCsItem.Properties["copyToOutput"]);
            Assert.Equal("config/config.xml", helperCsItem.Properties["outputPath"]);
        }

        [Fact]
        public async Task SharedContent_VerifyPPInLockFile()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            // Act
            var result = await StandardSetup(framework, logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var helperCsItem = target.Libraries.Single().SharedContent
                .Single(item => item.Path == "shared/cs/net45/code/util.cs.pp");

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(4, helperCsItem.Properties.Count);
            Assert.Equal("code/util.cs", helperCsItem.Properties["ppOutputPath"]);
            Assert.Equal("compile", helperCsItem.Properties["buildAction"]);
            Assert.Equal("False", helperCsItem.Properties["copyToOutput"]);
        }

        private async Task<RestoreResult> SetupWithRuntimes(string framework, NuGet.Logging.ILogger logger)
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

            // Create a shared content package
            CreateSharedContentPackage(repository);
            CreateRuntimesPackage(repository);

            var sources = new List<PackageSource>();
            sources.Add(new PackageSource(repository));

            var configJson = JObject.Parse(@"{
                  ""supports"": {
                      ""net46.app"": {},
                      ""uwp.10.0.app"": { },
                      ""dnxcore50.app"": { }
                    },
                  ""dependencies"": {
                    ""packageA"": ""1.0.0"",
                    ""runtimes"": ""1.0.0""
                  },
                  ""frameworks"": {
                    ""_FRAMEWORK_"": {}
                  }
                }".Replace("_FRAMEWORK_", framework));

            var specPath = Path.Combine(projectDir, "TestProject", "project.json");
            var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", specPath);

            var request = new RestoreRequest(spec, sources, packagesDir);
            request.MaxDegreeOfConcurrency = 1;
            request.LockFilePath = Path.Combine(projectDir, "project.lock.json");

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            return result;
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

            // Create a shared content package
            CreateSharedContentPackage(repository);

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
            request.MaxDegreeOfConcurrency = 1;
            request.LockFilePath = Path.Combine(projectDir, "project.lock.json");

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            return result;
        }

        private static FileInfo CreateRuntimesPackage(string repositoryDir)
        {
            var file = new FileInfo(Path.Combine(repositoryDir, "runtimes.1.0.0.nupkg"));

            using (var zip = new ZipArchive(File.Create(file.FullName), ZipArchiveMode.Create))
            {
                zip.AddEntry("runtime.json", GetRuntimeJson(), Encoding.UTF8);

                zip.AddEntry("runtimes.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
                        <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
                        <metadata>
                        <id>runtimes</id>
                        <version>1.0.0</version>
                        <title />
                        </metadata>
                        </package>", Encoding.UTF8);
            }

            return file;
        }

        private static FileInfo CreateSharedContentPackage(string repositoryDir)
        {
            var file = new FileInfo(Path.Combine(repositoryDir, "packageA.1.0.0.nupkg"));

            using (var zip = new ZipArchive(File.Create(file.FullName), ZipArchiveMode.Create))
            {
                zip.AddEntry("shared/any/any/config/config.xml", new byte[] { 0 });
                zip.AddEntry("shared/any/any/images/image.jpg", new byte[] { 0 });
                zip.AddEntry("shared/any/any/images/image2.jpg", new byte[] { 0 });
                zip.AddEntry("shared/cs/net45/code/util.cs.pp", new byte[] { 0 });
                zip.AddEntry("shared/cs/net45/code/code.cs", new byte[] { 0 });
                zip.AddEntry("shared/fs/net45/code/util.fs.pp", new byte[] { 0 });
                zip.AddEntry("shared/cs/net45/config/config.xml", new byte[] { 0 });
                zip.AddEntry("shared/cs/net45/images/image.jpg", new byte[] { 0 });
                zip.AddEntry("shared/cs/win8/_._", new byte[] { 0 });

                zip.AddEntry("packageA.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
                        <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
                        <metadata>
                            <id>packageA</id>
                            <version>1.0.0</version>
                            <title />
                            <shared>
                                <files include=""cs/net45/config/config.xml"" buildAction=""none"" />
                                <files include=""cs/net45/config/config.xml"" copyToOutput=""true"" flatten=""false"" />
                                <files include=""cs/net45/images/image.jpg"" buildAction=""embeddedresource"" />
                            </shared>
                        </metadata>
                        </package>", Encoding.UTF8);
            }

            return file;
        }

        private static FileInfo CreateSharedContentPackageWithNoNuspecSettings(string repositoryDir)
        {
            var file = new FileInfo(Path.Combine(repositoryDir, "packageA.1.0.0.nupkg"));

            using (var zip = new ZipArchive(File.Create(file.FullName), ZipArchiveMode.Create))
            {
                zip.AddEntry("shared/any/any/config/config.xml", new byte[] { 0 });
                zip.AddEntry("shared/any/any/images/image.jpg", new byte[] { 0 });
                zip.AddEntry("shared/any/any/images/image2.jpg", new byte[] { 0 });
                zip.AddEntry("shared/cs/net45/code/util.cs.pp", new byte[] { 0 });
                zip.AddEntry("shared/fs/net45/code/util.fs.pp", new byte[] { 0 });
                zip.AddEntry("shared/cs/net45/config/config.xml", new byte[] { 0 });
                zip.AddEntry("shared/cs/net45/images/image.jpg", new byte[] { 0 });
                zip.AddEntry("shared/win8/_._", new byte[] { 0 });

                zip.AddEntry("packageA.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
                        <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
                        <metadata>
                            <id>packageA</id>
                            <version>1.0.0</version>
                            <title />
                        </metadata>
                        </package>", Encoding.UTF8);
            }

            return file;
        }

        private static string GetRuntimeJson()
        {
            return @"{
                ""supports"": {
                    ""uwp.10.0.app"": {
                            ""uap10.0"": [
                                ""win10-x86"",
                                ""win10-x86-aot"",
                                ""win10-x64"",
                                ""win10-x64-aot"",
                                ""win10-arm"",
                                ""win10-arm-aot""
                        ]
                    },
                    ""net46.app"": {
                        ""net46"": [
                            ""win-x86"",
                            ""win-x64""
                        ]
                    },
                    ""dnxcore50.app"": {
                        ""dnxcore50"": [
                            ""win7-x86"",
                            ""win7-x64""
                        ]
                    }
                }
            }";
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