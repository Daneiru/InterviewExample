using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Text;
using System.Configuration;

namespace Service.Test.Common.DatabaseSetup;

public class LocalDBFunctions
{
    static object SyncRoot = new object();

    private static ConcurrentDictionary<string, SqlConnection> Connections = new ConcurrentDictionary<string, SqlConnection>();

    static SqlConnection GetConnection(string dataSource)
    {
        lock (SyncRoot)
        {
            return Connections.GetOrAdd(dataSource, source =>
            {
                var conn = new SqlConnection(new SqlConnectionStringBuilder
                {
                    InitialCatalog = "master",
                    DataSource = source,
                    IntegratedSecurity = true,
                    ConnectTimeout = 30,
                    ConnectRetryCount = 5,
                }.ConnectionString);
                conn.Open();
                using (var transaction = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                {
                    transaction.Commit();
                }
                return conn;
            });
        }
    }

    /// <summary>
    /// Utilizes SQL Create Db via ATTACH to create duplications of our test Db
    /// Db files are first copied for use in the ATTACH process so no conflicts occur,
    /// and the files aren't lost when we later drop the Db during cleanup.
    /// </summary>
    /// <returns>Connection string builder, with updated InitialCatalog for the new Db</returns>
    public static SqlConnectionStringBuilder DeployLocalDb(string databaseType, string dataSource = @"(localdb)\MSSQLLocalDb")
    {
        var sb = new StringBuilder();
        var databaseName = $"{databaseType}_{Guid.NewGuid().ToString("N")}";
        string dbCopyPath = Path.Combine(CurrDir(), "SQLDbFiles", databaseName);
        Directory.CreateDirectory(dbCopyPath); // Ensure db folder exists
        var connection = GetConnection(dataSource);


        var templatePaths = DbInfo.GetPaths(databaseType, connection);
        // Dropping the Db will remove files we utilized, ensure they exist to attach!
        // Copy all the files & Replaces any files with the same name, build the files portion of the SqlCommand
        StringBuilder dbFiles = new StringBuilder();
        foreach (string templatePath in templatePaths)
        {
            string filenName = templatePath.Split('\\').Last();
            string newFile = Path.Combine(dbCopyPath, filenName.Insert(filenName.Length - 4, $"_{databaseName}"));  // Append GUID for async tests

            File.Copy(templatePath, newFile, true);
            dbFiles.AppendFormat("\n(FILENAME = '{0}'),", newFile);
        }

        // Remove the trailing "," from the Sql
        var files = dbFiles.ToString().Substring(0, dbFiles.Length - 1);
        lock (SyncRoot)
        {
            using (SqlCommand createCommand = connection.CreateCommand())
            {
                createCommand.CommandText = $"CREATE DATABASE [{databaseName}] ON " + files + "\nFOR ATTACH;";
                createCommand.ExecuteNonQuery();
            }
        }

        return new SqlConnectionStringBuilder
        {
            InitialCatalog = databaseName,
            DataSource = dataSource,
            IntegratedSecurity = true
        };
    }

    /// <summary>
    /// Drops the InitialCatalog if it exists. This will cleanup the Db we created, 
    /// and delete any remaining files we used to create it.
    /// </summary>
    public static void CleanupLocalDB(SqlConnectionStringBuilder scsb)
    {
        var dbToDrop = scsb.InitialCatalog;
        var connection = GetConnection(scsb.DataSource);

        lock (SyncRoot)
        {

            using (SqlCommand killSessions = connection.CreateCommand())
            {
                killSessions.CommandText = $@"Declare @kill varchar(8000) = '';
                                                    Select @kill = @kill + 'kill ' + Convert(varchar(5), session_id) + ';'
                                                    From sys.dm_exec_sessions Where database_id = db_id('{dbToDrop}')

                                                    Exec(@kill)";
                killSessions.ExecuteNonQuery();
            }

            using (SqlCommand dropCmd = connection.CreateCommand())
            {
                dropCmd.CommandText = $"ALTER DATABASE [{dbToDrop}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                dropCmd.ExecuteNonQuery();
            }

            using (SqlCommand dropCmd = connection.CreateCommand())
            {
                dropCmd.CommandText = $"DROP DATABASE IF EXISTS {dbToDrop}";
                dropCmd.ExecuteNonQuery();
            }

            string dbPath = Path.Combine(CurrDir(), "SQLDbFiles", dbToDrop);
            Directory.Delete(dbPath, true);
        }
    }

    private static string CurrDir()
    {
        // On Azure, most places are locked down. Use the User's temp dir so we have access
        return Path.GetTempPath();
    }

    public static SqlConnectionStringBuilder CreateSqlConnectionStringBuilder(string connectionName)
    {
        SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings[connectionName].ConnectionString);
        var path = new Uri(System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath;
        Match match = Regex.Match(scsb.AttachDBFilename, @"^(.+)\\(.+)\.mdf$");
        string WorkingDatabasePath = Path.Combine(new FileInfo(path).DirectoryName, Guid.NewGuid().ToString());
        scsb.AttachDBFilename = Path.Combine(WorkingDatabasePath, match.Groups[2].Value + ".mdf");

        return scsb;
    }

    public static string CopyLocalDB(SqlConnectionStringBuilder scsb)
    {
        var fileInfo = new FileInfo(scsb.AttachDBFilename);
        var databaseName = fileInfo.Name.Replace(fileInfo.Extension, null);
        var location = fileInfo.Directory.Parent.FullName;
        string WorkingDatabasePath = fileInfo.DirectoryName;
        Directory.CreateDirectory(WorkingDatabasePath);

        //now assumes that the App_Data directory is a sibling of the working db directory
        File.Copy(Path.Combine(location, "App_Data", databaseName + ".mdf"), Path.Combine(WorkingDatabasePath, databaseName + ".mdf"), true);
        File.Copy(Path.Combine(location, "App_Data", databaseName + "_log.ldf"), Path.Combine(WorkingDatabasePath, databaseName + "_log.ldf"), true);

        return WorkingDatabasePath;

    }

    public static SqlConnectionStringBuilder CreateLocalDB()
    {
        Guid dbGuid = Guid.NewGuid();
        string outputFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "App_Data", dbGuid.ToString());
        string mdfFilename = $"ldb_{dbGuid.ToString("N")}";
        string dbFileName = Path.Combine(outputFolder, $"{mdfFilename}.mdf");
        string logFileName = Path.Combine(outputFolder, $"{mdfFilename}_log.ldf");
        // Create Data Directory If It Doesn't Already Exist.
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        CreateDatabase(mdfFilename, dbFileName);

        string connectionString = string.Format(@"Data Source=(LocalDB)\v12.0;Initial Catalog={0};AttachDBFileName={1};Integrated Security=True;", mdfFilename, dbFileName);
        return new SqlConnectionStringBuilder(connectionString);
    }

