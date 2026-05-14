using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxMail.Infrastructure.Repositories;

public class ContactRepository(AppDbContext db) : IContactRepository
{
    public Task<List<Contact>> GetAllAsync() =>
        db.Contacts.Where(c => !c.IsUnsubscribed && !c.IsBounced).OrderBy(c => c.Name).ToListAsync();

    public Task<List<Contact>> GetAllIncludingSuppressedAsync() =>
        db.Contacts.OrderBy(c => c.Name).ToListAsync();

    public Task<List<Contact>> GetByListIdAsync(int listId) =>
        db.ContactListMemberships
            .Where(m => m.ContactListId == listId)
            .Select(m => m.Contact)
            .Where(c => !c.IsUnsubscribed && !c.IsBounced)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public Task<Contact?> GetByIdAsync(int id) =>
        db.Contacts.FindAsync(id).AsTask();

    public async Task<int> AddAsync(Contact contact)
    {
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        return contact.Id;
    }

    public async Task UpdateAsync(Contact contact)
    {
        db.Contacts.Update(contact);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.Contacts.FindAsync(id);
        if (entity is not null)
        {
            db.Contacts.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task<int> ImportAsync(IEnumerable<Contact> contacts)
    {
        var list = contacts.ToList();
        var existingEmails = await db.Contacts.Select(c => c.Email).ToHashSetAsync();
        var newContacts = list.Where(c => !existingEmails.Contains(c.Email)).ToList();
        db.Contacts.AddRange(newContacts);
        await db.SaveChangesAsync();
        return newContacts.Count;
    }

    public Task<List<ContactList>> GetAllListsAsync() =>
        db.ContactLists.OrderBy(l => l.Name).ToListAsync();

    public Task<ContactList?> GetListByIdAsync(int id) =>
        db.ContactLists.Include(l => l.Memberships).FirstOrDefaultAsync(l => l.Id == id);

    public async Task<int> AddListAsync(ContactList list)
    {
        db.ContactLists.Add(list);
        await db.SaveChangesAsync();
        return list.Id;
    }

    public async Task DeleteListAsync(int id)
    {
        var entity = await db.ContactLists.FindAsync(id);
        if (entity is not null)
        {
            db.ContactLists.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task AddToListAsync(int contactId, int listId)
    {
        var exists = await db.ContactListMemberships
            .AnyAsync(m => m.ContactId == contactId && m.ContactListId == listId);
        if (!exists)
        {
            db.ContactListMemberships.Add(new ContactListMembership
            {
                ContactId = contactId,
                ContactListId = listId
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task RemoveFromListAsync(int contactId, int listId)
    {
        var entity = await db.ContactListMemberships
            .FirstOrDefaultAsync(m => m.ContactId == contactId && m.ContactListId == listId);
        if (entity is not null)
        {
            db.ContactListMemberships.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task<string?> FindListNameForEmailAsync(string email)
    {
        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Email == email);
        if (contact is null) return null;

        var membership = await db.ContactListMemberships
            .FirstOrDefaultAsync(m => m.ContactId == contact.Id);
        if (membership is null) return null;

        var list = await db.ContactLists.FindAsync(membership.ContactListId);
        return list?.Name;
    }

    public async Task<string?> FindNameForEmailAsync(string email)
    {
        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Email == email);
        return string.IsNullOrWhiteSpace(contact?.Name) ? null : contact.Name;
    }

    public async Task UnsubscribeAsync(int id)
    {
        var contact = await db.Contacts.FindAsync(id);
        if (contact is null) return;
        contact.IsUnsubscribed = true;
        await db.SaveChangesAsync();
    }

    public async Task UnsubscribeByEmailAsync(string email)
    {
        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Email == email);
        if (contact is null) return;
        contact.IsUnsubscribed = true;
        await db.SaveChangesAsync();
    }

    public async Task MarkBouncedAsync(int id)
    {
        var contact = await db.Contacts.FindAsync(id);
        if (contact is null) return;
        contact.IsBounced = true;
        await db.SaveChangesAsync();
    }
}
