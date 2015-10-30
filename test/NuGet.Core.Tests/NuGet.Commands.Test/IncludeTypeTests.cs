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
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Test.Utility;
using Xunit;

namespace NuGet.Commands.Test
{
    public class IncludeTypeTests : IDisposable
    {
        [Fact]
        public async Task IncludeType_ProjectToProjectWithBuildOverrideToExclude()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            var configJson2 = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageX"": ""1.0.0""
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            var configJson1 = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageX"": {
                        ""version"": ""1.0.0"",
                        ""exclude"": ""build""
                    }
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            // Arrange
            var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
            _testFolders.Add(workingDir);

            var repository = Path.Combine(workingDir, "repository");
            Directory.CreateDirectory(repository);
            var projectDir = Path.Combine(workingDir, "project");
            Directory.CreateDirectory(projectDir);
            var packagesDir = Path.Combine(workingDir, "packages");
            Directory.CreateDirectory(packagesDir);
            var testProject1Dir = Path.Combine(projectDir, "TestProject1");
            Directory.CreateDirectory(testProject1Dir);
            var testProject2Dir = Path.Combine(projectDir, "TestProject2");
            Directory.CreateDirectory(testProject2Dir);

            // X -> Y -> B
            CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
            CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
            CreateDependencyB(repository);

            var sources = new List<PackageSource>();
            sources.Add(new PackageSource(repository));

            var specPath1 = Path.Combine(testProject1Dir, "project.json");
            var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
            using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
            {
                writer.WriteLine(configJson1.ToString());
            }

            var specPath2 = Path.Combine(testProject2Dir, "project.json");
            var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
            using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
            {
                writer.WriteLine(configJson2.ToString());
            }

            var request = new RestoreRequest(spec1, sources, packagesDir);
            request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
            request.ExternalProjects = new List<ExternalProjectReference>()
            {
                new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
            };

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var targetsFile = Path.Combine(testProject1Dir, "TestProject1.nuget.targets");

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(3, target.Libraries.Count);
            Assert.False(File.Exists(targetsFile));
        }

        [Fact]
        public async Task IncludeType_ProjectToProjectWithBuildOverride()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            var configJson2 = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageX"": ""1.0.0""
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            var configJson1 = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageX"": ""1.0.0""
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            // Arrange
            var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
            _testFolders.Add(workingDir);

            var repository = Path.Combine(workingDir, "repository");
            Directory.CreateDirectory(repository);
            var projectDir = Path.Combine(workingDir, "project");
            Directory.CreateDirectory(projectDir);
            var packagesDir = Path.Combine(workingDir, "packages");
            Directory.CreateDirectory(packagesDir);
            var testProject1Dir = Path.Combine(projectDir, "TestProject1");
            Directory.CreateDirectory(testProject1Dir);
            var testProject2Dir = Path.Combine(projectDir, "TestProject2");
            Directory.CreateDirectory(testProject2Dir);

            // X -> Y -> B
            CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
            CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
            CreateDependencyB(repository);

            var sources = new List<PackageSource>();
            sources.Add(new PackageSource(repository));

            var specPath1 = Path.Combine(testProject1Dir, "project.json");
            var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
            using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
            {
                writer.WriteLine(configJson1.ToString());
            }

            var specPath2 = Path.Combine(testProject2Dir, "project.json");
            var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
            using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
            {
                writer.WriteLine(configJson2.ToString());
            }

