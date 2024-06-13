using Autofac;
using Autofac.Core;

namespace Service.Test.Common.DatabaseSetup.Factory;

public class DefaultTestFactoryResolver : ITestFactoryResolver
{
    public virtual string GetName(string currentName, object[] args)
    {
        return currentName;
    }

    public virtual Parameter[]? GetArguments(object[] args)
    {
        return args?.Select((a, ndx) => new PositionalParameter(ndx, a)).ToArray();
    }
}
