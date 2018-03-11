using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Common.Functions.DownloadAndExtractZip.ZipDownloaderAndExtractor;

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
            var files = Directory.EnumerateFiles(@"C:\projects\", " *.csproj", SearchOption.AllDirectories);
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
                Console.WriteLine($"Looking for package: {name}...");
                if (File.Exists(Path.Combine(cacheDirectory, name))) continue;

                try
                {
                    Console.WriteLine($"Caching package: {name}...");
                    DownloadAndExtract(new Uri($"{packageUri}/{name}.zip"), cacheDirectory);
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
