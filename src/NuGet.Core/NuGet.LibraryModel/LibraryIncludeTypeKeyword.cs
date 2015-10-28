﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.LibraryModel
{
    public class LibraryIncludeTypeKeyword
    {
        private static ConcurrentDictionary<string, LibraryIncludeTypeKeyword> _keywords 
            = new ConcurrentDictionary<string, LibraryIncludeTypeKeyword>(StringComparer.OrdinalIgnoreCase);

        public static readonly LibraryIncludeTypeKeyword Default;
        public static readonly LibraryIncludeTypeKeyword None;
        public static readonly LibraryIncludeTypeKeyword All;
        public static readonly LibraryIncludeTypeKeyword ContentFiles;
        public static readonly LibraryIncludeTypeKeyword Build;
        public static readonly LibraryIncludeTypeKeyword Native;
        public static readonly LibraryIncludeTypeKeyword Compile;
        public static readonly LibraryIncludeTypeKeyword Runtime;
        public static readonly LibraryIncludeTypeKeyword Dependencies;

        private readonly string _value;
        private readonly IEnumerable<LibraryIncludeTypeFlag> _flagsToAdd;
        private readonly IEnumerable<LibraryIncludeTypeFlag> _flagsToRemove;

        public IEnumerable<LibraryIncludeTypeFlag> FlagsToAdd
        {
            get { return _flagsToAdd; }
        }

        public IEnumerable<LibraryIncludeTypeFlag> FlagsToRemove
        {
            get { return _flagsToRemove; }
        }

        static LibraryIncludeTypeKeyword()
        {
            var emptyFlags = Enumerable.Empty<LibraryIncludeTypeFlag>();

            Default = Declare(
                "default",
                flagsToAdd: new[]
                    {
                        LibraryIncludeTypeFlag.Build,
                        LibraryIncludeTypeFlag.Compile,
                        LibraryIncludeTypeFlag.Dependencies,
                        LibraryIncludeTypeFlag.Native,
                        LibraryIncludeTypeFlag.Runtime
                    },
                flagsToRemove: emptyFlags);

            All = Declare(
                "all",
                flagsToAdd: new[]
                    {
                        LibraryIncludeTypeFlag.Build,
                        LibraryIncludeTypeFlag.ContentFiles,
                        LibraryIncludeTypeFlag.Compile,
                        LibraryIncludeTypeFlag.Dependencies,
                        LibraryIncludeTypeFlag.Native,
                        LibraryIncludeTypeFlag.Runtime
                    },
                flagsToRemove: emptyFlags);

            DeclareOnOff(nameof(LibraryIncludeTypeFlag.Build), LibraryIncludeTypeFlag.Build, emptyFlags);
            DeclareOnOff(nameof(LibraryIncludeTypeFlag.ContentFiles), LibraryIncludeTypeFlag.ContentFiles, emptyFlags);
            DeclareOnOff(nameof(LibraryIncludeTypeFlag.Compile), LibraryIncludeTypeFlag.Compile, emptyFlags);
            DeclareOnOff(nameof(LibraryIncludeTypeFlag.Dependencies), LibraryIncludeTypeFlag.Dependencies, emptyFlags);
            DeclareOnOff(nameof(LibraryIncludeTypeFlag.Native), LibraryIncludeTypeFlag.Native, emptyFlags);
            DeclareOnOff(nameof(LibraryIncludeTypeFlag.Runtime), LibraryIncludeTypeFlag.Runtime, emptyFlags);
            DeclareOnOff(nameof(LibraryIncludeTypeFlag.All), LibraryIncludeTypeFlag.All, All.FlagsToAdd);
            DeclareOnOff(nameof(LibraryIncludeTypeFlag.None), LibraryIncludeTypeFlag.None, emptyFlags);
        }

        private static void DeclareOnOff(
            string name, 
            LibraryIncludeTypeFlag flag, 
            IEnumerable<LibraryIncludeTypeFlag> emptyFlags)
        {
            Declare(name,
                flagsToAdd: new[] { flag },
                flagsToRemove: emptyFlags);

            Declare(
                name + "-off",
                flagsToAdd: emptyFlags,
                flagsToRemove: new[] { flag });
        }

        private LibraryIncludeTypeKeyword(
            string value,
            IEnumerable<LibraryIncludeTypeFlag> flagsToAdd,
            IEnumerable<LibraryIncludeTypeFlag> flagsToRemove)
        {
            _value = value;
            _flagsToAdd = flagsToAdd;
            _flagsToRemove = flagsToRemove;
        }

        internal static LibraryIncludeTypeKeyword Declare(
            string keyword,
            IEnumerable<LibraryIncludeTypeFlag> flagsToAdd,
            IEnumerable<LibraryIncludeTypeFlag> flagsToRemove)
        {
            return _keywords.GetOrAdd(keyword, _ => new LibraryIncludeTypeKeyword(keyword, flagsToAdd, flagsToRemove));
        }

        internal static LibraryIncludeTypeKeyword Parse(string keyword)
        {
            LibraryIncludeTypeKeyword value;
            if (_keywords.TryGetValue(keyword, out value))
            {
                return value;
            }
            throw new ArgumentException(string.Format("Unknown dependency include flag '{0}'.", keyword));
        }
    }
}
