namespace Blocker.App.Services;

public static class FilePatternResolver
{
    public static IReadOnlyList<string> ResolvePaths(IEnumerable<string> patterns)
    {
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in patterns)
        {
            foreach (var path in ResolvePattern(pattern))
            {
                resolved.Add(path);
            }
        }

        return resolved.ToList();
    }

    private static IEnumerable<string> ResolvePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            yield break;
        }

        if (!ContainsWildcard(pattern))
        {
            if (File.Exists(pattern))
            {
                yield return Path.GetFullPath(pattern);
            }

            yield break;
        }

        var root = Path.GetPathRoot(pattern);
        if (string.IsNullOrWhiteSpace(root))
        {
            yield break;
        }

        var relative = pattern[root.Length..];
        var segments = relative
            .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            yield break;
        }

        var currentDirectories = new List<string> { root };
        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            var nextDirectories = new List<string>();

            foreach (var dir in currentDirectories)
            {
                try
                {
                    if (ContainsWildcard(segment))
                    {
                        nextDirectories.AddRange(Directory.EnumerateDirectories(dir, segment));
                    }
                    else
                    {
                        var candidate = Path.Combine(dir, segment);
                        if (Directory.Exists(candidate))
                        {
                            nextDirectories.Add(candidate);
                        }
                    }
                }
                catch
                {
                    // Ignore inaccessible directories.
                }
            }

            currentDirectories = nextDirectories;
            if (currentDirectories.Count == 0)
            {
                yield break;
            }
        }

        var fileSegment = segments[^1];
        foreach (var dir in currentDirectories)
        {
            IEnumerable<string> files;
            try
            {
                files = ContainsWildcard(fileSegment)
                    ? Directory.EnumerateFiles(dir, fileSegment)
                    : File.Exists(Path.Combine(dir, fileSegment))
                        ? new[] { Path.Combine(dir, fileSegment) }
                        : Array.Empty<string>();
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return Path.GetFullPath(file);
            }
        }
    }

    private static bool ContainsWildcard(string value)
    {
        return value.Contains('*') || value.Contains('?');
    }
}
