using Autofac;
using Kernel.Extensions;
using Serilog;
using Serilog.Events;

namespace Service.Test.Common.Modules;

internal class TestLoggerModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        // Grab serilog settings from appsettings
        var settings = ConfigurationHelper.GetSection("Serilog");

        // Serilog template, and log file settings
        var loggerTemplate = "[{Timestamp:HH:mm:ss} {Level}] {Message} ({SourceContext:l}){NewLine}{Exception}";
        var loggerFilePath = settings["FilePath"] + settings["FileName"] + ".txt";

        // Configure Serilog/ILogger to output to Debug window, and logging file
        Log.Logger = new LoggerConfiguration()
                            .Enrich.WithProperty("SourceContext", null)
                            .WriteTo.Console(LogEventLevel.Verbose, outputTemplate: loggerTemplate)
                            //.WriteTo.File(loggerFilePath, LogEventLevel.Error, outputTemplate: loggerTemplate) // TODO: Resolve dependancy for this
                            .CreateLogger();

        // TODO: This no longer works this way??
        // Registers Serilog integration with Autofac, ILogger
        //builder.RegisterLogger();
    }
}
