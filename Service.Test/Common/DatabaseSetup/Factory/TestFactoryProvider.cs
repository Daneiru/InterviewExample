using Autofac;
using System.Dynamic;

namespace Service.Test.Common.DatabaseSetup.Factory;

internal class TestFactoryProvider<T, IResolver> : DynamicObject where IResolver : ITestFactoryResolver
{
    public TestFactoryProvider(IComponentContext context)
    {
        Context = context;
    }

    public IComponentContext Context { get; }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        var resolver = Context.Resolve<IResolver>();

        var nameKey = resolver.GetName(binder.Name, args);
        var paramList = resolver.GetArguments(args);

        var factory = typeof(T);
        var method = factory.GetMethod(binder.Name);
        if (method.ReturnType.IsGenericType)    //ignore the name of the method
        {
            var csharpBinder = binder.GetType().GetInterface("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");
            var typeArgs = (csharpBinder.GetProperty("TypeArguments").GetValue(binder, null) as IList<Type>);
            var returnType = method.ReturnType.GetGenericTypeDefinition().MakeGenericType(typeArgs.ToArray());
            result = Context.Resolve(returnType, paramList);
            return true;
        }

        if (binder.Name.StartsWith("Get"))
        {
            var key = nameKey.Replace("Get", "");
            result = Context.ResolveNamed(key, method.ReturnType, paramList);
            return true;
        }

        result = Context.Resolve(method.ReturnType, paramList);
        return true;
    }
}
