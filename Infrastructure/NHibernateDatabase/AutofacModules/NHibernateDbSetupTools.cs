using Autofac;
using Infrastructure.Enums;
using Infrastructure.NHibernateDatabase.Abstracts;
using Infrastructure.NHibernateDatabase.Interfaces;
using NHibernate;

namespace Infrastructure.NHibernateDatabase.AutofacModules;

public static class NHibernateDbSetupTools
{
    public static void RegisterDatabase<TDataSessionProvider>(ContainerBuilder builder, NHibernateDatabaseType dbType)
        where TDataSessionProvider : NHibernateSessionFactoryProvider
    {
        // Register the provided DbType for DatabaseFactory methods to resolve 
        builder.RegisterType<Implementations.NHibernateDatabase>()
               .Keyed<INHibernateDatabase>(dbType)
               .SingleInstance();

        // Configuration of connection strings lives here
        builder.RegisterType<TDataSessionProvider>()
               .Keyed<NHibernateSessionFactoryProvider>(dbType)
               .SingleInstance();

        // Registers how we spin up the db connection (ISessionFactory)
        builder.Register(context => context.ResolveKeyed<NHibernateSessionFactoryProvider>(
                                                dbType, new NamedParameter("dbType", dbType)).GetSessionFactory())
               .Keyed<ISessionFactory>(dbType)
               .SingleInstance();

        // Register an ISession to corrispond to the dbType provided
        builder.Register(context => context.ResolveKeyed<ISessionFactory>(dbType).OpenSession())
               .Keyed<ISession>(dbType)
               .InstancePerLifetimeScope();
    }
}
