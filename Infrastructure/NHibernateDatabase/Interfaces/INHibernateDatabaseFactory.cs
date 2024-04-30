namespace Infrastructure.NHibernateDatabase.Interfaces;

public interface INHibernateDatabaseFactory
{
    INHibernateDatabase GetDbADataSession();
    INHibernateDatabase GetDbBDataSession();
    INHibernateDatabase GetDbCDataSession();
}
