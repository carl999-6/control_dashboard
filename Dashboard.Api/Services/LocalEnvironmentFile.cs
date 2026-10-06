namespace Dashboard.Api.Services;

public static class LocalEnvironmentFile
{
    public static void Load(string path)
    {
        if (!File.Exists(path)) return;
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var separator = line.IndexOf('=');
            if (separator <= 0) continue;
            var key = line[..separator].Trim();
            if (key.Length == 0 || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key))) continue;
            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
