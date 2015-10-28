// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.VisualStudio;
using NuGet.Versioning;
using NuGet.VisualStudio;
using Mvs = Microsoft.VisualStudio.Shell;

namespace NuGet.PackageManagement.UI
{
    internal class InstalledTabLoader : ILoader
    {
        private readonly SourceRepository _sourceRepository;

        private readonly NuGetProject[] _projects;

        private readonly NuGetPackageManager _packageManager;

        private readonly PackageLoaderOption _option;

        // Indicates whether the loader is created by solution package manager.
        private readonly bool _isSolution;

        private readonly string _searchText;

        private readonly string LogEntrySource = "NuGet Package Manager";

        // The list of packages that have updates available
        private List<UISearchMetadata> _packagesWithUpdates;

        private IEnumerable<IVsPackageManagerProvider> _packageManagerProviders;

        public InstalledTabLoader(
            IEnumerable<IVsPackageManagerProvider> providers)
        {
            _packageManagerProviders = providers;
            _installedPackages = new Dictionary<string, HashSet<NuGetVersion>>(
                StringComparer.OrdinalIgnoreCase);
        }

        private Task _loadingTask;
        private CancellationTokenSource _cancellationTokenSource;

        public void StartLoadingTask()
        {
            // cancel existing task
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _loadingTask = LoadAsync(_cancellationTokenSource.Token);
        }

        /*
        // The list of all installed packages. This variable is used for the package status calculation.
        private readonly HashSet<PackageIdentity> _installedPackages;

        private readonly HashSet<string> _installedPackageIds;
        */

        // the key is the package id, the value is the installed versions.
        private Dictionary<string, HashSet<NuGetVersion>> _installedPackages;

        private List<UISearchMetadata> _packagesWithMetadata;

        private List<PackageItemListViewModel> _result;

        private async Task LoadAsync(CancellationToken cancellationToken)
        {
            // =================================
            await GetInstalledPackagesAsync(token: cancellationToken);

            // ===========================
            // Get metadata
            var results = new List<UISearchMetadata>();
            var localResource = await _packageManager.PackagesFolderSourceRepository
                .GetResourceAsync<UIMetadataResource>(cancellationToken);

            // UIMetadataResource may not be available
            // Given that this is the 'Installed' filter, we ignore failures in reaching the remote server
            // Instead, we will use the local UIMetadataResource
            UIMetadataResource metadataResource;
            try
            {
                metadataResource =
                _sourceRepository == null ?
                    null :
                    await _sourceRepository.GetResourceAsync<UIMetadataResource>(cancellationToken);
            }
            catch (Exception ex)
            {
                metadataResource = null;
                // Write stack to activity log
                Mvs.ActivityLog.LogError(LogEntrySource, ex.ToString());
            }

            // group installed packages
            var groupedPackages = _installedPackages.Select(
                p => new PackageIdentity(p.Key, p.Value.Max()));

            var tasks = new List<Task<UISearchMetadata>>();
            foreach (var installedPackage in groupedPackages)
            {
                var packageIdentity = installedPackage;

                tasks.Add(
                    Task.Run(() =>
                        GetPackageMetadataAsync(localResource,
                                                metadataResource,
                                                packageIdentity,
                                                cancellationToken)));
            }

            await Task.WhenAll(tasks);
            foreach (var task in tasks)
            {
                results.Add(task.Result);
            }

            _packagesWithMetadata = results;

            // ===================
            // get other data in background
            GetDataInBackground(cancellationToken);
        }

