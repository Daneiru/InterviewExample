namespace Service.Test.Common.DatabaseSetup.ScriptLoading.Interfaces;

public interface INHibernateSqlScriptLoader
{
    IEnumerable<string> Paths { get; }
}
