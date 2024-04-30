using AutoMapper;
using Infrastructure.NHibernateDatabase.Interfaces;
using Kernel.MassTransit.Abstracts;
using Serilog;

namespace Service.Common
{
    public abstract class BaseNHibernateConsumer<T> : BaseConsumer<T>
        where T : class
    {
        protected INHibernateDatabaseFactory DatabaseFactory { get; }
        protected IMapper MappingConfig { get; }

        protected BaseNHibernateConsumer(ILogger logger, INHibernateDatabaseFactory databaseFactory, IMapper mappingConfig) : base(logger)
        {
            DatabaseFactory = databaseFactory;  
            MappingConfig = mappingConfig;
        }
    }
}
