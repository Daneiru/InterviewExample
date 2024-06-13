using LoremNET;
using NHibernate;

namespace Service.Test.Common.TestDataGenerators;

public abstract class TDGenerator<T> where T : class, new()
{
    protected static readonly Random rng;
    static TDGenerator()
    {
        var seed = new Random().Next();
        rng = new Random(seed);
    }

    protected TDGenerator(TDGeneratorRules rules, ISession database)
    {
        Rules = rules;
        Database = database;
    }

    TDGeneratorRules Rules { get; }
    ISession Database { get; }

    //public T Generate() { } This was a duplicate of DbBuilder so we need to fix that

    public string Words(int maxWords, int maxLength)
    {
        var result = Lorem.Words(1, maxWords);
        if (result.Length > maxLength)
        {
            result = result.Remove(maxLength);
        }
        return result;
    }

    public string Identifier(int maxLength)
    {
        return Words(100, maxLength).Replace(" ", "");
    }
}
