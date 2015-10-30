﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Test.Utility;
using NuGet.Versioning;
using Xunit;

namespace NuGet.Commands.Test
{
    public class IncludeTypeTests : IDisposable
    {
        //[Fact]
        //public async Task IncludeType_ProjectToProjectWithBuildOverrideToExclude()
        //{
        //    // Arrange
        //    var logger = new TestLogger();
        //    var framework = "net46";

        //    var configJson2 = JObject.Parse(@"{
        //        ""dependencies"": {
        //            ""packageX"": ""1.0.0""
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    var configJson1 = JObject.Parse(@"{
        //        ""dependencies"": {
        //            ""packageX"": {
        //                ""version"": ""1.0.0"",
        //                ""exclude"": ""build""
        //            }
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    // Arrange
        //    var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
        //    _testFolders.Add(workingDir);

        //    var repository = Path.Combine(workingDir, "repository");
        //    Directory.CreateDirectory(repository);
        //    var projectDir = Path.Combine(workingDir, "project");
        //    Directory.CreateDirectory(projectDir);
        //    var packagesDir = Path.Combine(workingDir, "packages");
        //    Directory.CreateDirectory(packagesDir);
        //    var testProject1Dir = Path.Combine(projectDir, "TestProject1");
        //    Directory.CreateDirectory(testProject1Dir);
        //    var testProject2Dir = Path.Combine(projectDir, "TestProject2");
        //    Directory.CreateDirectory(testProject2Dir);

        //    // X -> Y -> B
        //    CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
        //    CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
        //    CreateDependencyB(repository);

        //    var sources = new List<PackageSource>();
        //    sources.Add(new PackageSource(repository));

        //    var specPath1 = Path.Combine(testProject1Dir, "project.json");
        //    var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
        //    {
        //        writer.WriteLine(configJson1.ToString());
        //    }

        //    var specPath2 = Path.Combine(testProject2Dir, "project.json");
        //    var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
        //    {
        //        writer.WriteLine(configJson2.ToString());
        //    }

        //    var request = new RestoreRequest(spec1, sources, packagesDir);
        //    request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
        //    request.ExternalProjects = new List<ExternalProjectReference>()
        //    {
        //        new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
        //    };

        //    var command = new RestoreCommand(logger, request);

        //    // Act
        //    var result = await command.ExecuteAsync();
        //    result.Commit(logger);

        //    var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

        //    var targetsFile = Path.Combine(testProject1Dir, "TestProject1.nuget.targets");

        //    // Assert
        //    Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
        //    Assert.Equal(0, logger.Errors);
        //    Assert.Equal(0, logger.Warnings);
        //    Assert.Equal(3, target.Libraries.Count);
        //    Assert.False(File.Exists(targetsFile));
        //}

        //[Fact]
        //public async Task IncludeType_ProjectToProjectWithBuildOverride()
        //{
        //    // Arrange
        //    var logger = new TestLogger();
        //    var framework = "net46";

        //    var configJson2 = JObject.Parse(@"{
        //        ""dependencies"": {
        //            ""packageX"": ""1.0.0""
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    var configJson1 = JObject.Parse(@"{
        //        ""dependencies"": {
        //            ""packageX"": ""1.0.0""
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    // Arrange
        //    var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
        //    _testFolders.Add(workingDir);

        //    var repository = Path.Combine(workingDir, "repository");
        //    Directory.CreateDirectory(repository);
        //    var projectDir = Path.Combine(workingDir, "project");
        //    Directory.CreateDirectory(projectDir);
        //    var packagesDir = Path.Combine(workingDir, "packages");
        //    Directory.CreateDirectory(packagesDir);
        //    var testProject1Dir = Path.Combine(projectDir, "TestProject1");
        //    Directory.CreateDirectory(testProject1Dir);
        //    var testProject2Dir = Path.Combine(projectDir, "TestProject2");
        //    Directory.CreateDirectory(testProject2Dir);

        //    // X -> Y -> B
        //    CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
        //    CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
        //    CreateDependencyB(repository);

        //    var sources = new List<PackageSource>();
        //    sources.Add(new PackageSource(repository));

        //    var specPath1 = Path.Combine(testProject1Dir, "project.json");
        //    var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
        //    {
        //        writer.WriteLine(configJson1.ToString());
        //    }

        //    var specPath2 = Path.Combine(testProject2Dir, "project.json");
        //    var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
        //    {
        //        writer.WriteLine(configJson2.ToString());
        //    }

        //    var request = new RestoreRequest(spec1, sources, packagesDir);
        //    request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
        //    request.ExternalProjects = new List<ExternalProjectReference>()
        //    {
        //        new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
        //    };

        //    var command = new RestoreCommand(logger, request);

        //    // Act
        //    var result = await command.ExecuteAsync();
        //    result.Commit(logger);

        //    var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

        //    var targetsFile = Path.Combine(testProject1Dir, "TestProject1.nuget.targets");

        //    // Assert
        //    Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
        //    Assert.Equal(0, logger.Errors);
        //    Assert.Equal(0, logger.Warnings);
        //    Assert.Equal(3, target.Libraries.Count);
        //    Assert.True(File.Exists(targetsFile));
        //}

        //[Fact]
        //public async Task IncludeType_ProjectToProjectNoTransitiveBuild()
        //{
        //    // Arrange
        //    var logger = new TestLogger();
        //    var framework = "net46";

        //    var configJson2 = JObject.Parse(@"{
        //        ""dependencies"": {
        //            ""packageX"": ""1.0.0""
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    var configJson1 = JObject.Parse(@"{
        //        ""dependencies"": {
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    // Arrange
        //    var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
        //    _testFolders.Add(workingDir);

        //    var repository = Path.Combine(workingDir, "repository");
        //    Directory.CreateDirectory(repository);
        //    var projectDir = Path.Combine(workingDir, "project");
        //    Directory.CreateDirectory(projectDir);
        //    var packagesDir = Path.Combine(workingDir, "packages");
        //    Directory.CreateDirectory(packagesDir);
        //    var testProject1Dir = Path.Combine(projectDir, "TestProject1");
        //    Directory.CreateDirectory(testProject1Dir);
        //    var testProject2Dir = Path.Combine(projectDir, "TestProject2");
        //    Directory.CreateDirectory(testProject2Dir);

        //    // X -> Y -> B
        //    CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
        //    CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
        //    CreateDependencyB(repository);

        //    var sources = new List<PackageSource>();
        //    sources.Add(new PackageSource(repository));

        //    var specPath1 = Path.Combine(testProject1Dir, "project.json");
        //    var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
        //    {
        //        writer.WriteLine(configJson1.ToString());
        //    }

        //    var specPath2 = Path.Combine(testProject2Dir, "project.json");
        //    var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
        //    {
        //        writer.WriteLine(configJson2.ToString());
        //    }

        //    var request = new RestoreRequest(spec1, sources, packagesDir);
        //    request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
        //    request.ExternalProjects = new List<ExternalProjectReference>()
        //    {
        //        new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
        //    };

        //    var command = new RestoreCommand(logger, request);

        //    // Act
        //    var result = await command.ExecuteAsync();
        //    result.Commit(logger);

        //    var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

        //    var targetsFile = Path.Combine(testProject1Dir, "TestProject1.nuget.targets");

        //    // Assert
        //    Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
        //    Assert.Equal(0, logger.Errors);
        //    Assert.Equal(0, logger.Warnings);
        //    Assert.Equal(3, target.Libraries.Count);
        //    Assert.False(File.Exists(targetsFile));
        //}

        //[Fact]
        //public async Task IncludeType_ProjectToProjectNoTransitiveContent()
        //{
        //    // Arrange
        //    var logger = new TestLogger();
        //    var framework = "net46";

        //    var configJson2 = JObject.Parse(@"{
        //        ""dependencies"": {
        //            ""packageX"": ""1.0.0""
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    var configJson1 = JObject.Parse(@"{
        //        ""dependencies"": {
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    // Arrange
        //    var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
        //    _testFolders.Add(workingDir);

        //    var repository = Path.Combine(workingDir, "repository");
        //    Directory.CreateDirectory(repository);
        //    var projectDir = Path.Combine(workingDir, "project");
        //    Directory.CreateDirectory(projectDir);
        //    var packagesDir = Path.Combine(workingDir, "packages");
        //    Directory.CreateDirectory(packagesDir);
        //    var testProject1Dir = Path.Combine(projectDir, "TestProject1");
        //    Directory.CreateDirectory(testProject1Dir);
        //    var testProject2Dir = Path.Combine(projectDir, "TestProject2");
        //    Directory.CreateDirectory(testProject2Dir);

        //    // X -> Y -> B
        //    CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
        //    CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
        //    CreateDependencyB(repository);

        //    var sources = new List<PackageSource>();
        //    sources.Add(new PackageSource(repository));

        //    var specPath1 = Path.Combine(testProject1Dir, "project.json");
        //    var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
        //    {
        //        writer.WriteLine(configJson1.ToString());
        //    }

        //    var specPath2 = Path.Combine(testProject2Dir, "project.json");
        //    var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
        //    {
        //        writer.WriteLine(configJson2.ToString());
        //    }

        //    var request = new RestoreRequest(spec1, sources, packagesDir);
        //    request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
        //    request.ExternalProjects = new List<ExternalProjectReference>()
        //    {
        //        new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
        //    };

        //    var command = new RestoreCommand(logger, request);

        //    // Act
        //    var result = await command.ExecuteAsync();
        //    result.Commit(logger);

        //    var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

        //    // Assert
        //    Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
        //    Assert.Equal(0, logger.Errors);
        //    Assert.Equal(0, logger.Warnings);
        //    Assert.Equal(3, target.Libraries.Count);
        //    Assert.Equal(3, result.LockFile.Libraries.Count);
        //    Assert.True(target.Libraries.All(lib => IsEmptyFolder(lib.ContentFiles)));
        //}

        //[Fact]
        //public async Task IncludeType_ExcludedAndTransitivePackage()
        //{
        //    // Restore Project1
        //    // Project2 has only build dependencies
        //    // Project1 -> packageB, Project2 -(suppress: all)-> packageX -> packageY -> packageB

        //    // Arrange
        //    var logger = new TestLogger();
        //    var framework = "net46";

        //    var configJson2 = JObject.Parse(@"{
        //        ""dependencies"": {
        //            ""packageX"": {
        //                ""version"": ""1.0.0"",
        //                ""suppressParent"": ""all""
        //            },
        //            ""packageB"": ""1.0.0""
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    var configJson1 = JObject.Parse(@"{
        //        ""dependencies"": {
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    // Arrange
        //    var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
        //    _testFolders.Add(workingDir);

        //    var repository = Path.Combine(workingDir, "repository");
        //    Directory.CreateDirectory(repository);
        //    var projectDir = Path.Combine(workingDir, "project");
        //    Directory.CreateDirectory(projectDir);
        //    var packagesDir = Path.Combine(workingDir, "packages");
        //    Directory.CreateDirectory(packagesDir);
        //    var testProject1Dir = Path.Combine(projectDir, "TestProject1");
        //    Directory.CreateDirectory(testProject1Dir);
        //    var testProject2Dir = Path.Combine(projectDir, "TestProject2");
        //    Directory.CreateDirectory(testProject2Dir);

        //    // X -> Y -> B
        //    CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
        //    CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
        //    CreateDependencyB(repository);

        //    var sources = new List<PackageSource>();
        //    sources.Add(new PackageSource(repository));

        //    var specPath1 = Path.Combine(testProject1Dir, "project.json");
        //    var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
        //    {
        //        writer.WriteLine(configJson1.ToString());
        //    }

        //    var specPath2 = Path.Combine(testProject2Dir, "project.json");
        //    var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
        //    {
        //        writer.WriteLine(configJson2.ToString());
        //    }

        //    var request = new RestoreRequest(spec1, sources, packagesDir);
        //    request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
        //    request.ExternalProjects = new List<ExternalProjectReference>()
        //    {
        //        new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
        //    };

        //    var command = new RestoreCommand(logger, request);

        //    // Act
        //    var result = await command.ExecuteAsync();
        //    result.Commit(logger);

        //    var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

        //    // Assert
        //    Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
        //    Assert.Equal(0, logger.Errors);
        //    Assert.Equal(0, logger.Warnings);
        //    Assert.Equal(1, target.Libraries.Count);
        //    Assert.Equal(1, result.LockFile.Libraries.Count);
        //    Assert.Equal("packageB", target.Libraries.Single().Name);
        //    Assert.Equal(1, target.Libraries.Single().CompileTimeAssemblies.Count);
        //}

        //[Fact]
        //public async Task IncludeType_ProjectToProjectReferenceWithBuildReferenceAndTopLevel()
        //{
        //    // Restore Project1
        //    // Project2 has only build dependencies
        //    // Project1 -> packageB, Project2 -(suppress: all)-> packageX -> packageY -> packageB

        //    // Arrange
        //    var logger = new TestLogger();
        //    var framework = "net46";

        //    var configJson2 = JObject.Parse(@"{
        //        ""dependencies"": {
        //            ""packageX"": {
        //                ""version"": ""1.0.0"",
        //                ""suppressParent"": ""all""
        //            }
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    var configJson1 = JObject.Parse(@"{
        //        ""dependencies"": {
        //            ""packageB"": ""1.0.0""
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    // Arrange
        //    var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
        //    _testFolders.Add(workingDir);

        //    var repository = Path.Combine(workingDir, "repository");
        //    Directory.CreateDirectory(repository);
        //    var projectDir = Path.Combine(workingDir, "project");
        //    Directory.CreateDirectory(projectDir);
        //    var packagesDir = Path.Combine(workingDir, "packages");
        //    Directory.CreateDirectory(packagesDir);
        //    var testProject1Dir = Path.Combine(projectDir, "TestProject1");
        //    Directory.CreateDirectory(testProject1Dir);
        //    var testProject2Dir = Path.Combine(projectDir, "TestProject2");
        //    Directory.CreateDirectory(testProject2Dir);

        //    // X -> Y -> B
        //    CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
        //    CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
        //    CreateDependencyB(repository);

        //    var sources = new List<PackageSource>();
        //    sources.Add(new PackageSource(repository));

        //    var specPath1 = Path.Combine(testProject1Dir, "project.json");
        //    var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
        //    {
        //        writer.WriteLine(configJson1.ToString());
        //    }

        //    var specPath2 = Path.Combine(testProject2Dir, "project.json");
        //    var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
        //    {
        //        writer.WriteLine(configJson2.ToString());
        //    }

        //    var request = new RestoreRequest(spec1, sources, packagesDir);
        //    request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
        //    request.ExternalProjects = new List<ExternalProjectReference>()
        //    {
        //        new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
        //    };

        //    var command = new RestoreCommand(logger, request);

        //    // Act
        //    var result = await command.ExecuteAsync();
        //    result.Commit(logger);

        //    var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

        //    // Assert
        //    Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
        //    Assert.Equal(0, logger.Errors);
        //    Assert.Equal(0, logger.Warnings);
        //    Assert.Equal(1, target.Libraries.Count);
        //    Assert.Equal(1, result.LockFile.Libraries.Count);
        //    Assert.Equal("packageB", target.Libraries.Single().Name);
        //    Assert.Equal(1, target.Libraries.Single().CompileTimeAssemblies.Count);
        //}

        //[Fact]
        //public async Task IncludeType_ProjectToProjectReferenceWithBuildReference()
        //{
        //    // Restore Project1
        //    // Project2 has only build dependencies
        //    // Project1 -> Project2 -(suppress: all)-> packageX -> packageY -> packageB

        //    // Arrange
        //    var logger = new TestLogger();
        //    var framework = "net46";

        //    var configJson2 = JObject.Parse(@"{
        //        ""dependencies"": {
        //            ""packageX"": {
        //                ""version"": ""1.0.0"",
        //                ""suppressParent"": ""all""
        //            }
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    var configJson1 = JObject.Parse(@"{
        //        ""dependencies"": {
        //        },
        //        ""frameworks"": {
        //        ""net46"": {}
        //        }
        //    }");

        //    // Arrange
        //    var workingDir = TestFileSystemUtility.CreateRandomTestFolder();
        //    _testFolders.Add(workingDir);

        //    var repository = Path.Combine(workingDir, "repository");
        //    Directory.CreateDirectory(repository);
        //    var projectDir = Path.Combine(workingDir, "project");
        //    Directory.CreateDirectory(projectDir);
        //    var packagesDir = Path.Combine(workingDir, "packages");
        //    Directory.CreateDirectory(packagesDir);
        //    var testProject1Dir = Path.Combine(projectDir, "TestProject1");
        //    Directory.CreateDirectory(testProject1Dir);
        //    var testProject2Dir = Path.Combine(projectDir, "TestProject2");
        //    Directory.CreateDirectory(testProject2Dir);

        //    // X -> Y -> B
        //    CreateFullPackage(repository, "packageX", "1.0.0", "packageY", "1.0.0");
        //    CreateFullPackage(repository, "packageY", "1.0.0", "packageB", "1.0.0");
        //    CreateDependencyB(repository);

        //    var sources = new List<PackageSource>();
        //    sources.Add(new PackageSource(repository));

        //    var specPath1 = Path.Combine(testProject1Dir, "project.json");
        //    var spec1 = JsonPackageSpecReader.GetPackageSpec(configJson1.ToString(), "TestProject1", specPath1);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath1)))
        //    {
        //        writer.WriteLine(configJson1.ToString());
        //    }

        //    var specPath2 = Path.Combine(testProject2Dir, "project.json");
        //    var spec2 = JsonPackageSpecReader.GetPackageSpec(configJson2.ToString(), "TestProject2", specPath2);
        //    using (var writer = new StreamWriter(File.OpenWrite(specPath2)))
        //    {
        //        writer.WriteLine(configJson2.ToString());
        //    }

        //    var request = new RestoreRequest(spec1, sources, packagesDir);
        //    request.LockFilePath = Path.Combine(testProject1Dir, "project.lock.json");
        //    request.ExternalProjects = new List<ExternalProjectReference>()
        //    {
        //        new ExternalProjectReference("TestProject2", specPath2, Enumerable.Empty<string>())
        //    };

        //    var command = new RestoreCommand(logger, request);

        //    // Act
        //    var result = await command.ExecuteAsync();
        //    result.Commit(logger);

        //    var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

        //    // Assert
        //    Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
        //    Assert.Equal(0, logger.Errors);
        //    Assert.Equal(0, logger.Warnings);
        //    Assert.Equal(0, target.Libraries.Count);
        //    Assert.Equal(0, result.LockFile.Libraries.Count);
        //}

        //[Fact]
        //public void IncludeType_ProjectJsonDefaultFlags()
        //{
        //    // Arrange
        //    JObject configJson = new JObject();
        //    var packageA = new JObject();
        //    var dependencies = new JObject();
        //    var frameworks = new JObject();
        //    var net46 = new JObject();
        //    frameworks.Add("net46", net46);
        //    configJson.Add("frameworks", frameworks);
        //    configJson.Add("dependencies", dependencies);
        //    dependencies.Add("packageA", packageA);
        //    packageA.Add("version", "1.0.0");

        //    // Act
        //    var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", "project.json");
        //    var result = spec.Dependencies.Single();

        //    // Assert
        //    Assert.True(result.HasFlag(LibraryIncludeTypeFlag.ContentFiles));
        //    Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Build));
        //    Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Native));
        //    Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Runtime));
        //    Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Compile));
        //}

        //[Fact]
        //public void IncludeType_ProjectJsonIncludeRuntimeOnly()
        //{
        //    // Arrange
        //    JObject configJson = new JObject();
        //    var packageA = new JObject();
        //    var dependencies = new JObject();
        //    var frameworks = new JObject();
        //    var net46 = new JObject();
        //    frameworks.Add("net46", net46);
        //    configJson.Add("frameworks", frameworks);
        //    configJson.Add("dependencies", dependencies);
        //    dependencies.Add("packageA", packageA);
        //    packageA.Add("version", "1.0.0");
        //    packageA.Add("include", "Runtime");

        //    // Act
        //    var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", "project.json");
        //    var result = spec.Dependencies.Single();

        //    // Assert
        //    Assert.True(result.HasFlag(LibraryIncludeTypeFlag.Runtime));
        //    Assert.False(result.HasFlag(LibraryIncludeTypeFlag.ContentFiles));
        //    Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Build));
        //    Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Native));
        //    Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Compile));
        //}

        //[Fact]
        //public void IncludeType_ProjectJsonIncludeContentFilesOnly()
        //{
        //    // Arrange
        //    JObject configJson = new JObject();
        //    var packageA = new JObject();
        //    var dependencies = new JObject();
        //    var frameworks = new JObject();
        //    var net46 = new JObject();
        //    frameworks.Add("net46", net46);
        //    configJson.Add("frameworks", frameworks);
        //    configJson.Add("dependencies", dependencies);
        //    dependencies.Add("packageA", packageA);
        //    packageA.Add("version", "1.0.0");
        //    packageA.Add("include", "contentFiles");

        //    // Act
        //    var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", "project.json");
        //    var result = spec.Dependencies.Single();

        //    // Assert
        //    Assert.True(result.HasFlag(LibraryIncludeTypeFlag.ContentFiles));
        //    Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Runtime));
        //    Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Build));
        //    Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Native));
        //    Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Runtime));
        //    Assert.False(result.HasFlag(LibraryIncludeTypeFlag.Compile));
        //}

        //[Fact]
        //public async Task IncludeType_IncludeRuntimeOnly()
        //{
        //    // Arrange
        //    var logger = new TestLogger();
        //    var framework = "net46";

        //    JObject configJson = JObject.Parse(@"{
        //        ""dependencies"": {
        //            ""packageA"": {
        //                ""version"": ""1.0.0"",
        //                ""include"": ""Runtime""
        //            }
        //        },
        //        ""frameworks"": {
        //        ""_FRAMEWORK_"": {}
        //        }
        //    }".Replace("_FRAMEWORK_", framework));

        //    // Act
        //    var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", "project.json");

        //    var result = await StandardSetup(framework, logger, configJson);

        //    var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

        //    var targetLibrary = target.Libraries.FirstOrDefault(lib => lib.Name == "packageA");

        //    var packageAType = result.GetAllInstalled().FirstOrDefault(package => package.Name == "packageA").Type;

        //    // Assert
        //    Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
        //    Assert.Equal(0, logger.Errors);
        //    Assert.Equal(0, logger.Warnings);
        //    Assert.True(IsEmptyFolder(targetLibrary.ContentFiles));
        //    Assert.True(IsEmptyFolder(targetLibrary.NativeLibraries));
        //    Assert.Equal(1, targetLibrary.RuntimeAssemblies.Count);
        //    Assert.Equal(1, targetLibrary.FrameworkAssemblies.Count);
        //    Assert.Equal(1, targetLibrary.Dependencies.Count);
        //    Assert.True(IsEmptyFolder(targetLibrary.CompileTimeAssemblies));
        //}

        [Theory]
        [InlineData(@"{
                        ""dependencies"": {
                            ""packageA"": {
                                ""version"": ""1.0.0"",
                                ""suppressParent"": ""all""
                            },
                            ""packageB"": {
                                ""version"": ""1.0.0"",
                                ""suppressParent"": ""all"",
                                ""exclude"": ""contentFiles""
                            }
                        },
                        ""frameworks"": {
                            ""net46"": {}
                        }
                    }")]
        [InlineData(@"{
                        ""dependencies"": {
                            ""packageA"": {
                                ""version"": ""1.0.0""
                            },
                            ""packageB"": {
                                ""version"": ""1.0.0"",
                                ""exclude"": ""contentFiles""
                            }
                        },
                        ""frameworks"": {
                            ""net46"": {}
                        }
                    }")]
        [InlineData(@"{
                        ""dependencies"": {
                            ""packageA"": {
                                ""version"": ""1.0.0""
                            },
                            ""packageB"": {
                                ""version"": ""1.0.0"",
                                ""include"": ""build,compile,runtime,native""
                            }
                        },
                        ""frameworks"": {
                            ""net46"": {}
                        }
                    }")]
        [InlineData(@"{
                        ""dependencies"": {
                            ""packageA"": {
                                ""version"": ""1.0.0"",
                                ""include"": ""build,compile,runtime,native,contentfiles""
                            }
                        },
                        ""frameworks"": {
                            ""net46"": {}
                        }
                    }")]
        [InlineData(@"{
                        ""dependencies"": {
                            ""packageA"": {
                                ""version"": ""1.0.0"",
                                ""include"": ""all"",
                                ""exclude"": ""none""
                            }
                        },
                        ""frameworks"": {
                            ""net46"": {}
                        }
                    }")]
        [InlineData(@"{
                        ""dependencies"": {
                            ""packageA"": {
                                ""version"": ""1.0.0"",
                                ""exclude"": ""none""
                            }
                        },
                        ""frameworks"": {
                            ""net46"": {}
                        }
                    }")]
        [InlineData(@"{
                        ""dependencies"": {
                            ""packageA"": {
                                ""version"": ""1.0.0"",
                                ""include"": ""all""
                            }
                        },
                        ""frameworks"": {
                            ""net46"": {}
                        }
                    }")]
        [InlineData(@"{
                        ""dependencies"": {
                            ""packageA"": ""1.0.0""
                        },
                        ""frameworks"": {
                            ""net46"": {}
                        }
                    }")]
        public async Task IncludeType_SingleProjectEquivalentToTheDefault(string projectJson)
        {
            // Arrange
            var logger = new TestLogger();
            var framework = "net46";
            var workingDir = TestFileSystemUtility.CreateRandomTestFolder();

            // Act
            var result = await StandardSetup(workingDir, logger, projectJson);

            var target = result.LockFile.GetTarget(NuGetFramework.Parse(framework), null);

            var targets = target.Libraries.ToDictionary(lib => lib.Name);

            var a = targets["packageA"];
            var b = targets["packageB"];

            var msbuildTargets = GetInstalledTargets(workingDir);

            // Assert
            Assert.Equal(0, result.CompatibilityCheckResults.Sum(checkResult => checkResult.Issues.Count));
            Assert.Equal(0, logger.Errors);
            Assert.Equal(0, logger.Warnings);

            Assert.Equal(1, GetNonEmptyCount(a.ContentFiles));
            Assert.Equal(1, GetNonEmptyCount(a.NativeLibraries));
            Assert.Equal(1, GetNonEmptyCount(a.RuntimeAssemblies));
            Assert.Equal(1, a.FrameworkAssemblies.Count);
            Assert.Equal(1, a.Dependencies.Count);

            Assert.Equal(0, GetNonEmptyCount(b.ContentFiles));
            Assert.Equal(1, GetNonEmptyCount(b.NativeLibraries));
            Assert.Equal(1, GetNonEmptyCount(b.RuntimeAssemblies));
            Assert.Equal(1, b.FrameworkAssemblies.Count);
            Assert.Equal(0, b.Dependencies.Count);

            Assert.Equal(2, msbuildTargets["TestProject"].Count);
        }

        private async Task<RestoreResult> StandardSetup(
            string workingDir,
            NuGet.Logging.ILogger logger,
            string configJson)
        {
            // Arrange
            _testFolders.Add(workingDir);

            var repository = Path.Combine(workingDir, "repository");
            Directory.CreateDirectory(repository);
            var projectDir = Path.Combine(workingDir, "project");
            Directory.CreateDirectory(projectDir);
            var packagesDir = Path.Combine(workingDir, "packages");
            Directory.CreateDirectory(packagesDir);
            var testProjectDir = Path.Combine(projectDir, "TestProject");
            Directory.CreateDirectory(testProjectDir);

            CreateAToB(repository);

            var sources = new List<PackageSource>();
            sources.Add(new PackageSource(repository));

            var specPath = Path.Combine(testProjectDir, "project.json");
            var spec = JsonPackageSpecReader.GetPackageSpec(configJson, "TestProject", specPath);

            var request = new RestoreRequest(spec, sources, packagesDir);

            request.LockFilePath = Path.Combine(testProjectDir, "project.lock.json");

            var command = new RestoreCommand(logger, request);

            // Act
            var result = await command.ExecuteAsync();
            result.Commit(logger);

            return result;
        }

        private static void CreateAToB(string repositoryDir)
        {
            var b = new TestPackage()
            {
                Id = "packageB",
                Version = "1.0.0"
            };

            var a = new TestPackage()
            {
                Id = "packageA",
                Version = "1.0.0"
            };
            a.Dependencies.Add(b);

            var packages = new List<TestPackage>()
            {
                a
            };

            CreatePackages(packages, repositoryDir);
        }

        private static void CreateXYZ(string repositoryDir)
        {
            CreateXYZ(repositoryDir, string.Empty, string.Empty);
        }

        private static void CreateXYZ(string repositoryDir, string include, string exclude)
        {
            var z = new TestPackage()
            {
                Id = "packageZ",
                Version = "1.0.0"
            };

            var y = new TestPackage()
            {
                Id = "packageY",
                Version = "1.0.0",
                Include = include,
                Exclude = exclude
            };
            y.Dependencies.Add(z);

            var x = new TestPackage()
            {
                Id = "packageX",
                Version = "1.0.0",
                Include = include,
                Exclude = exclude
            };
            x.Dependencies.Add(y);

            var packages = new List<TestPackage>()
            {
                x
            };

            CreatePackages(packages, repositoryDir);
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
            Packaging.Core.PackageDependency dependency)
        {
            return CreateFullPackage(repositoryDir, id, version, 
                new List<Packaging.Core.PackageDependency>() { dependency });
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

                var nuspecXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                        <package>
                        <metadata>
                            <id>{id}</id>
                            <version>{version}</version>
                            <title />
                            <frameworkAssemblies>
                                <frameworkAssembly assemblyName=""System.Runtime"" />
                            </frameworkAssemblies>
                            <contentFiles>
                                <files include=""cs/net45/config/config.xml"" buildAction=""none"" />
                                <files include=""cs/net45/config/config.xml"" copyToOutput=""true"" flatten=""false"" />
                                <files include=""cs/net45/images/image.jpg"" buildAction=""embeddedresource"" />
                            </contentFiles>
                        </metadata>
                        </package>";

                var xml = XDocument.Parse(nuspecXml);

                if (dependencies.Any())
                {
                    var metadata = xml.Element(XName.Get("package")).Element(XName.Get("metadata"));

                    var dependenciesNode = new XElement(XName.Get("dependencies"));
                    var groupNode = new XElement(XName.Get("group"));
                    dependenciesNode.Add(groupNode);
                    metadata.Add(dependenciesNode);

                    foreach (var dependency in dependencies)
                    {
                        var node = new XElement(XName.Get("dependency"));
                        groupNode.Add(node);
                        node.Add(new XAttribute(XName.Get("id"), dependency.Id));
                        node.Add(new XAttribute(XName.Get("version"), dependency.VersionRange.ToNormalizedString()));

                        if (dependency.Include.Count > 0)
                        {
                            node.Add(new XAttribute(XName.Get("include"), string.Join(",", dependency.Include)));
                        }

                        if (dependency.Exclude.Count > 0)
                        {
                            node.Add(new XAttribute(XName.Get("exclude"), string.Join(",", dependency.Exclude)));
                        }
                    }
                }

                zip.AddEntry($"{id}.nuspec", xml.ToString(), Encoding.UTF8);
            }

            return file;
        }

        private class TestPackage
        {
            public string Id { get; set; } = "packageA";
            public string Version { get; set; } = "1.0.0";
            public List<TestPackage> Dependencies { get; set; } = new List<TestPackage>();
            public string Include { get; set; } = string.Empty;
            public string Exclude { get; set; } = string.Empty;

            public PackageIdentity Identity
            {
                get
                {
                    return new PackageIdentity(Id, NuGetVersion.Parse(Version));
                }
            }
        }

        private static void CreatePackages(List<TestPackage> packages, string repositoryPath)
        {
            var done = new HashSet<PackageIdentity>();
            var toCreate = new Stack<TestPackage>(packages);

            while (toCreate.Count > 0)
            {
                var package = toCreate.Pop();

                if (done.Add(package.Identity))
                {
                    var dependencies = package.Dependencies.Select(e => 
                        new Packaging.Core.PackageDependency(
                            e.Id,
                            VersionRange.Parse(e.Version),
                            e.Include.Split(',').ToList(),
                            e.Exclude.Split(',').ToList()));

                    CreateFullPackage(
                        repositoryPath,
                        package.Id,
                        package.Version,
                        dependencies);

                    foreach (var dep in package.Dependencies)
                    {
                        toCreate.Push(dep);
                    }
                }
            }
        }

        private bool IsEmptyFolder(IList<LockFileItem> group)
        {
            return group.SingleOrDefault()?.Path.EndsWith("/_._") == true;
        }

        private int GetNonEmptyCount(IList<LockFileItem> group)
        {
            return group.Where(e => !e.Path.EndsWith("/_._")).Count();
        }

        private static Dictionary<string, HashSet<string>> GetInstalledTargets(string workingDir)
        {
            var result = new Dictionary<string, HashSet<string>>();
            var projectDir = new DirectoryInfo(Path.Combine(workingDir, "project"));

            foreach (var dir in projectDir.GetDirectories())
            {
                result.Add(dir.Name, new HashSet<string>());

                var targets = dir.GetFiles("*.nuget.targets").SingleOrDefault();

                if (targets != null)
                {
                    var xml = XDocument.Load(targets.OpenRead());

                    foreach (var package in xml.Descendants()
                        .Where(node => node.Name.LocalName == "Import")
                        .Select(node => node.Attribute(XName.Get("Project")))
                        .Select(file => Path.GetFileNameWithoutExtension(file.Value)))
                    {
                        result[dir.Name].Add(package);
                    }
                }
            }

            return result;
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
