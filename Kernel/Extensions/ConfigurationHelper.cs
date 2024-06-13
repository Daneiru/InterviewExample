using Microsoft.Extensions.Configuration;

namespace Kernel.Extensions;

public class ConfigurationHelper
{ 
    /// <summary>
    /// Retrieve a section from the appsettings.config
    /// Useful in cases where injection isn't an option
    /// </summary>
    /// <param name="section">Section to retrieve</param>
    public static IConfigurationSection GetSection(string section)
    {
    var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                            .AddEnvironmentVariables();

    IConfigurationRoot configuration = builder.Build();
    return configuration.GetSection(section);
    }
}
