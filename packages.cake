using System.Xml.Linq;

public class Packages
{
    public List<NuGetPackSettings> NuspecNuGetSettings { get; private set; }
    public FilePath[] NugetPackages { get; private set; }
    public FilePath[] BinFiles { get; private set; }
    public string NugetPackagesDir {get; private set;}
    public string SkiaSharpVersion {get; private set; }
    public Packages(ICakeContext context, Parameters parameters)
    {
        // NUGET NUSPECS
        context.Information("Getting git modules:");

        var ignoredSubModulesPaths = System.IO.File.ReadAllLines(".git/config").Where(m=>m.StartsWith("[submodule ")).Select(m => 
        {
            var path = m.Split(' ')[1].Trim("\"[] \t".ToArray());
            context.Information(path);
            return ((DirectoryPath)context.Directory(path)).FullPath;
        }).ToList();

        var normalizePath = new Func<string, string>(
            path => path.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar).ToUpperInvariant());

        // Key: Package Id
        // Value is Tuple where Item1: Package Version, Item2: The packages.config file path.
        var packageVersions = new Dictionary<string, IList<Tuple<string,string>>>();

        System.IO.Directory.EnumerateFiles(((DirectoryPath)context.Directory("./src")).FullPath,
            "*.csproj", SearchOption.AllDirectories).ToList().ForEach(fileName =>
        {
            if (!ignoredSubModulesPaths.Any(i => normalizePath(fileName).Contains(normalizePath(i))))
            {
                var xdoc = XDocument.Load(fileName);
                foreach (var reference in xdoc.Descendants().Where(x => x.Name.LocalName == "PackageReference"))
                {
                    var name = reference.Attribute("Include").Value;
                    var versionAttribute = reference.Attribute("Version");
                    var version = versionAttribute != null 
                        ? versionAttribute.Value 
                        : reference.Elements().First(x=>x.Name.LocalName == "Version").Value;
                    IList<Tuple<string, string>> versions;
                    packageVersions.TryGetValue(name, out versions);
                    if (versions == null)
                    {
                        versions = new List<Tuple<string, string>>();
                        packageVersions[name] = versions;
                    }
                    versions.Add(Tuple.Create(version, fileName));
                }
            }
        });

        context.Information("Checking installed NuGet package dependencies versions:");

        packageVersions.ToList().ForEach(package =>
        {
            var packageVersion = package.Value.First().Item1;
            bool isValidVersion = package.Value.All(x => x.Item1 == packageVersion);
            if (!isValidVersion)
            {
                context.Information("Error: package {0} has multiple versions installed:", package.Key);
                foreach (var v in package.Value)
                {
                    context.Information("{0}, file: {1}", v.Item1, v.Item2);
                }
                throw new Exception("Detected multiple NuGet package version installed for different projects.");
            }
        });

        context.Information("Setting NuGet package dependencies versions:");

        var SerilogVersion = packageVersions["Serilog"].FirstOrDefault().Item1;
        var SplatVersion = packageVersions["Splat"].FirstOrDefault().Item1;
        var SpracheVersion = packageVersions["Sprache"].FirstOrDefault().Item1;
        var SystemReactiveVersion = packageVersions["System.Reactive"].FirstOrDefault().Item1;
        SkiaSharpVersion = packageVersions["SkiaSharp"].FirstOrDefault().Item1;
        var SharpDXVersion = packageVersions["SharpDX"].FirstOrDefault().Item1;
        var SharpDXDirect2D1Version = packageVersions["SharpDX.Direct2D1"].FirstOrDefault().Item1;
        var SharpDXDirect3D11Version = packageVersions["SharpDX.Direct3D11"].FirstOrDefault().Item1;
        var SharpDXDXGIVersion = packageVersions["SharpDX.DXGI"].FirstOrDefault().Item1;

        context.Information("Package: Serilog, version: {0}", SerilogVersion);
        context.Information("Package: Splat, version: {0}", SplatVersion);
        context.Information("Package: Sprache, version: {0}", SpracheVersion);
        context.Information("Package: System.Reactive, version: {0}", SystemReactiveVersion);
        context.Information("Package: SkiaSharp, version: {0}", SkiaSharpVersion);
        context.Information("Package: SharpDX, version: {0}", SharpDXVersion);
        context.Information("Package: SharpDX.Direct2D1, version: {0}", SharpDXDirect2D1Version);
        context.Information("Package: SharpDX.Direct3D11, version: {0}", SharpDXDirect3D11Version);
        context.Information("Package: SharpDX.DXGI, version: {0}", SharpDXDXGIVersion);

