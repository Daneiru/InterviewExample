using Infrastructure.Enums;
using Infrastructure.NHibernateDatabase.Interfaces;
using LoremNET;
using Moq;
using NHibernate;
using Service.Test.Common.TestDataGenerators;
using System.Reflection;
using System.Text;

namespace Service.Test.Common.DatabaseSetup;

public class TestDbBuilder : IDisposable
{
    public INHibernateDatabase Database { get; }
    public ITDGeneratorFactory GeneratorFactory { get; }
    TDGeneratorRules Rules { get; }
    TDGeneratorRequirements Requirements { get; }

    public TestDbBuilder(INHibernateDatabaseFactory database, TDGeneratorRules rules, ITDGeneratorFactory generatorFactory, NHibernateDatabaseType dbType)
    {
        switch (dbType)
        {
            case NHibernateDatabaseType.DbA:
                Database = database.GetDbADataSession();
                break;
            case NHibernateDatabaseType.DbC:
                Database = database.GetDbCDataSession();
                break;
            default:
                throw new Exception("Other Dbs not setup to be supported at this time.");
        }

        GeneratorFactory = generatorFactory;
        Database.BeginTransaction();

        Rules = rules;
        Requirements = new TDGeneratorRequirements(this);
    }

    public TestDbBuilder Create<T>(Action<T>? configurator = null) where T : class, new()
    {
        T? obj = null;
        Mock<T>? mockObj = null;

        if (configurator == null)
        {
            Harvest(out obj, out mockObj);
        }
        else
        {
            Configure(configurator, out obj, out mockObj);
        }

        // TODO: Moq changed how thier object works at some point. I figured out a way to fix this originally but I dont recall what the solution was off-hand
        var config = mockObj.Invocations.ToDictionary(k => k.Method.Name.Replace("set_", ""), v => v.Arguments.Single());
        var generator = GeneratorFactory.GetGenerator<T>();

        var genProps = generator.GetType().GetProperties().ToDictionary(k => k.Name, value => value);
        var props = typeof(T).GetProperties().Where(p => p.CanWrite);

        var result = new T();
        foreach (var p in props)
        {
            try
            {
                var isSet = config.TryGetValue(p.Name, out var currentValue);
                if (isSet)
                {
                    p.SetValue(result, currentValue);
                    continue;
                }

                if (Rules.Associations.TryGetValue(p.PropertyType, out var list) && list.Any(item => item == typeof(T)))
                {
                    var value = Rules.GetOrAdd(p.PropertyType, () => genProps[p.Name].GetValue(generator));
                    p.SetValue(result, value);
                    Database.Session.SaveOrUpdate(value);
                    continue;
                }

                // TODO: This greatly duplicates what we're doing in Generator, we need to refactor this.
                // Attempt to resolve a value when none/null was given
                var genValue = genProps[p.Name].GetValue(generator);

                if (genValue == null)
                {
                    var propType = p.PropertyType.Name;

                    switch (propType)
                    {
                        case "String":
                            p.SetValue(result, Lorem.Words(1));
                            break;
                        case "Int":
                            p.SetValue(result, Lorem.Integer(1, 9999));
                            break;
                        case "Decimal":
                            p.SetValue(result, Lorem.Integer(1, 9999));
                            break;
                        case "DateTime":
                            p.SetValue(result, DateTime.UtcNow);
                            break;
                        default:
                            Console.WriteLine("Warning: Property '{0}' of entity '{1}' is not defined in the generator {2}, and a default could not be determined! Using null.", p.Name, result, this);
                            break;
                    }
                }
                // If the prop is a nested entity, save it explicitly
                else
                {
                    p.SetValue(result, genValue);
                    var genType = genValue.GetType();
                    if (genType.IsClass && genType.Namespace.Contains("Entities"))
                    {
                        Database.Session.Save(genValue);
                    }
                }
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("Property '{0}' of entity '{1}' is not defined in the generator {2}", p.Name, result, this);
                throw;
            }
        }

        try
        {
            // Attempt to commit generated object, use SQL Query string as fallback if this fails
            Database.Session.Save(result);
            generator = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not use traditional Db record generation, attempting SQL genration...");
            Console.WriteLine(ex.Message);

            var sqlQuery = GenerateSQLQuery(props, result);
            var query = Database.Session.CreateSQLQuery(sqlQuery);
            query.ExecuteUpdate();
        }

        return this;
    }

