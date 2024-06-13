namespace Service.Test.Common.TestDataGenerators;

public interface ITDGeneratorFactory
{
    TDGenerator<T> GetGenerator<T>() where T : class, new();
}
