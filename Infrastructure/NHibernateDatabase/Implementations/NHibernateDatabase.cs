using Autofac;
using Infrastructure.Enums;
using Infrastructure.NHibernateDatabase.Interfaces;
using NHibernate;

namespace Infrastructure.NHibernateDatabase.Implementations;

public sealed class NHibernateDatabase : INHibernateDatabase
{
    ITransaction? Transaction = null;
    bool Disposed = false;

    public NHibernateDatabase(ILifetimeScope scope, NHibernateDatabaseType dbType)
    {
        Session = scope.ResolveKeyed<ISession>(dbType);
    }

    public ISession Session { get; }

    public void BeginTransaction()
    {
        Transaction = Session.BeginTransaction();
    }

    public void Commit()
    {
        if (Transaction == null)
            return;

        try
        {
            Transaction.Commit();
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (!Disposed)
        {
            Transaction?.Dispose();
            Session?.Close();
            Session?.Dispose();
            Transaction = null;
            Disposed = true;
        }
    }
}
