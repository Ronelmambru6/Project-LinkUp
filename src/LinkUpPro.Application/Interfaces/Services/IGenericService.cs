namespace LinkUpPro.Application.Interfaces.Services
{
    public interface IGenericService<TDto> where TDto : class
    {
        Task<TDto?> GetByIdAsync(int id);
        Task<List<TDto>> GetAllAsync();
        Task<TDto> AddAsync(TDto dto);
        Task<bool> UpdateAsync(int id, TDto dto);
        Task<bool> DeleteAsync(int id);
    }
}