using Inventory.Application.DTOs;

namespace Inventory.Application.Interfaces
{
    public interface IProductService
    {
        Task<ProductDto> CreateAsync(CreateProductDto dto);
        Task<bool> DeleteAsync(long id);
        Task<IEnumerable<ProductDto>> GetAllAsync();
        Task<ProductDto?> GetByIdAsync(long id);
        Task<bool> UpdateAsync(long id, UpdateProductDto dto);
    }
}
