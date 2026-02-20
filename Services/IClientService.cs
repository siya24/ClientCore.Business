namespace ClientCore.Business.Services
{
    public interface IClientService
    {
        Task<string> AddAsync(CreateClientDTO createClientDTO);
        Task<List<GetClientDTO>> GetAllAsync();
    }
}
