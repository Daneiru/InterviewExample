using Infrastructure.Entities;
using LoremNET;
using NHibernate;
using Service.Test.Common.TestDataGenerators;

namespace Service.Test.TestDataGenerators;

internal class ExampleTDG : TDGenerator<ExampleEntity>
{
    public ExampleTDG(TDGeneratorRules rules, ISession databaseSession) : base(rules, databaseSession) { }

    public int Id => Lorem.Integer(60, 320);
    public string Name => Lorem.Sentence(1);
}
