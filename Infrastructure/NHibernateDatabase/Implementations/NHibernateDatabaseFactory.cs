using Autofac;
using Infrastructure.Enums;
using Infrastructure.NHibernateDatabase.Interfaces;

namespace Infrastructure.NHibernateDatabase.Implementations
{
    public class NHibernateDatabaseFactory : INHibernateDatabaseFactory
    {
        ILifetimeScope Scope;

        public NHibernateDatabaseFactory(ILifetimeScope scope)
        {
            Scope = scope;
        }

        public INHibernateDatabase GetDbADataSession()
        {
            return Scope.ResolveKeyed<INHibernateDatabase>(NHibernateDatabaseType.ExampleData, 
                new NamedParameter("dbType", NHibernateDatabaseType.ExampleData));
        }

        public INHibernateDatabase GetDbBDataSession()
        {
            return Scope.ResolveKeyed<INHibernateDatabase>(NHibernateDatabaseType.DbB, 
                new NamedParameter("dbType", NHibernateDatabaseType.DbB));
        }

        public INHibernateDatabase GetDbCDataSession()
        {
            return Scope.ResolveKeyed<INHibernateDatabase>(NHibernateDatabaseType.DbC, 
                new NamedParameter("dbType", NHibernateDatabaseType.DbC));
        }
    }
}
