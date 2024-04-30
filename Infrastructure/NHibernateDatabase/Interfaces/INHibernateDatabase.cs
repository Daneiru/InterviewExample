using NHibernate;

namespace Infrastructure.NHibernateDatabase.Interfaces;

public interface INHibernateDatabase : IDisposable
{
    ISession Session { get; }
    void BeginTransaction();
    void Commit();
}
