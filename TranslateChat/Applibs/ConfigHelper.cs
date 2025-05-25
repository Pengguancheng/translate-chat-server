namespace TranslateChat.Applibs;

public static class ConfigHelper
{
    private static IConfiguration? _config;

    public static IConfiguration Config
    {
        get
        {
            if (_config == null)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
                        optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();

                _config = builder.Build();
            }

            return _config;
        }
    }

    public static string Env => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!;

    public static List<string> ChatLanguages => Config.GetSection("ChatLanguages").Get<List<string>>()!;
}