        var nugetPackagesDir = System.Environment.GetEnvironmentVariable("NUGET_HOME")
            ?? System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("USERPROFILE") ?? System.Environment.GetEnvironmentVariable("HOME"), ".nuget");
        
        NugetPackagesDir = System.IO.Path.Combine(nugetPackagesDir, "packages");
        
        var SetNuGetNuspecCommonProperties = new Action<NuGetPackSettings> ((nuspec) => {
            nuspec.Version = parameters.Version;
            nuspec.Authors = new [] { "Avalonia Team" };
            nuspec.Owners = new [] { "stevenk" };
            nuspec.LicenseUrl = new Uri("http://opensource.org/licenses/MIT");
            nuspec.ProjectUrl = new Uri("https://github.com/AvaloniaUI/Avalonia/");
            nuspec.RequireLicenseAcceptance = false;
            nuspec.Symbols = false;
            nuspec.NoPackageAnalysis = true;
            nuspec.Description = "The Avalonia UI framework";
            nuspec.Copyright = "Copyright 2015";
            nuspec.Tags = new [] { "Avalonia" };
        });

        var coreLibraries = new string[][]
        {
            new [] { "./src/", "Avalonia.Animation", ".dll" },
            new [] { "./src/", "Avalonia.Animation", ".xml" },
            new [] { "./src/", "Avalonia.Base", ".dll" },
            new [] { "./src/", "Avalonia.Base", ".xml" },
            new [] { "./src/", "Avalonia.Controls", ".dll" },
            new [] { "./src/", "Avalonia.Controls", ".xml" },
            new [] { "./src/", "Avalonia.DesignerSupport", ".dll" },
            new [] { "./src/", "Avalonia.DesignerSupport", ".xml" },
            new [] { "./src/", "Avalonia.Diagnostics", ".dll" },
            new [] { "./src/", "Avalonia.Diagnostics", ".xml" },
            new [] { "./src/", "Avalonia.Input", ".dll" },
            new [] { "./src/", "Avalonia.Input", ".xml" },
            new [] { "./src/", "Avalonia.Interactivity", ".dll" },
            new [] { "./src/", "Avalonia.Interactivity", ".xml" },
            new [] { "./src/", "Avalonia.Layout", ".dll" },
            new [] { "./src/", "Avalonia.Layout", ".xml" },
            new [] { "./src/", "Avalonia.Logging.Serilog", ".dll" },
            new [] { "./src/", "Avalonia.Logging.Serilog", ".xml" },
            new [] { "./src/", "Avalonia.Visuals", ".dll" },
            new [] { "./src/", "Avalonia.Visuals", ".xml" },
            new [] { "./src/", "Avalonia.Styling", ".dll" },
            new [] { "./src/", "Avalonia.Styling", ".xml" },
            new [] { "./src/", "Avalonia.ReactiveUI", ".dll" },
            new [] { "./src/", "Avalonia.Themes.Default", ".dll" },
            new [] { "./src/", "Avalonia.Themes.Default", ".xml" },
            new [] { "./src/Markup/", "Avalonia.Markup", ".dll" },
            new [] { "./src/Markup/", "Avalonia.Markup", ".xml" },
            new [] { "./src/Markup/", "Avalonia.Markup.Xaml", ".dll" },
            new [] { "./src/Markup/", "Avalonia.Markup.Xaml", ".xml" }
        };

        var coreLibrariesFiles = coreLibraries.Select((lib) => {
            return (FilePath)context.File(lib[0] + lib[1] + "/bin/" + parameters.DirSuffix + "/netstandard1.1/" + lib[1] + lib[2]);
        }).ToList();

        var coreLibrariesNuSpecContent = coreLibrariesFiles.Select((file) => {
            return new NuSpecContent { 
                Source = file.FullPath, Target = "lib/netstandard1.1" 
            };
        });

        var win32CoreLibrariesNuSpecContent = coreLibrariesFiles.Select((file) => {
            return new NuSpecContent { 
                Source = file.FullPath, Target = "lib/net45" 
            };
        });

        var netcoreappCoreLibrariesNuSpecContent = coreLibrariesFiles.Select((file) => {
            return new NuSpecContent { 
                Source = file.FullPath, Target = "lib/netcoreapp1.0" 
            };
        });

        var net45RuntimePlatformExtensions = new [] {".xml", ".dll"};
        var net45RuntimePlatform = net45RuntimePlatformExtensions.Select(libSuffix => {
            return new NuSpecContent {
                Source = ((FilePath)context.File("./src/Avalonia.DotNetFrameworkRuntime/bin/" + parameters.DirSuffix + "/Avalonia.DotNetFrameworkRuntime" + libSuffix)).FullPath, 
                Target = "lib/net45" 
            };
        });

        var netCoreRuntimePlatformExtensions = new [] {".xml", ".dll"};
        var netCoreRuntimePlatform = netCoreRuntimePlatformExtensions.Select(libSuffix => {
            return new NuSpecContent {
                Source = ((FilePath)context.File("./src/Avalonia.DotNetCoreRuntime/bin/" + parameters.DirSuffix + "/netcoreapp1.0/Avalonia.DotNetCoreRuntime" + libSuffix)).FullPath, 
                Target = "lib/netcoreapp1.0" 
            };
        });

        var nuspecNuGetSettingsCore = new []
        {
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Serilog", Version = SerilogVersion },
                    new NuSpecDependency() { Id = "Splat", Version = SplatVersion },
                    new NuSpecDependency() { Id = "Sprache", Version = SpracheVersion },
                    new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion },
                    //.NET Core
                    new NuSpecDependency() { Id = "System.Threading.ThreadPool", TargetFramework = "netcoreapp1.0", Version = "4.3.0" },
                    new NuSpecDependency() { Id = "NETStandard.Library", TargetFramework = "netcoreapp1.0", Version = "1.6.0" },
                    new NuSpecDependency() { Id = "Microsoft.NETCore.Portable.Compatibility", TargetFramework = "netcoreapp1.0", Version = "1.0.1" },
                    new NuSpecDependency() { Id = "Splat", TargetFramework = "netcoreapp1.0", Version = SplatVersion },
                    new NuSpecDependency() { Id = "Serilog", TargetFramework = "netcoreapp1.0", Version = SerilogVersion },
                    new NuSpecDependency() { Id = "Sprache", TargetFramework = "netcoreapp1.0", Version = SpracheVersion },
                    new NuSpecDependency() { Id = "System.Reactive", TargetFramework = "netcoreapp1.0", Version = SystemReactiveVersion }
                },
                Files = coreLibrariesNuSpecContent
                    .Concat(win32CoreLibrariesNuSpecContent).Concat(net45RuntimePlatform)
                    .Concat(netcoreappCoreLibrariesNuSpecContent).Concat(netCoreRuntimePlatform)
                    .ToList(),
                BasePath = context.Directory("./"),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.HtmlRenderer
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.HtmlRenderer",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.HtmlRenderer.dll", Target = "lib/netstandard1.1" }
                },
                BasePath = context.Directory("./src/Avalonia.HtmlRenderer/bin/" + parameters.DirSuffix + "/netstandard1.1"),
                OutputDirectory = parameters.NugetRoot
            }
        };

        var nuspecNuGetSettingsMobile = new []
        {
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Android
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Android",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Skia.Android", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Android.dll", Target = "lib/MonoAndroid10" }
                },
                BasePath = context.Directory("./src/Android/Avalonia.Android/bin/" + parameters.DirSuffix),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Skia.Android
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Skia.Android",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "SkiaSharp", Version = SkiaSharpVersion }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Skia.Android.dll", Target = "lib/MonoAndroid10" }
                },
                BasePath = context.Directory("./src/Skia/Avalonia.Skia.Android/bin/" + parameters.DirSuffix),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.iOS
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.iOS",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Skia.iOS", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.iOS.dll", Target = "lib/Xamarin.iOS10" }
                },
                BasePath = context.Directory("./src/iOS/Avalonia.iOS/bin/" + parameters.DirSuffixIOS),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Skia.iOS
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Skia.iOS",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "SkiaSharp", Version = SkiaSharpVersion }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Skia.iOS.dll", Target = "lib/Xamarin.iOS10" }
                },
                BasePath = context.Directory("./src/Skia/Avalonia.Skia.iOS/bin/" + parameters.DirSuffixIOS),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Mobile
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Mobile",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia.Android", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.iOS", Version = parameters.Version }
                },
                Files = new NuSpecContent[]
                {
                    new NuSpecContent { Source = "licence.md", Target = "" }
                },
                BasePath = context.Directory("./"),
                OutputDirectory = parameters.NugetRoot
            }
        };

        var nuspecNuGetSettingsDesktop = new []
        {
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Win32
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Win32",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Win32/bin/" + parameters.DirSuffix + "/Avalonia.Win32.dll", Target = "lib/net45" },
                    new NuSpecContent { Source = "Avalonia.Win32.NetStandard/bin/" + parameters.DirSuffix + "/netstandard1.1/Avalonia.Win32.dll", Target = "lib/netstandard1.1" }
                },
                BasePath = context.Directory("./src/Windows"),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Direct2D1
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Direct2D1",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "SharpDX", Version = SharpDXVersion },
                    new NuSpecDependency() { Id = "SharpDX.Direct2D1", Version = SharpDXDirect2D1Version },
                    new NuSpecDependency() { Id = "SharpDX.Direct3D11", Version = SharpDXDirect3D11Version },
                    new NuSpecDependency() { Id = "SharpDX.DXGI", Version = SharpDXDXGIVersion }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Direct2D1.dll", Target = "lib/net45" }
                },
                BasePath = context.Directory("./src/Windows/Avalonia.Direct2D1/bin/" + parameters.DirSuffix),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Gtk
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Gtk",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Gtk.dll", Target = "lib/net45" }
                },
                BasePath = context.Directory("./src/Gtk/Avalonia.Gtk/bin/" + parameters.DirSuffix),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Gtk3
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Gtk3",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Gtk3.dll", Target = "lib/netstandard1.1" }
                },
                BasePath = context.Directory("./src/Gtk/Avalonia.Gtk3/bin/" + parameters.DirSuffix + "/netstandard1.1"),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Cairo
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Cairo",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Cairo.dll", Target = "lib/net45" }
                },
                BasePath = context.Directory("./src/Gtk/Avalonia.Cairo/bin/" + parameters.DirSuffix),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Skia.Desktop
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Skia.Desktop",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "SkiaSharp", Version = SkiaSharpVersion },
                    //.NET Core
                    new NuSpecDependency() { Id = "Avalonia", TargetFramework = "netcoreapp1.0", Version = parameters.Version },
                    new NuSpecDependency() { Id = "SkiaSharp", TargetFramework = "netcoreapp1.0", Version = SkiaSharpVersion },
                    new NuSpecDependency() { Id = "NETStandard.Library", TargetFramework = "netcoreapp1.0", Version = "1.6.0" },
                    new NuSpecDependency() { Id = "Microsoft.NETCore.Portable.Compatibility", TargetFramework = "netcoreapp1.0", Version = "1.0.1" }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Skia.Desktop/bin/" + parameters.DirSuffixSkia + "/Avalonia.Skia.Desktop.dll", Target = "lib/net45" },
                    new NuSpecContent { Source = "Avalonia.Skia.Desktop.NetStandard/bin/" + parameters.DirSuffix + "/netstandard1.3/Avalonia.Skia.Desktop.dll", Target = "lib/netcoreapp1.0" }
                },
                BasePath = context.Directory("./src/Skia/"),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Desktop
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Desktop",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia.Win32", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Direct2D1", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Gtk", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Cairo", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Skia.Desktop", Version = parameters.Version }
                },
                Files = new NuSpecContent[]
                {
                    new NuSpecContent { Source = "licence.md", Target = "" }
                },
                BasePath = context.Directory("./"),
                OutputDirectory = parameters.NugetRoot
            }
        };

        NuspecNuGetSettings = new List<NuGetPackSettings>();

        NuspecNuGetSettings.AddRange(nuspecNuGetSettingsCore);
        NuspecNuGetSettings.AddRange(nuspecNuGetSettingsDesktop);
        NuspecNuGetSettings.AddRange(nuspecNuGetSettingsMobile);

        NuspecNuGetSettings.ForEach((nuspec) => SetNuGetNuspecCommonProperties(nuspec));

        NugetPackages = NuspecNuGetSettings.Select(nuspec => {
            return nuspec.OutputDirectory.CombineWithFilePath(string.Concat(nuspec.Id, ".", nuspec.Version, ".nupkg"));
        }).ToArray();

        BinFiles = NuspecNuGetSettings.SelectMany(nuspec => {
            return nuspec.Files.Select(file => {
                return ((DirectoryPath)nuspec.BasePath).CombineWithFilePath(file.Source);
            });
        }).GroupBy(f => f.FullPath).Select(g => g.First()).ToArray();
    }
}
