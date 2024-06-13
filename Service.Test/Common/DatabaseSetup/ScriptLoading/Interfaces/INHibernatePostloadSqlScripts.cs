namespace Service.Test.Common.DatabaseSetup.ScriptLoading.Interfaces;

public interface INHibernatePostloadSqlScripts
{
    IEnumerable<string> PostloadScripts { get; }
}
