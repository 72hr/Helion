using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helion.Resources.IWad;

public class IWadLocator
{
    private static readonly string[] SteamDoomDirs = new[]
    {
        "steamapps/common/Ultimate Doom/base",
        "steamapps/common/Doom 2/base",
        "steamapps/common/Doom 2/masterbase",
        "steamapps/common/Doom 2/finaldoombase",
        "steamapps/common/Final Doom/base",
        "steamapps/common/DOOM 3 BFG Edition/base/wads",
    };

    private readonly List<string> m_directories;

    public static IWadLocator CreateDefault()
    {
        List<string> paths = new() { Directory.GetCurrentDirectory() };

        string? steamPath = GetSteamPath();

        if (steamPath != null && Directory.Exists(steamPath))
        {
            foreach (var dir in SteamDoomDirs)
                paths.Add(Path.Combine(steamPath, dir));
        }

        return new IWadLocator(paths);
    }

    public IWadLocator(IEnumerable<string> directories)
    {
        m_directories = directories.ToList();
    }

    public List<IWadPath> Locate()
    {
        List<IWadPath> iwads = new();
        HashSet<IWadType> foundTypes = new();
        foreach (var dir in m_directories)
        {
            if (!Directory.Exists(dir))
                continue;

            EnumerateDirectory(iwads, foundTypes, dir);
        }

        iwads.Sort((i1, i2) => i1.Info.IWadType.CompareTo(i2.Info.IWadType));
        return iwads;
    }

    private static void EnumerateDirectory(List<IWadPath> iwads, HashSet<IWadType> foundTypes, string dir)
    {
        try
        {
            var files = Directory.EnumerateFiles(dir, "*")
                .Where(x => Path.GetExtension(x).Equals(".wad", StringComparison.OrdinalIgnoreCase));
            foreach (var file in files)
            {
                IWadInfo iwadInfo = IWadInfo.GetIWadInfo(file);
                if (iwadInfo != IWadInfo.DefaultIWadInfo && !foundTypes.Contains(iwadInfo.IWadType))
                {
                    foundTypes.Add(iwadInfo.IWadType);
                    iwads.Add(new(file, iwadInfo));
                }
            }
        }
        catch { }
    }

    private static string? GetSteamPath()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam/");

        // On Linux, default to "$XDG_CONFIG_HOME/.steam/steam"
        if (OperatingSystem.IsLinux())
        {
            var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

            if (!string.IsNullOrWhiteSpace(xdgConfigHome))
            {
                return $"{xdgConfigHome}/.steam/steam";
            }

            // Fallback to "$HOME/.steam/steam"
            var home = Environment.GetEnvironmentVariable("HOME");

            if (!string.IsNullOrWhiteSpace(home))
            {
                return $"{home}/.steam/steam";
            }
        }

        return null;
    }
}
