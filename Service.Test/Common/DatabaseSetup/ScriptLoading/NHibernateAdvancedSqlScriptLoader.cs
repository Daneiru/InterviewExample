using Service.Test.Common.DatabaseSetup.ScriptLoading.Interfaces;

namespace Service.Test.Common.DatabaseSetup.ScriptLoading;

public class NHibernateAdvancedSqlScriptLoader : NHibernateBasicSqlScriptLoader, INHibernatePreloadSqlScripts, INHibernatePostloadSqlScripts, INHibernateSqlScriptLoader
{
    readonly List<(NHibernateSqlScriptLoadingType, string)> Scripts = new List<(NHibernateSqlScriptLoadingType, string)>();

    public NHibernateAdvancedSqlScriptLoader()
    {
        BasePath = "SQL";
        AppendSuffix = true;
    }

    /// <summary>
    /// Registers a sql file as a 'PreLoad' type.
    /// </summary>
    /// <param name="sqlFile">Project relative file path</param>
    public void Preload(string sqlFile)
    {
        Scripts.Add((NHibernateSqlScriptLoadingType.Preload, Load(sqlFile)));
    }

    /// <summary>
    /// Registers a sql file as a 'PostLoad' type.
    /// </summary>
    /// <param name="sqlFile">Project relative file path</param>
    public void Postload(string sqlFile)
    {
        Scripts.Add((NHibernateSqlScriptLoadingType.PostLoad, Load(sqlFile)));
    }

    /// <summary>
    /// Registers a sql file as a 'Load' type.
    /// </summary>
    /// <param name="sqlFile">Project relative file path</param>
    public void Add(string sqlFile)
    {
        Scripts.Add((NHibernateSqlScriptLoadingType.Load, Load(sqlFile)));
    }

    public IEnumerable<string> GetScriptsBy(NHibernateSqlScriptLoadingType type)
    {
        return Scripts.Where(x => x.Item1 == type)
                      .Select(x => x.Item2);
    }

    public virtual IEnumerable<string> PreloadScripts => GetScriptsBy(NHibernateSqlScriptLoadingType.Preload);

    public virtual IEnumerable<string> Paths => GetScriptsBy(NHibernateSqlScriptLoadingType.Load);

    public virtual IEnumerable<string> PostloadScripts => GetScriptsBy(NHibernateSqlScriptLoadingType.PostLoad);
}
