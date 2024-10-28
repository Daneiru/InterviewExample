using Infrastructure.Enums;
using Infrastructure.NHibernateDatabase.Implementations;
using Microsoft.Extensions.Configuration;

namespace ExampleData;

public class ExampleDataSessionProvider : NHibernateDataSessionProvider
{
    public ExampleDataSessionProvider(IConfiguration configuration) 
        : base(configuration, NHibernateDatabaseType.ExampleData)
    {

    }
}
