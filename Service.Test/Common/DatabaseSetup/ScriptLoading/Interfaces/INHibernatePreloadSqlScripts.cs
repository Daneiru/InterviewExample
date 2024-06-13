namespace Service.Test.Common.DatabaseSetup.ScriptLoading.Interfaces;

public interface INHibernatePreloadSqlScripts
{
    IEnumerable<string> PreloadScripts { get; }
}
