using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Contracts;


namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IPublishEndpoint _publishEndpoint;


        public ProductsController(IProductService productService, IPublishEndpoint publishEndpoint)
        {
            _productService = productService;
            _publishEndpoint = publishEndpoint;
        }

        // GET /api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var dtos = await _productService.GetAllAsync();
            return Ok(dtos);
        }

        // GET /api/products/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProductDto>> GetById(long id)
        {
            var dto = await _productService.GetByIdAsync(id);
            if (dto == null)
                return NotFound($"Producto con Id: {id} no encontrado");

            return Ok(dto);
        }

        // POST /api/products
        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
        {
            var newProductDto = await _productService.CreateAsync(dto);

            // Crear y publicar el evento
            var productCreated = new ProductCreated(
                Id: newProductDto.Id,
                Name: newProductDto.Name,
                Description: newProductDto.Description,
                Price: newProductDto.Price,
                Stock: newProductDto.Stock,
                Category: newProductDto.Category.Name
            );

            await _publishEndpoint.Publish(productCreated);


            return CreatedAtAction(nameof(GetById), new { id = newProductDto.Id }, newProductDto);
        }

        // PUT /api/products/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductDto dto)
        {
            bool isUpdated = await _productService.UpdateAsync(id, dto);
            if (!isUpdated)
                return NotFound($"Producto con Id: {id} no encontrado");

            await _publishEndpoint.Publish(new ProductUpdated(id, dto.Name, dto.Stock));

            return NoContent(); // 204
        }

        // DELETE /api/products/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var isDeleted = await _productService.DeleteAsync(id);
            if (!isDeleted)
                return NotFound($"Producto con Id: {id} no encontrado");

            await _publishEndpoint.Publish(new ProductDeleted(id));

            return NoContent();
        }
    }
}
