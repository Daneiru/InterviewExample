namespace Service.Test.Common.DatabaseSetup;

public class DbColumnAttribute
{
    public DbColumnAttribute(string? columnName = null)
    {
        ColumnName = columnName;
    }

    public string? ColumnName { get; set; }
    // TODO: Would be nice to add a ColumnIgnore bool too
}
