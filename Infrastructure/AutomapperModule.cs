using Autofac;
using AutoMapper;
using System.Reflection;

namespace Infrastructure;

// INTERVIEW NOTE: Looks like there is a much newer version of AutoMapper vs what I am currently familiar with,
//                  this is based on AM v11.0.1 instead of v13

public class AutoMapperModule : Autofac.Module
{
    private readonly IEnumerable<Assembly> assembliesToScan;
    public AutoMapperModule(IEnumerable<Assembly> assembliesToScan)
    {
        this.assembliesToScan = assembliesToScan;
    }

    public AutoMapperModule(params Assembly[] assembliesToScan) : this((IEnumerable<Assembly>)assembliesToScan) { }

    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        var assembliesToScan = this.assembliesToScan as Assembly[] ?? this.assembliesToScan.ToArray();

        var allTypes = assembliesToScan.Where(a => !a.IsDynamic && a.GetName().Name != nameof(AutoMapper))
                                       .Distinct() // avoid AutoMapper.DuplicateTypeMapConfigurationException
                                       .SelectMany(a => a.DefinedTypes)
                                       .ToArray();

        var openTypes = new[] {
            typeof(IValueResolver<,,>),
            typeof(IMemberValueResolver<,,,>),
            typeof(ITypeConverter<,>),
            typeof(IValueConverter<,>),
            typeof(IMappingAction<,>)
        };

        foreach (var type in openTypes.SelectMany(openType =>
         allTypes.Where(t => t.IsClass && !t.IsAbstract && ImplementsGenericInterface(t.AsType(), openType))))
        {
            builder.RegisterType(type.AsType()).InstancePerDependency();
        }

        builder.Register<IConfigurationProvider>(ctx => new MapperConfiguration(cfg => cfg.AddMaps(assembliesToScan)))
               .SingleInstance();

        builder.Register<IMapper>(ctx => new Mapper(ctx.Resolve<IConfigurationProvider>(), ctx.Resolve))
               .InstancePerDependency();
    }

    private static bool ImplementsGenericInterface(Type type, Type interfaceType)
              => IsGenericType(type, interfaceType) || type.GetTypeInfo().ImplementedInterfaces.Any(@interface => IsGenericType(@interface, interfaceType));

    private static bool IsGenericType(Type type, Type genericType)
              => type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == genericType;
}
