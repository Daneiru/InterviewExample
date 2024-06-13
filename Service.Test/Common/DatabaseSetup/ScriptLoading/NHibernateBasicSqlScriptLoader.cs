using System.Reflection;

namespace Service.Test.Common.DatabaseSetup.ScriptLoading;

public class NHibernateBasicSqlScriptLoader
{
    private string? LocalPath;
    private string DefaultExtention = ".sql";

    public string BasePath = "";
    public bool AppendSuffix;

    public NHibernateBasicSqlScriptLoader()
    {
        LocalPath = new FileInfo(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath).DirectoryName;

        if (LocalPath == null || LocalPath == "")
            throw new Exception("LocalPath could not be determined!");
    }

    protected string Load(string sqlFilePath)
    {
        string text = sqlFilePath;

        if (AppendSuffix)
            text += DefaultExtention;

        if (LocalPath != null)
            return Path.Combine(LocalPath, BasePath, text);
        else
            throw new Exception("Load could not completed, LocalPath could not be determined!");
    }
}
