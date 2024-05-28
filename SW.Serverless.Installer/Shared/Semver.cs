using System;
using System.Collections.Generic;
using System.Linq;

namespace SW.Serverless.Installer.Shared;

public static class Semver
{
    public static string GetNewVersion(string mode, List<string> olderVersions)
    {
        // If there are no older versions and mode is a version number, return the mode
        if ((olderVersions == null || !olderVersions.Any()) && IsVersionNumber(mode))
        {
            return mode;
        }

        // If there are no older versions and mode is major, minor, or patch, return 1.0.0
        if (olderVersions == null || !olderVersions.Any())
        {
            return "1.0.0";
        }

        if (olderVersions.Count == 1 && !IsVersionNumber(mode) && !IsVersionNumber(olderVersions.First()))
        {
            return "1.0.0";
        }

        // Find the highest version
        var highestVersion = GetHighestVersion(olderVersions);

        // Handle mode
        if (mode.Equals("major", StringComparison.OrdinalIgnoreCase))
        {
            return $"{highestVersion.Major + 1}.0.0";
        }

        if (mode.Equals("minor", StringComparison.OrdinalIgnoreCase))
        {
            return $"{highestVersion.Major}.{highestVersion.Minor + 1}.0";
        }

        if (mode.Equals("patch", StringComparison.OrdinalIgnoreCase))
        {
            return $"{highestVersion.Major}.{highestVersion.Minor}.{highestVersion.Patch + 1}";
        }

        // Validate the provided version
        if (IsVersionNumber(mode))
        {
            var newVersion = ParseVersion(mode);
            if (newVersion.CompareTo(highestVersion) <= 0)
            {
                throw new ArgumentException("The provided version must be higher than all older versions.");
            }

            return mode;
        }

        throw new ArgumentException(
            $"Invalid mode: {mode}. Use 'major', 'minor', 'patch' or a valid semantic version.");
    }

    private static (int Major, int Minor, int Patch) GetHighestVersion(List<string> versions)
    {
        // Parse versions into tuples of (major, minor, patch)
        var parsedVersions = versions.Where(IsVersionNumber).Select(ParseVersion).ToList();

        // Find the highest version
        return parsedVersions.Max();
    }

    private static (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
        {
            throw new ArgumentException($"Invalid version format: {version}. Expected format is major.minor.patch.");
        }

        return (major, minor, patch);
    }

    private static bool IsVersionNumber(string mode)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(mode, @"^\d+\.\d+\.\d+(-\S+)?$");
    }
}
