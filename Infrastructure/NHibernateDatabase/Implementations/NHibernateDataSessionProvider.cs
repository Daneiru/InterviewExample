using Infrastructure.Enums;
using Infrastructure.NHibernateDatabase.Abstracts;
using Microsoft.Extensions.Configuration;
using NHibernate.Cfg;
using System.Reflection;

namespace Infrastructure.NHibernateDatabase.Implementations;

public class NHibernateDataSessionProvider : NHibernateSessionFactoryProvider
{
    public NHibernateDataSessionProvider(IConfiguration configuration, NHibernateDatabaseType dbType)
        : base(configuration)
    {
        switch (dbType)
        {
            case NHibernateDatabaseType.DbA:
                MappingAssembly = Assembly.Load("Domain.Entities.DbA");
                ConnectionString = Configuration["ConnectionStrings:DbA"];
                break;
            case NHibernateDatabaseType.DbC:
                MappingAssembly = Assembly.Load("Domain.Entities.DbC");
                ConnectionString = Configuration["ConnectionStrings:DbC"];
                break;
            default:
                break;
        }
    }

    private readonly string ConnectionString = null;

    protected override void ConfigureNHibernate(Configuration configurator)
    {
        base.ConfigureNHibernate(configurator);

        // Grab's the hibernate.cfg.xml from the executing project
        NHibernateConfiguration.Configure();

        // Setup the connection string for the database
        NHibernateConfiguration.SetProperty(NHibernate.Cfg.Environment.ConnectionString, ConnectionString);
    }
}
