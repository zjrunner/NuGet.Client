// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace NuGet.LibraryModel
{
    public class LibraryIncludeTypeFlag : IEquatable<LibraryIncludeTypeFlag>, IComparable<LibraryIncludeTypeFlag>
    {
        private static ConcurrentDictionary<string, LibraryIncludeTypeFlag> _flags 
            = new ConcurrentDictionary<string, LibraryIncludeTypeFlag>();
        private readonly string _value;

        public static readonly LibraryIncludeTypeFlag None = Declare(nameof(None));
        public static readonly LibraryIncludeTypeFlag All = Declare(nameof(All));
        public static readonly LibraryIncludeTypeFlag ContentFiles = Declare(nameof(ContentFiles));
        public static readonly LibraryIncludeTypeFlag Build = Declare(nameof(Build));
        public static readonly LibraryIncludeTypeFlag Native = Declare(nameof(Native));
        public static readonly LibraryIncludeTypeFlag Compile = Declare(nameof(Compile));
        public static readonly LibraryIncludeTypeFlag Runtime = Declare(nameof(Runtime));

        private LibraryIncludeTypeFlag(string value)
        {
            _value = value;
        }

        public static LibraryIncludeTypeFlag Declare(string keyword)
        {
            return _flags.GetOrAdd(keyword, x => new LibraryIncludeTypeFlag(x));
        }

        public override string ToString()
        {
            return _value;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LibraryIncludeTypeFlag);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_value);
        }

        public bool Equals(LibraryIncludeTypeFlag other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            return StringComparer.OrdinalIgnoreCase.Equals(_value, other.ToString());
        }

        public int CompareTo(LibraryIncludeTypeFlag other)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(_value, other.ToString());
        }
    }
}
