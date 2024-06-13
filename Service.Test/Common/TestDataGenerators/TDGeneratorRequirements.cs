using Service.Test.Common.DatabaseSetup;

namespace Service.Test.Common.TestDataGenerators;

public class TDGeneratorRequirements
{
    public TDGeneratorRequirements(TestDbBuilder dbBuilder)
    {
        DbBuilder = dbBuilder;
    }

    TestDbBuilder DbBuilder { get; }
    internal List<Action> Actions { get; } = new List<Action>();

    public TDGeneratorRequirements Entity<T>() where T : class, new()
    {
        Actions.Add(() => DbBuilder.Create<T>());
        return this;
    }
}
