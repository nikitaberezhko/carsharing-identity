using Ardalis.SmartEnum;

namespace Domain.AccountAggregate;

public class Status(string value, int id) : SmartEnum<Status>(value, id)
{
    public int Id { get; } = id;
    
    public static readonly Status Approved = new Status("Approved", 0);
    
    public static readonly Status PendingConfirmation = new Status("Pending confirmation", 1);
    
    public static readonly Status PendingApproval = new Status("Pending approval", 2);
    
    public static readonly Status Deactivated = new Status("Deactivated", 3);
    
    public static readonly Status Deleted = new Status("Deleted", 4);
    
    
    
    public static bool operator == (Status a, Status b) => a.Value == b.Value;
    
    public static bool operator != (Status a, Status b) => a.Value != b.Value;
}