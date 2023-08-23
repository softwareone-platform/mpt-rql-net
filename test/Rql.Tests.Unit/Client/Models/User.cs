namespace Rql.Tests.Unit.Client.Models;

public record User(string FirstName, string LastName, int Id, Address HomeAddress, Address OfficeAddress)
{
    public string GetName() => $"{FirstName} {LastName}";

    public IList<string> InvoiceIds { get; set; }
}