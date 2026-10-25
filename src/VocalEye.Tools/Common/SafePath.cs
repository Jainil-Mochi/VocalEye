namespace VocalEye.Tools.Common;

/// <summary>
/// Keeps file-writing tools inside their output folder. A model (or a prompt-injected document) must not be able to
/// write outside it, so every path goes through here.
/// </summary>
public static class SafePath
{
    private const int MaximumNameLength = 80;

    private static readonly char[] ExtraInvalidCharacters = { '/', '\\', ':' };

    /// <summary>Turns free text into a safe single file or folder name. Returns <paramref name="fallback"/> if nothing usable is left.</summary>
    public static string SanitizeName(string name, string fallback)
    {
        char[] invalid = Path.GetInvalidFileNameChars();

        char[] cleaned = name
            .Trim()
            .Select(character => invalid.Contains(character) || ExtraInvalidCharacters.Contains(character) || char.IsControl(character) ? '_' : character)
            .ToArray();

        string result = new string(cleaned).Trim('.', ' ');
        if (result.Length > MaximumNameLength)
        {
            result = result.Substring(0, MaximumNameLength).TrimEnd('.', ' ');
        }

        return string.IsNullOrWhiteSpace(result) ? fallback : result;
    }

    /// <summary>Combines a relative path with the root and proves the result is still inside the root.</summary>
    /// <exception cref="ArgumentException">The path is empty, absolute, contains "..", or would escape the root.</exception>
    public static string Resolve(string rootDirectory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("A file path must not be empty.");
        }

        string normalized = relativePath.Trim().Replace('\\', '/');

        bool rooted = Path.IsPathRooted(relativePath) || normalized.StartsWith('/') || normalized.Contains(':');
        if (rooted)
        {
            throw new ArgumentException($"Path '{relativePath}' must be relative (no drive letters or leading slashes).");
        }

        if (normalized.Split('/').Any(part => part == ".."))
        {
            throw new ArgumentException($"Path '{relativePath}' must not contain '..'.");
        }

        if (relativePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            throw new ArgumentException($"Path '{relativePath}' contains invalid characters.");
        }

        string root = Path.GetFullPath(rootDirectory);
        string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        string full = Path.GetFullPath(Path.Combine(root, normalized));

        if (!full.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Path '{relativePath}' is outside the allowed folder.");
        }

        return full;
    }

    /// <summary>Returns the path unchanged if nothing exists there, otherwise "name (2).ext", "name (3).ext" and so on. Never overwrites.</summary>
    /// <exception cref="InvalidOperationException">No free name was found.</exception>
    public static string MakeUniquePath(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return path;
        }

        string directory = Path.GetDirectoryName(path) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);

        for (int index = 2; index < 1000; index++)
        {
            string candidate = Path.Combine(directory, $"{name} ({index}){extension}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Could not find a free name next to '{path}'.");
    }
}
