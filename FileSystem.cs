using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool;

internal static class FileSystem
{
    internal static readonly string? RootPath = Environment.ProcessPath;
    internal static string? ProjectPath = null;

    static FileSystem()
    {
        RootPath = Path.GetFullPath(@$"{Environment.ProcessPath}\..");
    }

    internal static string? FindUProjectPath()
    {
        if (RootPath is null)
            return null;

        string path = RootPath;

        int count;
        for (count = Directory.GetFiles(RootPath, "*.uproject", SearchOption.TopDirectoryOnly).Count();
            count <= 0;
            path = Path.GetFullPath(@$"{path}\.."),
            count = Directory.GetFiles(path, "*.uproject", SearchOption.TopDirectoryOnly).Count()
            )
        {
            if (path.Length <= 3)
                return null;
        }

        ProjectPath = path;
        return path;
    }

    internal static List<string> GetAllModules()
    {
        List<string> result = new();
        if (ProjectPath is null)
            return result;

        List<string> pluginRoots = [.. Directory.GetDirectories($@"{ProjectPath}\Plugins")];
        foreach (var item in pluginRoots)
        {
            if (!Directory.Exists($@"{item}\Source"))
                continue;

            List<string> pluginSourceModules = [.. Directory.GetDirectories($@"{item}\Source")];
            foreach (var module in pluginSourceModules)
            {
                if (File.Exists($@"{module}\{new DirectoryInfo(module).Name}.Build.cs"))
                    result.Add(module);
            }
        }

        List<string> projectRoots = [.. Directory.GetDirectories($@"{ProjectPath}\Source")];
        foreach (var module in projectRoots)
        {
            if (File.Exists($@"{module}\{new DirectoryInfo(module).Name}.Build.cs"))
                result.Add(module);
        }

        return result;
    }

    internal static List<string> GetAllIntermediateFolders(List<string> modulePaths)
    {
        List<string> result = new();

        foreach (var modulePath in modulePaths)
        {
            string path;
            for (path = modulePath; path.Length > 3; path = Path.GetFullPath($@"{path}\.."))
            {
                if (Directory.Exists($@"{path}\Intermediate"))
                {
                    result.Add($@"{path}\Intermediate");
                    break;
                }
            }
        }

        return result;
    }
}
