namespace Infrastructure.Common;

public abstract record BaseAuditedEntity : IAuditedEntity
{
    public virtual DateTime LastModifiedDate { get; set; }
    public virtual int LastModifiedByUserId { get; set; }
}