            var request = new RestoreRequest(spec1, sources, packagesDir);
            request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
            request.ExternalProjects = new List<ExternalProjectReference>()
            {
                new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
            };

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var targetsFile = Path.Combine(testProject1Dir, "TestProject1.nuget.targets");

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(3, target.Libraries.Count);
            Assert.True(File.Exists(targetsFile));
        }

        [Fact]
        public async Task IncludeType_ProjectToProjectNoTransitiveBuild()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            var configJson2 = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageX"": ""1.0.0""
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            var configJson1 = JObject.Parse(@"{
                ""dependencies"": {
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            // Arrange
            var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
            _testFolders.Add(workingDir);

            var repository = Path.Combine(workingDir, "repository");
            Directory.CreateDirectory(repository);
            var projectDir = Path.Combine(workingDir, "project");
            Directory.CreateDirectory(projectDir);
            var packagesDir = Path.Combine(workingDir, "packages");
            Directory.CreateDirectory(packagesDir);
            var testProject1Dir = Path.Combine(projectDir, "TestProject1");
            Directory.CreateDirectory(testProject1Dir);
            var testProject2Dir = Path.Combine(projectDir, "TestProject2");
            Directory.CreateDirectory(testProject2Dir);

            // X -> Y -> B
            CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
            CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
            CreateDependencyB(repository);

            var sources = new List<PackageSource>();
            sources.Add(new PackageSource(repository));

            var specPath1 = Path.Combine(testProject1Dir, "project.json");
            var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
            using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
            {
                writer.WriteLine(configJson1.ToString());
            }

            var specPath2 = Path.Combine(testProject2Dir, "project.json");
            var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
            using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
            {
                writer.WriteLine(configJson2.ToString());
            }

            var request = new RestoreRequest(spec1, sources, packagesDir);
            request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
            request.ExternalProjects = new List<ExternalProjectReference>()
            {
                new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
            };

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var targetsFile = Path.Combine(testProject1Dir, "TestProject1.nuget.targets");

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(3, target.Libraries.Count);
            Assert.False(File.Exists(targetsFile));
        }

        [Fact]
        public async Task IncludeType_ProjectToProjectNoTransitiveContent()
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            var configJson2 = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageX"": ""1.0.0""
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            var configJson1 = JObject.Parse(@"{
                ""dependencies"": {
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            // Arrange
            var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
            _testFolders.Add(workingDir);

            var repository = Path.Combine(workingDir, "repository");
            Directory.CreateDirectory(repository);
            var projectDir = Path.Combine(workingDir, "project");
            Directory.CreateDirectory(projectDir);
            var packagesDir = Path.Combine(workingDir, "packages");
            Directory.CreateDirectory(packagesDir);
            var testProject1Dir = Path.Combine(projectDir, "TestProject1");
            Directory.CreateDirectory(testProject1Dir);
            var testProject2Dir = Path.Combine(projectDir, "TestProject2");
            Directory.CreateDirectory(testProject2Dir);

            // X -> Y -> B
            CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
            CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
            CreateDependencyB(repository);

            var sources = new List<PackageSource>();
            sources.Add(new PackageSource(repository));

            var specPath1 = Path.Combine(testProject1Dir, "project.json");
            var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
            using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
            {
                writer.WriteLine(configJson1.ToString());
            }

            var specPath2 = Path.Combine(testProject2Dir, "project.json");
            var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
            using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
            {
                writer.WriteLine(configJson2.ToString());
            }

            var request = new RestoreRequest(spec1, sources, packagesDir);
            request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
            request.ExternalProjects = new List<ExternalProjectReference>()
            {
                new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
            };

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(3, target.Libraries.Count);
            Assert.Equal(3, result.LockFile.Libraries.Count);
            Assert.True(target.Libraries.All(lib => IsEmptyFolder(lib.ContentFiles)));
        }

        [Fact]
        public async Task IncludeType_ExcludedAndTransitivePackage()
        {
            // Restore Project1
            // Project2 has only build dependencies
            // Project1 -> packageB, Project2 -(suppress: all)-> packageX -> packageY -> packageB

            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            var configJson2 = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageX"": {
                        ""version"": ""1.0.0"",
                        ""suppressParent"": ""all""
                    },
                    ""packageB"": ""1.0.0""
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            var configJson1 = JObject.Parse(@"{
                ""dependencies"": {
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            // Arrange
            var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
            _testFolders.Add(workingDir);

            var repository = Path.Combine(workingDir, "repository");
            Directory.CreateDirectory(repository);
            var projectDir = Path.Combine(workingDir, "project");
            Directory.CreateDirectory(projectDir);
            var packagesDir = Path.Combine(workingDir, "packages");
            Directory.CreateDirectory(packagesDir);
            var testProject1Dir = Path.Combine(projectDir, "TestProject1");
            Directory.CreateDirectory(testProject1Dir);
            var testProject2Dir = Path.Combine(projectDir, "TestProject2");
            Directory.CreateDirectory(testProject2Dir);

            // X -> Y -> B
            CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
            CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
            CreateDependencyB(repository);

            var sources = new List<PackageSource>();
            sources.Add(new PackageSource(repository));

            var specPath1 = Path.Combine(testProject1Dir, "project.json");
            var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
            using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
            {
                writer.WriteLine(configJson1.ToString());
            }

            var specPath2 = Path.Combine(testProject2Dir, "project.json");
            var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
            using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
            {
                writer.WriteLine(configJson2.ToString());
            }

            var request = new RestoreRequest(spec1, sources, packagesDir);
            request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
            request.ExternalProjects = new List<ExternalProjectReference>()
            {
                new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
            };

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(1, target.Libraries.Count);
            Assert.Equal(1, result.LockFile.Libraries.Count);
            Assert.Equal("packageB", target.Libraries.Single().Name);
            Assert.Equal(1, target.Libraries.Single().CompileTimeAssemblies.Count);
        }

        [Fact]
        public async Task IncludeType_ProjectToProjectReferenceWithBuildReferenceAndTopLevel()
        {
            // Restore Project1
            // Project2 has only build dependencies
            // Project1 -> packageB, Project2 -(suppress: all)-> packageX -> packageY -> packageB

            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            var configJson2 = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageX"": {
                        ""version"": ""1.0.0"",
                        ""suppressParent"": ""all""
                    }
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            var configJson1 = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageB"": ""1.0.0""
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            // Arrange
            var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
            _testFolders.Add(workingDir);

            var repository = Path.Combine(workingDir, "repository");
            Directory.CreateDirectory(repository);
            var projectDir = Path.Combine(workingDir, "project");
            Directory.CreateDirectory(projectDir);
            var packagesDir = Path.Combine(workingDir, "packages");
            Directory.CreateDirectory(packagesDir);
            var testProject1Dir = Path.Combine(projectDir, "TestProject1");
            Directory.CreateDirectory(testProject1Dir);
            var testProject2Dir = Path.Combine(projectDir, "TestProject2");
            Directory.CreateDirectory(testProject2Dir);

            // X -> Y -> B
            CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
            CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
            CreateDependencyB(repository);

            var sources = new List<PackageSource>();
            sources.Add(new PackageSource(repository));

            var specPath1 = Path.Combine(testProject1Dir, "project.json");
            var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
            using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
            {
                writer.WriteLine(configJson1.ToString());
            }

            var specPath2 = Path.Combine(testProject2Dir, "project.json");
            var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
            using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
            {
                writer.WriteLine(configJson2.ToString());
            }

            var request = new RestoreRequest(spec1, sources, packagesDir);
            request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
            request.ExternalProjects = new List<ExternalProjectReference>()
            {
                new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
            };

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(1, target.Libraries.Count);
            Assert.Equal(1, result.LockFile.Libraries.Count);
            Assert.Equal("packageB", target.Libraries.Single().Name);
            Assert.Equal(1, target.Libraries.Single().CompileTimeAssemblies.Count);
        }

        [Fact]
        public async Task IncludeType_ProjectToProjectReferenceWithBuildReference()
        {
            // Restore Project1
            // Project2 has only build dependencies
            // Project1 -> Project2 -(suppress: all)-> packageX -> packageY -> packageB

            // Arrange
            var logger = new TestLogger();
            var framework = "net46";

            var configJson2 = JObject.Parse(@"{
                ""dependencies"": {
                    ""packageX"": {
                        ""version"": ""1.0.0"",
                        ""suppressParent"": ""all""
                    }
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            var configJson1 = JObject.Parse(@"{
                ""dependencies"": {
                },
                ""frameworks"": {
                ""net46"": {}
                }
            }");

            // Arrange
            var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
            _testFolders.Add(workingDir);

            var repository = Path.Combine(workingDir, "repository");
            Directory.CreateDirectory(repository);
            var projectDir = Path.Combine(workingDir, "project");
            Directory.CreateDirectory(projectDir);
            var packagesDir = Path.Combine(workingDir, "packages");
            Directory.CreateDirectory(packagesDir);
            var testProject1Dir = Path.Combine(projectDir, "TestProject1");
            Directory.CreateDirectory(testProject1Dir);
            var testProject2Dir = Path.Combine(projectDir, "TestProject2");
            Directory.CreateDirectory(testProject2Dir);

            // X -> Y -> B
            CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
            CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
            CreateDependencyB(repository);

            var sources = new List<PackageSource>();
            sources.Add(new PackageSource(repository));

            var specPath1 = Path.Combine(testProject1Dir, "project.json");
            var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
            using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
            {
                writer.WriteLine(configJson1.ToString());
            }

            var specPath2 = Path.Combine(testProject2Dir, "project.json");
            var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
            using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
            {
                writer.WriteLine(configJson2.ToString());
            }

            var request = new RestoreRequest(spec1, sources, packagesDir);
            request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
            request.ExternalProjects = new List<ExternalProjectReference>()
            {
                new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
            };

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);
            Assert.Equal(0, target.Libraries.Count);
            Assert.Equal(0, result.LockFile.Libraries.Count);
        }

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
            packageA.Add("include", "Runtime");

            // Act
            var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", "project.json");
            var result = spec.Dependencies.Single();

            // Assert
            Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Runtime));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.ContentFiles));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Build));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Native));
            Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Compile));
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
            packageA.Add("include", "contentFiles");

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
                        ""include"": ""Runtime""
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
            Assert.Equal(1, targetLibrary.Dependencies.Count);
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
                        ""include"": ""all""
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
            return CreateFullPackage(repositoryDir, "packageA", "1.0.0", "packageB", "1.0.0");
        }
        private static FileInfo CreateFullPackage(
            string repositoryDir,
            string id,
            string version)
        {
            return CreateFullPackage(repositoryDir, id, version, new List<Packaging.Core.PackageDependency>());
        }

        private static FileInfo CreateFullPackage(
            string repositoryDir, 
            string id, 
            string version, 
            IEnumerable<Packaging.Core.PackageDependency> dependencies)
        {
            var file = new FileInfo(Path.Combine(repositoryDir, $"{id}.{version}.nupkg"));

            using (var zip = new ZipArchive(File.Create(file.FullName), ZipArchiveMode.Create))
            {
                zip.AddEntry("contentFiles/any/any/config.xml", new byte[] { 0 });
                zip.AddEntry("contentFiles/cs/net45/code.cs", new byte[] { 0 });
                zip.AddEntry("lib/net45/a.dll", new byte[] { 0 });
                zip.AddEntry($"build/net45/{id}.targets", @"<targets />", Encoding.UTF8);
                zip.AddEntry("native/net45/a.dll", new byte[] { 0 });
                zip.AddEntry("tools/a.exe", new byte[] { 0 });

                zip.AddEntry($"{id}.nuspec", $@"<?xml version=""1.0"" encoding=""utf-8""?>
                        <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
                        <metadata>
                            <id>{id}</id>
                            <version>{version}</version>
                            <title />
                            <frameworkAssemblies>
                                <frameworkAssembly assemblyName=""System.Runtime"" />
                            </frameworkAssemblies>
                            <dependencies>
                                <group>
                                    <dependency id=""{dependencyId}"" version=""{dependencyVersion}"" />
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
                zip.AddEntry("lib/net45/a.dll", new byte[] { 0 });

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
