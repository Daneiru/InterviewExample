namespace Infrastructure.Common;

public interface IAuditedEntity
{
    int LastModifiedByUserId { get; set; }
    DateTime LastModifiedDate { get; set; }
}
