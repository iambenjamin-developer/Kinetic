using Inventory.API.Producers;
using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        //private readonly IPublishEndpoint _publishEndpoint;
        //private readonly QueueProducerService _queueProducerService;

        public ProductsController(IProductService productService)//, IPublishEndpoint publishEndpoint, QueueProducerService queueProducerService)
        {
            _productService = productService;
            //_publishEndpoint = publishEndpoint;
            //_queueProducerService = queueProducerService;
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
            var created = await _productService.CreateAsync(dto);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT /api/products/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductDto dto)
        {
            bool isUpdated = await _productService.UpdateAsync(id, dto);
            if (!isUpdated)
                return NotFound($"Producto con Id: {id} no encontrado");

            return NoContent(); // 204
        }

        // DELETE /api/products/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var isDeleted = await _productService.DeleteAsync(id);
            if (!isDeleted)
                return NotFound($"Producto con Id: {id} no encontrado");

            return NoContent();
        }
    }
}
