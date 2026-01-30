using System.Text.Json;

namespace mk8.identity.Infrastructure.Environment
{
    public static class EnvironmentLoader
    {
        private static EnvironmentConfig? _cachedConfig;
        private static readonly object _lock = new();

        public static EnvironmentConfig Load(bool isDevelopment = false)
        {
            lock (_lock)
            {
                if (_cachedConfig != null)
                    return _cachedConfig;

                var baseDir = AppContext.BaseDirectory;
                var envDir = FindEnvironmentDirectory(baseDir);

                if (envDir == null)
                    throw new InvalidOperationException("Environment directory not found. Ensure 'Environment' folder exists with .env files.");

                var envFile = isDevelopment ? ".dev.env" : ".env";
                var filePath = Path.Combine(envDir, envFile);

                // Fall back to .env if .dev.env doesn't exist
                if (!File.Exists(filePath))
                {
                    filePath = Path.Combine(envDir, ".env");
                }

                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Environment file not found: {filePath}");

                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<EnvironmentConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _cachedConfig = config ?? throw new InvalidOperationException("Failed to parse environment configuration.");
                return _cachedConfig;
            }
        }

        public static void ClearCache()
        {
            lock (_lock)
            {
                _cachedConfig = null;
            }
        }

        private static string? FindEnvironmentDirectory(string startDir)
        {
            // Search up the directory tree for the Environment folder
            var current = new DirectoryInfo(startDir);

            while (current != null)
            {
                // Check in Infrastructure\Environment
                var envPath = Path.Combine(current.FullName, "mk8.identity.Infrastructure", "Environment");
                if (Directory.Exists(envPath))
                    return envPath;

                // Check direct Environment folder
                envPath = Path.Combine(current.FullName, "Environment");
                if (Directory.Exists(envPath) && File.Exists(Path.Combine(envPath, ".env")))
                    return envPath;

                current = current.Parent;
            }

            return null;
        }
    }
}
