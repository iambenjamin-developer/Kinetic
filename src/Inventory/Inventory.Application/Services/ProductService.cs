using AutoMapper;
using AutoMapper.QueryableExtensions;
using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly InventoryDbContext _context;
        private readonly IMapper _mapper;

        public ProductService(InventoryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            return await _context.Products
                .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<ProductDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Products
                        .Include(x => x.Category)
                        .Where(x => x.Id == id)
                        .FirstOrDefaultAsync();

            if (entity == null) return null;

            return _mapper.Map<ProductDto>(entity);
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto createDto)
        {
            var entity = _mapper.Map<Product>(createDto);
            _context.Products.Add(entity);
            await _context.SaveChangesAsync();

            var dto = await _context.Products
                   .Where(x => x.Id == entity.Id)
                   .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
                   .FirstOrDefaultAsync();

            return dto;
        }

        public async Task<bool> UpdateAsync(long id, UpdateProductDto updateDto)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity == null) return false;

            _mapper.Map(updateDto, entity); // Mapea sobre la entidad existente
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity == null) return false;

            _context.Products.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
