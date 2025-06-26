using Inventory.Application.DTOs;

namespace Inventory.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
    }
}
