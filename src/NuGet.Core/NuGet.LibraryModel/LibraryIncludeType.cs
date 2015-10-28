// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace NuGet.LibraryModel
{
    public class LibraryIncludeType
    {
        private readonly LibraryIncludeTypeFlag[] _keywords;

        public static LibraryIncludeType Default;
        public static LibraryIncludeType All;

        static LibraryIncludeType()
        {
            Default = new LibraryIncludeType(LibraryIncludeTypeKeyword.Default.FlagsToAdd as LibraryIncludeTypeFlag[]);
            All = new LibraryIncludeType(LibraryIncludeTypeKeyword.All.FlagsToAdd as LibraryIncludeTypeFlag[]);
        }

        public LibraryIncludeType()
        {
            _keywords = new LibraryIncludeTypeFlag[0];
        }

        private LibraryIncludeType(LibraryIncludeTypeFlag[] flags)
        {
            _keywords = flags;
        }

        public bool Contains(LibraryIncludeTypeFlag flag)
        {
            return _keywords.Contains(flag);
        }

        public static LibraryIncludeType Parse(IEnumerable<string> keywords)
        {
            var type = new LibraryIncludeType();
            foreach (var keyword in keywords.Select(LibraryIncludeTypeKeyword.Parse))
            {
                type = type.Combine(keyword.FlagsToAdd, keyword.FlagsToRemove);
            }
            return type;
        }

        public LibraryIncludeType Combine(
            IEnumerable<LibraryIncludeTypeFlag> add,
            IEnumerable<LibraryIncludeTypeFlag> remove)
        {
            return new LibraryIncludeType(
                _keywords.Except(remove).Union(add).ToArray());
        }

        public LibraryIncludeType Intersect(LibraryIncludeType second)
        {
            return new LibraryIncludeType(_keywords.Intersect(second.Keywords).ToArray());
        }

        public LibraryIncludeType Combine(LibraryIncludeType second)
        {
            return new LibraryIncludeType(_keywords.Union(second.Keywords).ToArray());
        }

        public IEnumerable<LibraryIncludeTypeFlag> Keywords
        {
            get
            {
                return _keywords;
            }
        }

        public override string ToString()
        {
            return string.Join(",", _keywords.Select(kw => kw.ToString()));
        }
    }
}
