namespace Service.Test.Common.TestDataGenerators;

public class TDGeneratorRuleAssociator<U>
{
    public TDGeneratorRuleAssociator(TDGeneratorRules rules)
    {
        Rules = rules;
    }

    TDGeneratorRules Rules { get; }

    public TDGeneratorRuleAssociator<U> With<T>()
    {
        Rules.Associations[typeof(U)].Add(typeof(T));
        return this;
    }
}