        private void GetDataInBackground(CancellationToken cancellationToken)
        {
            _result = new List<PackageItemListViewModel>();
            foreach (var package in _packagesWithMetadata)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packageItem = new PackageItemListViewModel();
                packageItem.Id = package.Identity.Id;
                packageItem.Version = package.Identity.Version;
                packageItem.IconUrl = package.IconUrl;
                packageItem.Author = package.Author;
                packageItem.DownloadCount = package.DownloadCount;

                // set InstalledVersion
                if (!_isSolution)
                {
                    HashSet<NuGetVersion> versions;
                    if (_installedPackages.TryGetValue(packageItem.Id, out versions))
                    {
                        packageItem.InstalledVersion = versions.FirstOrDefault();
                    }
                }

                // load Versions
                packageItem.Versions = new Lazy<Task<IEnumerable<VersionInfo>>>(async () =>
                {
                    var versions = await package.Versions.Value;
                    var filteredVersions = versions
                        .Where(v => !v.Version.IsPrerelease || _option.IncludePrerelease)
                        .ToList();

                    if (!filteredVersions.Any(v => v.Version == packageItem.Version))
                    {
                        filteredVersions.Add(new VersionInfo(packageItem.Version, downloadCount: null));
                    }

                    return filteredVersions;
                });

                // Start background loader
                packageItem.BackgroundLoader = new Lazy<Task<BackgroundLoaderResult>>(
                    () => BackgroundLoad(packageItem, packageItem.Versions));

                if (!_isSolution && _packageManagerProviders.Any())
                {
                    packageItem.ProvidersLoader = new Lazy<Task<AlternativePackageManagerProviders>>(
                        () => AlternativePackageManagerProviders.CalculateAlternativePackageManagersAsync(
                            _packageManagerProviders,
                            packageItem.Id,
                            _projects[0]));
                }
                
                // filter out prerelease version when needed.
                if (packageItem.Version.IsPrerelease &&
                    !_option.IncludePrerelease)
                {
                    continue;
                }

                _result.Add(packageItem);
            }
        }

        // !!!
        /*

            _sourceRepository = sourceRepository;
            _isSolution = isSolution;
            _packageManager = packageManager;
            _projects = projects.ToArray();
            _packageManagerProviders = providers;
            _option = option;
            _searchText = searchText;

            LoadingMessage = string.IsNullOrWhiteSpace(searchText) ?
                Resources.Text_Loading :
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Text_Searching,
                    searchText);

            _installedPackages = new HashSet<PackageIdentity>(PackageIdentity.Comparer);
            _installedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        } */

        public string LoadingMessage { get; }
        
        private async Task GetInstalledPackagesAsync(CancellationToken token)
        {
            _installedPackages.Clear();

            foreach (var project in _projects)
            {
                foreach (var package in (await project.GetInstalledPackagesAsync(token)))
                {
                    HashSet<NuGetVersion> versions;
                    if (!_installedPackages.TryGetValue(package.PackageIdentity.Id, out versions))
                    {
                        versions = new HashSet<NuGetVersion>();
                        _installedPackages[package.PackageIdentity.Id] = versions;
                    }

                    versions.Add(package.PackageIdentity.Version);
                }
            }
        }

        // Gets the package metadata from the local resource when the remote source
        // is not available.
        private static async Task<UISearchMetadata> GetPackageMetadataWhenRemoteSourceUnavailable(
            UIMetadataResource localResource,
            PackageIdentity identity,
            CancellationToken cancellationToken)
        {
            UIPackageMetadata packageMetadata = null;
            if (localResource != null)
            {
                var localMetadata = await localResource.GetMetadata(
                    identity.Id,
                    includePrerelease: true,
                    includeUnlisted: true,
                    token: cancellationToken);
                packageMetadata = localMetadata.FirstOrDefault(p => p.Identity.Version == identity.Version);
            }

            string summary = string.Empty;
            string title = identity.Id;
            string author = string.Empty;
            if (packageMetadata != null)
            {
                summary = packageMetadata.Summary;
                if (string.IsNullOrEmpty(summary))
                {
                    summary = packageMetadata.Description;
                }
                if (!string.IsNullOrEmpty(packageMetadata.Title))
                {
                    title = packageMetadata.Title;
                }

                author = string.Join(", ", packageMetadata.Authors);
            }

            var versions = new List<VersionInfo>
            {
                new VersionInfo(identity.Version, downloadCount: null)
            };

            return new UISearchMetadata(
                identity,
                title: title,
                summary: summary,
                author: author,
                downloadCount: packageMetadata?.DownloadCount,
                iconUrl: packageMetadata?.IconUrl,
                versions: ToLazyTask(versions),
                latestPackageMetadata: null);
        }

        private async Task<UISearchMetadata> GetPackageMetadataFromMetadataResourceAsync(
            UIMetadataResource metadataResource,
            PackageIdentity identity,
            CancellationToken cancellationToken)
        {
            var uiPackageMetadatas = await metadataResource.GetMetadata(
                identity.Id,
                _option.IncludePrerelease,
                includeUnlisted: false,
                token: cancellationToken);
            var packageMetadata = uiPackageMetadatas.FirstOrDefault(p => p.Identity.Version == identity.Version);

            string summary = string.Empty;
            string title = identity.Id;
            string author = string.Empty;
            if (packageMetadata != null)
            {
                summary = packageMetadata.Summary;
                if (string.IsNullOrEmpty(summary))
                {
                    summary = packageMetadata.Description;
                }
                if (!string.IsNullOrEmpty(packageMetadata.Title))
                {
                    title = packageMetadata.Title;
                }

                author = string.Join(", ", packageMetadata.Authors);
            }

            var versions = uiPackageMetadatas.OrderByDescending(m => m.Identity.Version)
                .Select(m => new VersionInfo(m.Identity.Version, m.DownloadCount));
            return new UISearchMetadata(
                identity,
                title: title,
                summary: summary,
                author: author,
                downloadCount: packageMetadata?.DownloadCount,
                iconUrl: packageMetadata?.IconUrl,
                versions: ToLazyTask(versions),
                latestPackageMetadata: packageMetadata);
        }

