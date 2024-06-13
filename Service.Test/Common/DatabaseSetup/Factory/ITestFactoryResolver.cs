using Autofac.Core;

namespace Service.Test.Common.DatabaseSetup.Factory;

public interface ITestFactoryResolver
{
    Parameter[]? GetArguments(object[] args);
    string GetName(string currentName, object[] args);
}
