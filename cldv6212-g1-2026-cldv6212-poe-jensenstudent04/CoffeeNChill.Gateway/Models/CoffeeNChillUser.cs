using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Gateway.Models;

/// <summary>
/// One row of the Azure Table "Users". PartitionKey = Role (Customer, Staff or Admin),
/// RowKey = lower-cased email, exactly as the Part 3 brief specifies.
/// </summary>
public class CoffeeNChillUser : ITableEntity
{
    public string PartitionKey { get; set; } = Roles.Customer; // the user's role
    public string RowKey { get; set; } = "";                   // normalised email
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";             // salted hash, never the password
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
