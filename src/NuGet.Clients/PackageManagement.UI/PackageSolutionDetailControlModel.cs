// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NuGet.ProjectManagement;
using NuGet.Versioning;
using NuGet.VisualStudio;

namespace NuGet.PackageManagement.UI
{
    public class PackageSolutionDetailControlModel : DetailControlModel
    {
        private readonly ISolutionManager _solutionManager;

        private IEnumerable<IVsPackageManagerProvider> _packageManagerProviders;

        public override bool IsSolution
        {
            get { return true; }
        }

        protected override void OnCurrentPackageChanged()
        {
            UpdateInstalledVersions();
        }

        public override void Refresh()
        {
            UpdateInstalledVersions();
        }

        private void UpdateInstalledVersions()
        {
            var hash = new HashSet<NuGetVersion>();

            ForeachProject((project) =>
            {
                var installedVersion = GetInstalledPackage(project.Project, Id);
                if (installedVersion != null)
                {
                    project.InstalledVersion = installedVersion.PackageIdentity.Version;
                    hash.Add(installedVersion.PackageIdentity.Version);
                }
                else
                {
                    project.InstalledVersion = null;
                }
            });

            InstalledVersionsCount = hash.Count;
        }

        private NuGetVersion GetInstalledVersion(NuGetProject project, string packageId)
        {
            return NuGetUIThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                var installedPackages = await project.GetInstalledPackagesAsync(CancellationToken.None);
                var installedPackage = installedPackages
                    .Where(p => StringComparer.OrdinalIgnoreCase.Equals(p.PackageIdentity.Id, packageId))
                    .FirstOrDefault();

                if (installedPackage != null)
                {
                    return installedPackage.PackageIdentity.Version;
                }
                else
                {
                    return null;
                }
            });
        }

        private void ForeachProject(Action<ProjectNodeModel> action)
        {
            var queue = new Queue<NodeModelBase>();
            foreach (var node in ProjectNodes)
            {
                queue.Enqueue(node);
            }

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node is ProjectNodeModel)
                {
                    var projectNode = (ProjectNodeModel)node;
                    action(projectNode);
                }
                else
                {
                    // node is FolderNodeModel
                    var folderNode = (FolderNodeModel)node;
                    foreach (var child in folderNode.Children)
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }

        /// <summary>
        /// This method is called from several methods that are called from properties and LINQ queries
        /// It is likely not called more than once in an action. So, consolidating the use of JTF.Run in this method
        /// </summary>
        private static Packaging.PackageReference GetInstalledPackage(NuGetProject project, string id)
        {
            return NuGetUIThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    var installedPackages = await project.GetInstalledPackagesAsync(CancellationToken.None);
                    var installedPackage = installedPackages
                        .Where(p => StringComparer.OrdinalIgnoreCase.Equals(p.PackageIdentity.Id, id))
                        .FirstOrDefault();
                    return installedPackage;
                });
        }

        protected override void CreateVersions()
        {
            _versions = new List<VersionForDisplay>();
            var allVersions = _allPackageVersions.OrderByDescending(v => v);
            var latestPrerelease = allVersions.FirstOrDefault(v => v.IsPrerelease);
            var latestStableVersion = allVersions.FirstOrDefault(v => !v.IsPrerelease);

            if (latestPrerelease != null
                && (latestStableVersion == null || latestPrerelease > latestStableVersion))
            {
                _versions.Add(new VersionForDisplay(latestPrerelease, Resources.Version_LatestPrerelease));
            }

            if (latestStableVersion != null)
            {
                _versions.Add(new VersionForDisplay(latestStableVersion, Resources.Version_LatestStable));
            }

            // add a separator
            if (_versions.Count > 0)
            {
                _versions.Add(null);
            }

            foreach (var version in allVersions)
            {
                _versions.Add(new VersionForDisplay(version, string.Empty));
            }

            SelectVersion();
            OnPropertyChanged("Versions");
        }

        // the count of different installed versions
        private int _installedVersionsCount;

        public int InstalledVersionsCount
        {
            get
            {
                return _installedVersionsCount;
            }
            set
            {
                _installedVersionsCount = value;
                OnPropertyChanged(nameof(InstalledVersionsCount));
            }
        }

        public PackageSolutionDetailControlModel(
            ISolutionManager solutionManager,
            IEnumerable<NuGetProject> projects,
            IEnumerable<IVsPackageManagerProvider> packageManagerProviders)
            :
                base(projects)
        {
            _solutionManager = solutionManager;
            _solutionManager.NuGetProjectAdded += (_, __) => RefreshProjectListAfterProjectAddedRemovedOrRenamed();
            _solutionManager.NuGetProjectRemoved += (_, __) => RefreshProjectListAfterProjectAddedRemovedOrRenamed();
            _solutionManager.NuGetProjectRenamed += (_, __) => RefreshProjectListAfterProjectAddedRemovedOrRenamed();
            _packageManagerProviders = packageManagerProviders;

            CreateSolutionNode();
        }

        // The solutin hierarchy. It only contains the solution node.
        private List<NodeModelBase> _projectNodes;

        public IEnumerable<NodeModelBase> ProjectNodes
        {
            get
            {
                return _projectNodes;
            }
            set
            {
                _projectNodes = value.ToList();
                OnPropertyChanged(nameof(ProjectNodes));
            }
        }

        private void CreateSolutionNode()
        {
            var nodes = _solutionManager.GetSolutionHierarchy();

            var nodeModels = new List<NodeModelBase>();
            foreach (var node in nodes)
            {
                var nodeModel = CreateNodeModel(node);
                nodeModels.Add(nodeModel);
            }

            nodeModels.Sort(NodeComparer.Default);

            // create the solution node
            var solutionNode = new FolderNodeModel(
                "solution",
                null,
                nodeModels);

            ProjectNodes = new List<NodeModelBase> { solutionNode };
        }

        private NodeModelBase CreateNodeModel(NodeBase node)
        {
            var projectNode = node as ProjectNode;
            if (projectNode != null)
            {
                var name = projectNode.Project.GetMetadata<string>(NuGetProjectMetadataKeys.Name);
                var dteUniqueName = projectNode.Project.GetMetadata<string>(
                    NuGetProjectMetadataKeys.DTEUniqueName);
                var icon = ProjectUtilities.GetImage(dteUniqueName);
                var projectNodeModel = new ProjectNodeModel(
                    name,
                    projectNode.Project,
                    icon);
                return projectNodeModel;
            }
            else
            {
                // the node is a project node, so it must be a folder node.
                var folderNode = (FolderNode)node;
                List<NodeModelBase> children = new List<NodeModelBase>();
                foreach (var child in folderNode.Children)
                {
                    var childModel = CreateNodeModel(child);
                    children.Add(childModel);
                }

                children.Sort(NodeComparer.Default);
                var folderNodeModel = new FolderNodeModel(
                    folderNode.Name,
                    folderNode.UniqueName,
                    children);
                return folderNodeModel;
            }
        }

        // Refresh the project list after a project is added/removed/renamed.
        private void RefreshProjectListAfterProjectAddedRemovedOrRenamed()
        {
            _nugetProjects = _solutionManager.GetNuGetProjects();

            /* !!!
            RefreshAllProjectList();
            RefreshProjectList(); */
        }

        private static bool IsInstalled(NuGetProject project, string id)
        {
            var packageReference = GetInstalledPackage(project, id);
            return packageReference != null;
        }

        /* !!!
        protected override async void OnCurrentPackageChanged()
        {
            if (_searchResultPackage == null)
            {
                return;
            }

            foreach (var p in _allProjects)
            {
                if (_packageManagerProviders.Any())
                {
                    p.Providers = await AlternativePackageManagerProviders.CalculateAlternativePackageManagersAsync(
                        _packageManagerProviders,
                        Id,
                        p.NuGetProject);
                }
            }
        } */

        private bool? _checkboxState;

        public bool? CheckboxState
        {
            get { return _checkboxState; }
            set
            {
                _checkboxState = value;
                OnPropertyChanged("CheckboxState");
            }
        }

        private string _selectCheckboxText;

        // The text of the project selection checkbox
        public string SelectCheckboxText
        {
            get { return _selectCheckboxText; }
            set
            {
                _selectCheckboxText = value;
                OnPropertyChanged("SelectCheckboxText");
            }
        }

        /* !!!

        private void UpdateSelectCheckbox()
        {
            if (Projects == null)
            {
                return;
            }

            _updatingCheckbox = true;
            var countTotal = Projects.Count(p => p.Enabled);

            SelectCheckboxText = string.Format(
                CultureInfo.CurrentCulture,
                Resources.Checkbox_ProjectSelection,
                countTotal);

            var countSelected = Projects.Count(p => p.Selected);
            if (countSelected == 0)
            {
                CheckboxState = false;
            }
            else if (countSelected == countTotal)
            {
                CheckboxState = true;
            }
            else
            {
                CheckboxState = null;
            }
            _updatingCheckbox = false;
        }

        internal void UncheckAllProjects()
        {
            if (_updatingCheckbox)
            {
                return;
            }

            Projects.ForEach(p =>
                {
                    if (p.Enabled)
                    {
                        p.Selected = false;
                    }
                });
        }

        internal void CheckAllProjects()
        {
            if (_updatingCheckbox)
            {
                return;
            }

            Projects.ForEach(p =>
                {
                    if (p.Enabled)
                    {
                        p.Selected = true;
                    }
                });

            OnPropertyChanged("Projects");
        } */

        public override IEnumerable<NuGetProject> GetSelectedProjects(NuGetProjectActionType action)
        {
            var selectedProjects = new List<NuGetProject>();
            ForeachProject(
                (project) =>
                {
                    if (project.IsSelected == false)
                    {
                        return;
                    }

                    if (action == NuGetProjectActionType.Install)
                    {
                        selectedProjects.Add(project.Project);
                    }
                    else
                    {
                        // for uninstall, the package must be already installed
                        if (project.InstalledVersion != null)
                        {
                            selectedProjects.Add(project.Project);
                        }
                    }
                });

            return selectedProjects;
        }
    }
}