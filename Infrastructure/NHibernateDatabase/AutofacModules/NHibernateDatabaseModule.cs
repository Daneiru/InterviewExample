using Autofac;
using Infrastructure.Enums;
using Infrastructure.NHibernateDatabase.Implementations;
using Infrastructure.NHibernateDatabase.Interfaces;

namespace Infrastructure.NHibernateDatabase.AutofacModules;

public class NHibernateStandardDatabasesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register our Db factory implementation to resolve types requested explicitly
        builder.RegisterType<NHibernateDatabaseFactory>()
               .As<INHibernateDatabaseFactory>()
               .InstancePerLifetimeScope();

        // Register each DbType we need
        NHibernateDbSetupTools.RegisterDatabase<NHibernateDataSessionProvider>(builder, NHibernateDatabaseType.DbA);
        NHibernateDbSetupTools.RegisterDatabase<NHibernateDataSessionProvider>(builder, NHibernateDatabaseType.DbC);
    }
}
