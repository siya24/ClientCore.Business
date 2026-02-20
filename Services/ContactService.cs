
using ClientCore.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ClientCore.Business.Services
{
    public class ContactService(IMapper mapper, ClientCoreDBContext context) : IContactService
    {
        private readonly IMapper _mapper = mapper;
        private readonly ClientCoreDBContext _context = context;
        public async Task<string> AddAsync(CreateContactDTO createContactDTO)
        {
            if (createContactDTO is null)
            {
                throw new Exception("contact cannot be null");
            }

            try
            {
                var contact = _mapper.Map<Contact>(createContactDTO);
                contact.Id = Guid.NewGuid().ToString();
                await _context.Contacts.AddAsync(contact);
                await _context.SaveChangesAsync();

                return contact.Id;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while adding the contact.", ex);
            }
        }

        public async Task<List<GetContactDTO>> GetAllAsync()
        {
            try
            {
                var contacts = await _context.Contacts
                   .OrderBy(contact => contact.Name)
                   .ThenBy(contact => contact.Surname)
                   .Select(c => new GetContactDTO
                     {
                         Id = c.Id,
                         FullName = $"{c.Name} {c.Surname}",
                         Email = c.Email
                     })
                   .ToListAsync();
                   
                var contactDto = _mapper.Map<List<GetContactDTO>>(contacts);

                return contactDto;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving contact.", ex);
            }
        }
        public async Task<List<GetContactDTO>> GetContactsAsync(string clientId)
        {
            try
            {
                var contacts = await _context.Contacts
                   .Where(c => c.Clients.Any(cc => cc.ClientId == clientId))
                   .Include(i => i.Clients)
                   .OrderBy(contact => contact.Name)
                   .ThenBy(contact => contact.Surname)
                   .Select(c => new GetContactDTO
                   {
                       Id = c.Id,
                       FullName = $"{c.Name} {c.Surname}",
                       Email = c.Email
                   })
                   .ToListAsync();

                var contactDto = _mapper.Map<List<GetContactDTO>>(contacts);

                return contactDto;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving contact.", ex);
            }
        }

        public async Task LinkContactToClientsAsync(string contactId, List<string> clientIds)
        {
            if (clientIds == null || !clientIds.Any())
                return;

            try
            {
                foreach (var clientId in clientIds)
                {
                    // Check if link already exists
                    var exists = await _context.ClientContacts
                        .AnyAsync(cc => cc.ClientId == clientId && cc.ContactId == contactId);

                    if (!exists)
                    {
                        var clientContact = new ClientContact
                        {
                            Id = Guid.NewGuid().ToString(),
                            ClientId = clientId,
                            ContactId = contactId
                        };

                        await _context.ClientContacts.AddAsync(clientContact);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while linking contact to clients.", ex);
            }
        }

     }
}
