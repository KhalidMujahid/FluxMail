namespace FluxMail.Core.Models;

public class Contact
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Company { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ContactListMembership> ListMemberships { get; set; } = [];
}

public class ContactList
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ContactListMembership> Memberships { get; set; } = [];
}

public class ContactListMembership
{
    public int ContactId { get; set; }
    public int ContactListId { get; set; }
    public Contact Contact { get; set; } = null!;
    public ContactList ContactList { get; set; } = null!;
}
