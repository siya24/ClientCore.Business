namespace ClientCore.Business.Services
{
    public interface IContactService
    {
        Task<string> AddAsync(CreateContactDTO createContactDTO);
        Task<List<GetContactDTO>> GetAllAsync();
        Task<List<GetContactDTO>> GetContactsAsync(string clientId);
    }
}
