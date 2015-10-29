using NuGet.DependencyResolver;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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

                    var childResult = FlattenDependencyTypes(node, flags);

                    // Merge all flags
                    MergeFlags(result, childResult);
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

        internal static Dictionary<string, LibraryIncludeType> FlattenDependencyTypes(
            GraphNode<RemoteResolveResult> root,
            LibraryIncludeType dependencyType)
        {
            var result = new Dictionary<string, LibraryIncludeType>(StringComparer.OrdinalIgnoreCase);

            // Add in the current nodes to the result
            result.Add(root.Key.Name, dependencyType);

            foreach (var child in root.InnerNodes)
            {
                var childType = GetDependencyType(root, child);

                // Intersect on the way down
                var typeIntersection = dependencyType.Intersect(childType);

                var childResult = FlattenDependencyTypes(child, typeIntersection);

                // Combine results on the way up
                MergeFlags(result, childResult);
            }

            return result;
        }

        private static void MergeFlags(
            Dictionary<string, LibraryIncludeType> main,
            Dictionary<string, LibraryIncludeType> childResult)
        {
            LibraryIncludeType currentTypes;

            foreach (var pair in childResult)
            {
                if (main.TryGetValue(pair.Key, out currentTypes))
                {
                    // Combine results on the way up
                    main[pair.Key] = currentTypes.Combine(pair.Value);
                }
                else
                {
                    main.Add(pair.Key, pair.Value);
                }
            }
        }

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
