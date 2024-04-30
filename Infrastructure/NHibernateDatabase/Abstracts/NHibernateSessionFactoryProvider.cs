using FluentNHibernate.Cfg;
using Microsoft.Extensions.Configuration;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using System.Reflection;

namespace Infrastructure.NHibernateDatabase.Abstracts;

public abstract class NHibernateSessionFactoryProvider : IDisposable
{
    public NHibernateSessionFactoryProvider(IConfiguration? configuration)
    {
        Configuration = configuration;
        NHibernateConfiguration = new Configuration(); // Avoids null errors if this never gets set
        MappingAssembly = Assembly.GetCallingAssembly(); // Avoids null errors if this never gets set
    }

    /// <summary>
    /// MS Appsettings config, comes in via constructor
    /// </summary>
    protected IConfiguration? Configuration { get; }
    protected Assembly MappingAssembly { get; set; }
    protected Configuration NHibernateConfiguration { get; set; }

    #region NHibernate Configuration Stubs
    /// <summary>
    /// At base level, simply sets our NHibernateConfiguration property for us.
    /// Call base, and override to add additional configuration to NHibernate.
    /// This will get called during our Initialize()
    /// </summary>
    /// <param name="configurator">NHibernate configuration</param>
    protected virtual void ConfigureNHibernate(Configuration configurator)
    {
        NHibernateConfiguration = configurator;
    }

    /// <summary>
    /// Allows altering of the raw NHibernate Configuration object before creation
    /// </summary>
    /// <param name="configuration">NHibernate configuration</param>
    protected virtual void TreatConfiguration(Configuration configuration)
    {
        var update = new SchemaUpdate(configuration);
        update.Execute(false, true);
    }
    #endregion

    /// <summary>
    /// Calls to setup ConfigureNHibernate()
    /// Configures OCR mappings via the MappingAssembly
    /// </summary>
    protected virtual ISessionFactory InitializeNHibernate()
    {
        ConfigureNHibernate(NHibernateConfiguration);

        return Fluently.Configure(NHibernateConfiguration)
                       .Mappings(m =>
                       {
                           m.FluentMappings.AddFromAssembly(MappingAssembly);
                       })
                       .ExposeConfiguration(TreatConfiguration)
                       .BuildSessionFactory();
    }

    /// <summary>
    /// Calls to Initialize()
    /// Opens, and closes a Db session in order to pre-warm the Db on initial startup
    /// </summary>
    public ISessionFactory GetSessionFactory()
    {
        var factory = InitializeNHibernate();
        factory.OpenSession()
               .Dispose();

        return factory;
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
            disposedValue = true;
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
