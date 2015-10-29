using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.Text;
using Microsoft.Win32;

namespace Console.Test
{
    public class TestTextBufferHelper
    {
        private static string _idePath;
        private static string _editorPath;
        private static string _privatePath;
        private readonly CompositionContainer _container;
        private static string[] _editorAssemblies = new string[]
        {
            //"Microsoft.VisualStudio.CoreUtility.dll",
            //"Microsoft.VisualStudio.Editor.dll",
            //"Microsoft.VisualStudio.Language.Intellisense.dll",
            "Microsoft.VisualStudio.Platform.VSEditor.dll",
            //"Microsoft.VisualStudio.Text.Data.dll",
            //"Microsoft.VisualStudio.Text.Logic.dll",
            //"Microsoft.VisualStudio.Text.UI.dll",
            //"Microsoft.VisualStudio.Text.UI.Wpf.dll",
        };

        public TestTextBufferHelper()
        {
            _container = Initialize();
            _container.ComposeParts(this);
        }

        private static CompositionContainer Initialize()
        {
            _idePath = GetHostExePath();

            _editorPath = Path.Combine(_idePath, @"CommonExtensions\Microsoft\Editor");
            _privatePath = Path.Combine(_idePath, @"PrivateAssemblies\");

            AggregateCatalog aggregateCatalog = new AggregateCatalog();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            foreach (string asmName in _editorAssemblies)
            {
                string asmPath = Path.Combine(_editorPath, asmName);
                Assembly editorAssebmly = Assembly.LoadFrom(asmPath);

                AssemblyCatalog editorCatalog = new AssemblyCatalog(editorAssebmly);
                aggregateCatalog.Catalogs.Add(editorCatalog);
            }

            var compositionContainer = new CompositionContainer(aggregateCatalog, isThreadSafe: true);

            // The following line resolves the parts
            var parts = compositionContainer.Catalog.Parts;

            return compositionContainer;
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string name = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";
            Assembly asm = null;

            if (!string.IsNullOrEmpty(_privatePath))
            {
                string path = Path.Combine(_privatePath, name);

                if (File.Exists(path))
                {
                    try
                    {
                        asm = Assembly.LoadFrom(path);
                    }
                    catch (IOException)
                    {
                    }
                }
            }

            if (asm == null && !string.IsNullOrEmpty(_idePath))
            {
                string path = Path.Combine(_idePath, name);

                if (File.Exists(path))
                {
                    try
                    {
                        asm = Assembly.LoadFrom(path);
                    }
                    catch (IOException)
                    {
                    }
                }
            }

            return asm;
        }

        private static string GetHostExePath()
        {
            string path = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\" + GetHostVersion(),
                "InstallDir",
                string.Empty) as string;

            return path;
        }

        private static string GetHostVersion()
        {
            string version = Environment.GetEnvironmentVariable("ExtensionsVSVersion");

            foreach (string checkVersion in new string[]
            {
                "14.0",
                "12.0"
            })
            {
                if (string.IsNullOrEmpty(version))
                {
                    using (RegistryKey key
                        = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\" + checkVersion))
                    {
                        if (key != null)
                        {
                            version = checkVersion;
                        }
                    }
                }
            }

            return version;
        }

        [Import]
        public ITextBufferFactoryService TextBufferFactory = null;
    }
}
