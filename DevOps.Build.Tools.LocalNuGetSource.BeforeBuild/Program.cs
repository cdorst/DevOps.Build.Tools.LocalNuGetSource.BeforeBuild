using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Common.Functions.DownloadFile.FileDownloader;
using static System.IO.Path;

namespace DevOps.Build.Tools.LocalNuGetSource.BeforeBuild
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var token = args.FirstOrDefault() ?? throw new ArgumentNullException("NamespacePrefix");
            var cacheDirectory = args[1] ?? throw new ArgumentNullException("CacheDirectory");
            var packageUri = args[2] ?? throw new ArgumentNullException("PackageUri");

            // Get list of required NuGet packages
            var packages = new Dictionary<string, string>();

            Console.WriteLine("Finding .csproj...");
            var dir = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_FOLDER");
            var files = Directory.EnumerateFiles(dir, "*.csproj", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                Console.WriteLine($"Found .csproj: {file}");
                Console.WriteLine($"Getting {token} package references...");
                var lines = File.ReadAllLines(file);
                foreach (var line in lines.Where(ln
                    => ln.Contains("PackageReference") && ln.Contains($"\"{token}.")))
                    packages.Add(line);
            }

            // Save each package in local NuGet cache
            foreach (var package in packages)
            {
                var name = package.GetFileName();
                var path = Combine(cacheDirectory, name);
                Console.WriteLine($"Looking for package: {name}...");
                if (File.Exists(path)) continue;

                try
                {
                    Console.WriteLine($"Caching package: {name}...");
                    Download(new Uri($"{packageUri}/{name}"), path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}...");
                    // Ignore 404 or network exception and continue
                }
            }
        }
    }
}
