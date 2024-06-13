namespace Service.Test.Common.TestDataGenerators;

public class TDGeneratorRules
{
    internal Dictionary<Type, List<Type>> Associations = new Dictionary<Type, List<Type>>();
    internal List<object> Garden = new List<object>();

    //this eventually needs to be read-only for code outside of system
    public Dictionary<Type, object> Cache = new Dictionary<Type, object>();

    public TDGeneratorRuleAssociator<T> Share<T>()
    {
        var list = new List<Type>();
        Associations.Add(typeof(T), list);
        return new TDGeneratorRuleAssociator<T>(this);
    }

    public object GetOrAdd(Type lookup, Func<object> creator)
    {
        var inCache = Cache.TryGetValue(lookup, out var value);
        if (!inCache)
        {
            value = creator.Invoke();
            Cache.Add(lookup, value);
        }
        return value;
    }
}
