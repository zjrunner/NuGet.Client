using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NuGet.DependencyResolver;
using NuGet.LibraryModel;
using NuGet.ProjectModel;

namespace NuGet.Commands
{
    internal static class IncludeFlagUtils
    {
        internal static Dictionary<string, LibraryIncludeType> FlattenDependencyTypes(
            IEnumerable<GraphNode<RemoteResolveResult>> graphs,
            PackageSpec spec)
        {
            var result = new Dictionary<string, LibraryIncludeType>(StringComparer.OrdinalIgnoreCase);

            var directDependencies = new SortedSet<string>(
                spec.Dependencies.Select(dependency => dependency.Name),
                StringComparer.OrdinalIgnoreCase);

            // Walk all graphs and merge the results
            foreach (var graph in graphs)
            {
                foreach (var node in graph.InnerNodes)
                {
                    var flags = GetDependencyType(graph, node);

                    FlattenDependencyTypes(result, node, flags);
                }

                // Override flags for direct dependencies
                foreach (var node in graph.InnerNodes)
                {
                    if (directDependencies.Contains(node.Key.Name))
                    {
                        var flags = GetDependencyType(graph, node);

                        result[node.Key.Name] = flags;
                    }
                }
            }

            return result;
        }

        private static void FlattenDependencyTypes(
            Dictionary<string, LibraryIncludeType> result,
            GraphNode<RemoteResolveResult> root,
            LibraryIncludeType dependencyType)
        {
            // Intersect on the way down
            foreach (var child in root.InnerNodes)
            {
                var childType = GetDependencyType(root, child);

                var typeIntersection = dependencyType.Intersect(childType);

                FlattenDependencyTypes(result, child, typeIntersection);
            }

            // Combine results on the way up
            LibraryIncludeType currentTypes;
            if (result.TryGetValue(root.Key.Name, out currentTypes))
            {
                result[root.Key.Name] = currentTypes.Combine(dependencyType);
            }
            else
            {
                result.Add(root.Key.Name, dependencyType);
            }
        }

        /// <summary>
        /// Find the flags for a node. 
        /// Include - Exclude - ParentExclude
        /// </summary>
        private static LibraryIncludeType GetDependencyType(
            GraphNode<RemoteResolveResult> parent,
            GraphNode<RemoteResolveResult> child)
        {
            var match = parent.Item.Data.Dependencies.FirstOrDefault(dependency =>
                dependency.Name.Equals(child.Key.Name, StringComparison.OrdinalIgnoreCase));

            Debug.Assert(match != null, "The graph contains a dependency that the node does not list");

            var flags = match.IncludeType;

            // Unless the root project is the grand parent here, the suppress flag should be applied directly to the 
            // child since it has no effect on the parent.
            if (parent.OuterNode != null)
            {
                flags.Except(match.ParentExcludeType);
            }

            return flags;
        }
    }
}
