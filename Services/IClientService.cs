namespace ClientCore.Business.Services
{
    public interface IClientService
    {
        Task<string> AddAsync(CreateClientDTO createClientDTO);
        Task<List<GetClientDTO>> GetAllAsync();
        Task<GetClientDTO> GetAsync(string id);
        Task LinkContactAsync(LinkContactDTO linkContactDTO);
    }
}