    private string GenerateSQLQuery<T>(IEnumerable<PropertyInfo> props, T result) where T : class, new()
    {
        Type entityType = typeof(T);
        if (typeof(T).Name.StartsWith("Meta"))
            entityType = typeof(T).BaseType;

        var constType = Type.GetType($"Interplx.Core.Entities.ExpenseData.Mapping.Constants.{entityType.Name}, Interplx.Core.Entities.ExpenseData");
        var consts = constType.GetFields();
        var tableName = consts.First(c => c.Name == "Context").GetValue(null);

        var propsBase = typeof(T).GetProperties();
        var extraProps = propsBase.Where(p => p.GetCustomAttribute(typeof(DbColumnAttribute)) != null);

        // Need to add any extras that happen to be missing due to protected/missing setters
        props = props.Union(extraProps);

        var queryBuilder = new StringBuilder();
        queryBuilder.AppendLine($"SET IDENTITY_INSERT [dbo].[{tableName}] ON");
        queryBuilder.Append($"INSERT [dbo].[{tableName}] (");

        var queryValuesBuilder = new StringBuilder();
        queryValuesBuilder.Append("VALUES (");

        foreach (var prop in props)
        {
            var propValue = prop.GetValue(result);
            var column = consts.FirstOrDefault(c => c.Name == prop.Name)?.GetValue(null).ToString();
            var propColumn = extraProps.FirstOrDefault(ep => ep.Name == prop.Name);

            // In case we have a special attribute flagged column, double check
            if (propColumn != null)
            {
                // TODO: Can add extra logic here based on the attribute itself 
                if (column == null)
                    column = propColumn.Name;
            }

            if (propValue != null && column != null)
            {
                queryBuilder.Append($"[{column}], ");

                // TODO: This currently cannot support nullable decimals, or DateTimes. Need to implement a work around for that
                switch (prop.PropertyType.Name)
                {
                    case "DateTime":
                        queryValuesBuilder.Append($"CAST('{propValue}' AS DateTime), ");
                        break;
                    case "String":
                        queryValuesBuilder.Append($"'{propValue}', ");
                        break;
                    case "Byte[]":
                        queryValuesBuilder.Append($"0x90245F92BA8240ED, ");
                        break;
                    case "Decimal":
                    case "Double":
                        queryValuesBuilder.Append($"CAST({propValue} AS Decimal(18, 2)), ");
                        break;
                    case "Boolean":
                        if ((bool)propValue)
                            queryValuesBuilder.Append("1, ");
                        else
                            queryValuesBuilder.Append("0, ");
                        break;
                    default:
                        if (prop.PropertyType.BaseType.Name == "Enum")
                        {
                            if (prop.PropertyType.Name == "TranMode")
                                // TODO: Do we need DEL in here??
                                queryValuesBuilder.Append($"'INS', ");
                            else if (prop.PropertyType.Name == "PaymentType")
                                // Stupid employee table saves this as an int, even tho all our other Enums are strings
                                queryValuesBuilder.Append($"1, ");
                            else if (prop.PropertyType.Name == "EmployeeStatus")
                                // Stupid employee table saves this as an int, even tho all our other Enums are strings
                                queryValuesBuilder.Append($"1, ");
                            else if (prop.PropertyType.Name == "CredentialStatus")
                                // Stupid AppUser table saves this as an nvar(1)
                                queryValuesBuilder.Append($"1, ");
                            else
                                queryValuesBuilder.Append($"'{propValue}', ");
                        }
                        else if (prop.PropertyType.BaseType.Name == "BaseEntity")
                        {
                            // Need to attempt to get the ID 
                            var idProp = prop.PropertyType.GetProperties().FirstOrDefault(p => p.Name.Equals("ID", StringComparison.InvariantCultureIgnoreCase)
                                                                                          || p.Name.Equals($"{column}Key", StringComparison.InvariantCultureIgnoreCase)
                                                                                          || p.Name.Equals($"{column}_Key", StringComparison.InvariantCultureIgnoreCase)
                                                                                          || p.Name.Equals($"{column}Id", StringComparison.InvariantCultureIgnoreCase));

                            if (idProp == null)
                                // Note: if you hit this, you'll need to add another entry above to cover this entities PK column name
                                throw new Exception("Unable to determine what the PK for this is!!");

                            var id = idProp.GetValue(propValue);
                            if (idProp.GetType().Name == "String")
                                queryValuesBuilder.Append($"'{id}', ");
                            else
                                queryValuesBuilder.Append($"{id}, ");
                        }
                        else
                            queryValuesBuilder.Append($"{propValue}, ");
                        break;
                }
            }
        }

        // Need to remove the trailing comma and space
        queryBuilder.Remove(queryBuilder.Length - 2, 2);
        queryValuesBuilder.Remove(queryValuesBuilder.Length - 2, 2);

        queryBuilder.AppendLine(")");
        queryBuilder.AppendLine(queryValuesBuilder.ToString() + ")");
        queryBuilder.AppendLine($"SET IDENTITY_INSERT [dbo].[{tableName}] OFF");
        return queryBuilder.ToString();
    }

    private static void Configure<T>(Action<T> configurator, out T obj, out Mock<T> mockObj) where T : class, new()
    {
        obj = Mock.Of<T>();
        mockObj = Mock.Get(obj);
        mockObj.SetupAllProperties();
        configurator.Invoke(obj);
    }

    private void Harvest<T>(out T obj, out Mock<T> mockObj) where T : class, new()
    {
        obj = Rules.Garden.FirstOrDefault(item => typeof(T).IsAssignableFrom(item.GetType())) as T ?? Mock.Of<T>();
        var ndx = Rules.Garden.FindIndex(item => typeof(T).IsAssignableFrom(item.GetType()));

        if (ndx != -1)
            Rules.Garden.RemoveAt(ndx);

        mockObj = Mock.Get(obj);
    }

    public TDGeneratorRules Rule()
    {
        return Rules;
    }

    public TDGeneratorRequirements Requires()
    {
        return Requirements;
    }

    public void Generate(int times = 1)
    {

        for (var ndx = 0; ndx < times; ndx++)
        {
            Requirements.Actions.ForEach(a => a.Invoke());
            Rules.Cache.Clear();
        }

        Database.Session.Flush();
        Database.Session.Clear();  //required to clear first level cache so code will re-retrieve from DB rather than memory.
    }

    public void Plant<T>(Action<T> configurator) where T : class
    {
        var obj = Mock.Of<T>();
        var mockObj = Mock.Get(obj);
        mockObj.SetupAllProperties();
        configurator?.Invoke(obj);

        Rules.Garden.Add(obj);
    }

    public void Dispose()
    {
        Database.Session.GetCurrentTransaction()?.Dispose();
        Database.Dispose();
    }
}