        /// <summary>
        /// Get the metadata of an installed package.
        /// </summary>
        /// <param name="localResource">The local resource, i.e. the package folder of the solution.</param>
        /// <param name="metadataResource">The remote metadata resource.</param>
        /// <param name="identity">The installed package.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The metadata of the package.</returns>
        private async Task<UISearchMetadata> GetPackageMetadataAsync(
            UIMetadataResource localResource,
            UIMetadataResource metadataResource,
            PackageIdentity identity,
            CancellationToken cancellationToken)
        {
            if (metadataResource == null)
            {
                return await GetPackageMetadataWhenRemoteSourceUnavailable(
                    localResource,
                    identity,
                    cancellationToken);
            }

            try
            {
                var metadata = await GetPackageMetadataFromMetadataResourceAsync(
                    metadataResource,
                    identity,
                    cancellationToken);

                // if the package does not exist in the remote source, NuGet should
                // try getting metadata from the local resource.
                if (String.IsNullOrEmpty(metadata.Summary) && localResource != null)
                {
                    return await GetPackageMetadataWhenRemoteSourceUnavailable(
                        localResource,
                        identity,
                        cancellationToken);
                }
                else
                {
                    return metadata;
                }
            }
            catch
            {
                // When a v2 package source throws, it throws an InvalidOperationException or WebException
                // When a v3 package source throws, it throws an HttpRequestException

                // The remote source is not available. NuGet should not fail but
                // should use the local resource instead.
                if (localResource != null)
                {
                    return await GetPackageMetadataWhenRemoteSourceUnavailable(
                        localResource,
                        identity,
                        cancellationToken);
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<LoadResult> LoadItemsAsync(int startIndex, CancellationToken cancellationToken)
        {
            if (_loadingTask == null)
            {
                return new LoadResult()
                {
                    Items = new List<PackageItemListViewModel>(),
                    HasMoreItems = false,
                    NextStartIndex = 1
                };
            }
                
            await _loadingTask;

            return new LoadResult()
            {
                Items = _result.Skip(startIndex).ToList(),
                HasMoreItems = false,
                NextStartIndex = _result.Count
            };
        }

        // Load info in the background
        private async Task<BackgroundLoaderResult> BackgroundLoad(
            PackageItemListViewModel package, Lazy<Task<IEnumerable<VersionInfo>>> versions)
        {
            HashSet<NuGetVersion> installedVersions;
            if (_installedPackages.TryGetValue(package.Id, out installedVersions))
            {
                var versionsUnwrapped = await versions.Value;
                var highestAvailableVersion = versionsUnwrapped
                    .Select(v => v.Version)
                    .Max();

                var lowestInstalledVersion = installedVersions.Min();

                if (VersionComparer.VersionRelease.Compare(lowestInstalledVersion, highestAvailableVersion) < 0)
                {
                    return new BackgroundLoaderResult()
                    {
                        LatestVersion = highestAvailableVersion,
                        InstalledVersion = lowestInstalledVersion,
                        Status = PackageStatus.UpdateAvailable
                    };
                }

                return new BackgroundLoaderResult()
                {
                    LatestVersion = null,
                    InstalledVersion = lowestInstalledVersion,
                    Status = PackageStatus.Installed
                };
            }

            // the package is not installed. In this case, the latest version is the version
            // of the search result.
            return new BackgroundLoaderResult()
            {
                LatestVersion = package.Version,
                InstalledVersion = null,
                Status = PackageStatus.NotInstalled
            };
        }

        private static Lazy<Task<IEnumerable<VersionInfo>>> ToLazyTask(IEnumerable<VersionInfo> versions)
        {
            return new Lazy<Task<IEnumerable<VersionInfo>>>(() => Task.FromResult(versions));
        }
    }
}