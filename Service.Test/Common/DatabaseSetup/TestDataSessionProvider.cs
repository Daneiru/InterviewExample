using Infrastructure.NHibernateDatabase.Abstracts;
using NHibernate.Tool.hbm2ddl;
using NHibernate;
using System.Reflection;
using System.Text.RegularExpressions;
using NHibernate.Cfg;
using Infrastructure.Enums;
using Microsoft.Data.SqlClient;
using Service.Test.Common.DatabaseSetup.ScriptLoading.Interfaces;
using Service.Test.Common.DatabaseSetup.ScriptLoading;

namespace Service.Test.Common.DatabaseSetup;

internal class TestDataSessionProvider : NHibernateSessionFactoryProvider
{
    private readonly SqlConnectionStringBuilder Scsb;
    private readonly bool KeepDb = false;
    private readonly INHibernateSqlScriptLoader ScriptLoader;

    public TestDataSessionProvider(INHibernateSqlScriptLoader loader,
                                      TestDataSessionProviderCongfiguration dataSessionProviderConfiguration,
                                      NHibernateDatabaseType dbType)
        : base(null)
    {
        Scsb = LocalDBFunctions.GenerateDatabase(dbType.ToString(), dataSessionProviderConfiguration.DataSource);
        KeepDb = dataSessionProviderConfiguration.KeepDbAfterTest;
        ScriptLoader = loader;

        if (ScriptLoader != null)
            ((NHibernateAdvancedSqlScriptLoader)ScriptLoader).Preload(dbType.ToString() + @"\Preload\Schemas");

        switch (dbType)
        {
            case NHibernateDatabaseType.DbA:
                MappingAssembly = Assembly.Load("Viridian.Entities.DbA");
                break;
            case NHibernateDatabaseType.DbC:
                MappingAssembly = Assembly.Load("Viridian.Entities.DbC");
                break;
            default:
                break;
        }
    }

    #region Initialize and Configure NHibernate
    protected override void ConfigureNHibernate(Configuration configurator)
    {
        configurator.SetProperty("connection.connection_string", Scsb.ConnectionString);

        configurator.SetProperty("dialect", "NHibernate.Dialect.MsSql2012Dialect");
        configurator.SetProperty("connection.isolation", "ReadCommitted");
        configurator.SetProperty("generate_statistics", "true");
        configurator.SetProperty("prepare_sql", "true");
        configurator.SetProperty("adonet.batch_size", "15");
        configurator.SetProperty("cache.region_prefix", "QA");
    }

    Configuration NHConfiguration { get; set; }

    protected override ISessionFactory InitializeNHibernate()
    {
        var SessionFactory = base.InitializeNHibernate();

        using (var dbSession = SessionFactory.OpenSession())
        {
            ExecutePreloader(dbSession, ScriptLoader as INHibernatePreloadSqlScripts);

            new SchemaExport(NHConfiguration).Execute(true, true, false);

            ExecuteLoader(dbSession, ScriptLoader);
            ExecutePostLoader(dbSession, ScriptLoader as INHibernatePostloadSqlScripts);
        }

        return SessionFactory;
    }

    protected override void TreatConfiguration(Configuration configuration)
    {
        NHConfiguration = configuration;
    }
    #endregion

    #region Script Loading
    private static void ExecutePostLoader(ISession dbSession, INHibernatePostloadSqlScripts postloader)
    {
        if (postloader == null)
            return;

        foreach (var script in postloader.PostloadScripts)
        {
            ExecuteSQLFile(dbSession, script);
        }
    }

    private static void ExecuteLoader(ISession dbSession, INHibernateSqlScriptLoader scriptLoader)
    {
        foreach (var scriptPath in scriptLoader.Paths)
        {
            ExecuteSQLFile(dbSession, scriptPath);
        }
    }

    private static void ExecutePreloader(ISession dbSession, INHibernatePreloadSqlScripts preloader)
    {
        if (preloader == null)
            return;

        foreach (var script in preloader.PreloadScripts)
        {
            ExecuteSQLFile(dbSession, script);
        }
    }

    private static void ExecuteSQLFile(ISession dbSession, string sqlFilePath)
    {
        string sqlScript;

        using (FileStream strm = File.OpenRead(sqlFilePath))
        {
            var reader = new StreamReader(strm);
            sqlScript = reader.ReadToEnd();
        }

        var regex = new Regex("^GO", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        string[] lines = regex.Split(sqlScript);

        foreach (string line in lines)
        {
            Console.Out.WriteLine(line);
            IQuery query = dbSession.CreateSQLQuery(line);
            query.ExecuteUpdate();
        }
    }
    #endregion

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        if (disposing && !KeepDb && Scsb != null)
            LocalDBFunctions.CleanupLocalDB(Scsb);

        base.Dispose(disposing);
    }
    #endregion
}
