using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;

namespace Service.Test.Common.DatabaseSetup;

static class DbInfo
{
    private static readonly ConcurrentDictionary<string, List<string>> TemplateLocations = new ConcurrentDictionary<string, List<string>>();

    private static object SyncRoot = new object();
    static List<string> GetDbTemplatePaths(string databaseType, SqlConnection connection)
    {
        List<string> paths = new List<string>();
        EnsureDbOffline(databaseType, connection);

        using (SqlCommand queryCmd = connection.CreateCommand())
        {
            queryCmd.CommandText = $"select dbs.name, dbs.state_desc, mf.physical_name from sys.databases dbs join sys.master_files mf on mf.database_id = dbs.database_id where dbs.name = '{databaseType}Data_db_template'"; ;
            var reader = queryCmd.ExecuteReader();

            try
            {
                if (!reader.HasRows)
                {
                    // An exception is thrown so no need to return
                    throw new Exception($"Could not find {databaseType}Data_db_template database.");
                }

                while (reader.Read())
                {
                    string sourcePath = reader.GetString(2); // NOTE: When its taken offline it adds a "state" column to index 1
                    paths.Add(sourcePath);
                }
            }
            finally
            {
                reader.Close();
            }
        }
        return paths;
    }

    static void EnsureDbOffline(string databaseType, SqlConnection connection)
    {
        var isOnline = false;
        using (SqlCommand queryCmd = connection.CreateCommand())
        {
            queryCmd.CommandText = $"select dbs.name, dbs.state_desc, mf.physical_name from sys.databases dbs join sys.master_files mf on mf.database_id = dbs.database_id where dbs.name = '{databaseType}Data_db_template' and dbs.state_desc = 'ONLINE'"; ;
            var reader = queryCmd.ExecuteReader();
            isOnline = reader.HasRows;
            reader.Close();
        }
        if (isOnline)
        {
            // DB Must be set OFFLINE so we can copy files
            using (SqlCommand offlineCmd = connection.CreateCommand())
            {
                offlineCmd.CommandText = $"ALTER DATABASE [{databaseType}Data_db_template] SET OFFLINE";
                offlineCmd.ExecuteNonQuery();
            }
        }
    }

    public static IReadOnlyCollection<string> GetPaths(string databaseType, SqlConnection connection)
    {
        //concurrent dictioinary ensure only one entry and prevent race conditions
        lock (SyncRoot)
        {
            return TemplateLocations.GetOrAdd(databaseType, dbType => GetDbTemplatePaths(dbType, connection)).AsReadOnly();
        }
    }
}
