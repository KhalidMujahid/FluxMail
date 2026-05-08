using FluxMail.Core.Models;

namespace FluxMail.Core.Interfaces;

public interface IContactRepository
{
    Task<List<Contact>> GetAllAsync();
    Task<List<Contact>> GetByListIdAsync(int listId);
    Task<Contact?> GetByIdAsync(int id);
    Task<int> AddAsync(Contact contact);
    Task UpdateAsync(Contact contact);
    Task DeleteAsync(int id);
    Task<int> ImportAsync(IEnumerable<Contact> contacts);

    Task<List<ContactList>> GetAllListsAsync();
    Task<ContactList?> GetListByIdAsync(int id);
    Task<int> AddListAsync(ContactList list);
    Task DeleteListAsync(int id);
    Task AddToListAsync(int contactId, int listId);
    Task RemoveFromListAsync(int contactId, int listId);

    Task<string?> FindListNameForEmailAsync(string email);
    Task<string?> FindNameForEmailAsync(string email);
}
