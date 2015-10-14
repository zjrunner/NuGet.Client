using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.ProjectModel
{
    /// <summary>
    /// Stores a list of LockFileItems along with a property.
    /// This is used for codeLanguage groups with shared items.
    /// </summary>
    public class LockFileItemGroup : IEquatable<LockFileItemGroup>
    {
        /// <summary>
        /// Group property key.
        /// </summary>
        public string Property { get; }

        /// <summary>
        /// Group property value.
        /// </summary>
        public string PropertyValue { get; }

        /// <summary>
        /// Lock file items in this group.
        /// </summary>
        public IList<LockFileItem> Items { get; }

        public LockFileItemGroup(string property, string propertyValue, IList<LockFileItem> items)
        {
            Property = property;
            PropertyValue = propertyValue;
            Items = items;
        }

        public bool Equals(LockFileItemGroup other)
        {
            if (other == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Property, other.Property, StringComparison.Ordinal)
                && string.Equals(PropertyValue, other.PropertyValue, StringComparison.Ordinal)
                && Items.OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
                    .SequenceEqual(other.Items.OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LockFileItemGroup);
        }

        public override int GetHashCode()
        {
            var combiner = new HashCodeCombiner();

            combiner.AddObject(Property);
            combiner.AddObject(PropertyValue);

            foreach (var item in Items.OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase))
            {
                combiner.AddObject(item);
            }

            return combiner.CombinedHash;
        }
    }
}