    private static bool CreateDatabase(string dbName, string dbFileName)
    {
        string connectionString = string.Format(@"Data Source=(LocalDB)\v12.0;Initial Catalog=master;Integrated Security=True");
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            SqlCommand cmd = connection.CreateCommand();

            cmd.CommandText = string.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')", dbName, dbFileName);
            cmd.ExecuteNonQuery();
        }

        return File.Exists(dbFileName);
    }

    /// <summary>
    /// Creates an Empty, named database
    /// </summary>
    /// <param name="dbName">Prefix used to identify databases created by this method. Defaults to 'TestingDB'</param>
    /// <param name="dataSource">The database engine to attach the database to. Defaults to '(localdb)\MSSQLLocalDB'</param>
    /// <returns></returns>
    public static SqlConnectionStringBuilder GenerateDatabase(string dbName = "TestingDB", string dataSource = @"(LocalDB)\MSSQLLocalDB")
    {
        var databaseName = string.Format("{0}_{1:N}", dbName, Guid.NewGuid());
        var tmpPath = Path.Combine(Path.GetTempPath(), "SQLDbFiles", databaseName);
        Directory.CreateDirectory(tmpPath);
        string path = Path.Combine(CurrDir(), "SQLDbFiles", databaseName, databaseName + ".mdf");
        string connectionString = string.Format($"Data Source={dataSource};Initial Catalog=master;Integrated Security=True");
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            SqlCommand cmd = connection.CreateCommand();

            cmd.CommandText = string.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')", databaseName, path);
            cmd.ExecuteNonQuery();
        }
        return new SqlConnectionStringBuilder
        {
            InitialCatalog = databaseName,
            DataSource = dataSource,
            IntegratedSecurity = true
        };
    }
}
