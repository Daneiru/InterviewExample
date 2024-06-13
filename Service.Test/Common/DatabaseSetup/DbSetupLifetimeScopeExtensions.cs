using Autofac;
using Infrastructure.Enums;
using Infrastructure.NHibernateDatabase.Interfaces;
using Moq;
using NHibernate;
using Service.Test.Common.DatabaseSetup.ScriptLoading.Interfaces;
using System.Text.RegularExpressions;

namespace Service.Test.Common.DatabaseSetup;

public static class DbSetupLifetimeScopeExtensions
{
    public static Mock<INHibernateDatabase> SetupDbMock(this ILifetimeScope lifetimeScope, NHibernateDatabaseType dbType, bool persistSession = false, Action? onCommit = null)
    {
        // Setup a Mock database with proper verifiable callbacks for transactions and persistance
        var session = lifetimeScope.ResolveKeyed<ISession>(dbType);

        var database = Mock.Get(lifetimeScope.Resolve<INHibernateDatabase>());
        database.Setup(db => db.Session)
                .Returns(session);

        var commit = database.Setup(db => db.Commit());

        if (persistSession)
        {
            commit.Callback(() =>
            {
                session.GetCurrentTransaction()?.Commit(); // The commit should fail and thus not modify the database when set
                onCommit?.Invoke();
            })
                    .Verifiable();
        }
        else
        {
            commit.Callback(() =>
            {
                session.Flush(); // Does not commit
                onCommit?.Invoke();
            })
                    .Verifiable();
        }

        var databaseFactory = Mock.Get(lifetimeScope.Resolve<INHibernateDatabaseFactory>());

        switch (dbType)
        {
            case NHibernateDatabaseType.DbA:
                databaseFactory.Setup(d => d.GetDbADataSession())
                               .Returns(database.Object);
                break;
            case NHibernateDatabaseType.DbB:
                databaseFactory.Setup(d => d.GetDbBDataSession())
                               .Returns(database.Object);
                break;
            default:
                break;
        }

        return database;
    }

    public static TestDbBuilder GetDatabaseBuilder(this ILifetimeScope lifetimeScope, NHibernateDatabaseType dbType)
    {
        return lifetimeScope.Resolve<TestDbBuilder>(new NamedParameter("dbType", dbType));
    }

    public static void RunLoader(this ILifetimeScope lifetimeScope)
    {
        var session = lifetimeScope.Resolve<ISession>();
        var loader = lifetimeScope.Resolve<INHibernateSqlScriptLoader>();
        session.BeginTransaction();
        ExecuteLoader(session, loader);
    }

    private static void ExecuteLoader(ISession dbSession, INHibernateSqlScriptLoader scriptLoader)
    {
        foreach (var scriptPath in scriptLoader.Paths)
        {
            ExecuteSQLFile(dbSession, scriptPath);
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
            IQuery query = dbSession.CreateSQLQuery(line);
            query.ExecuteUpdate();
        }
    }
}
