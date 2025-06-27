using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Polly;
using SharedKernel.Contracts;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly AsyncPolicy _circuitBreakerPolicy;

        public ProductsController(IProductService productService, IPublishEndpoint publishEndpoint, AsyncPolicy circuitBreakerPolicy)
        {
            _productService = productService;
            _publishEndpoint = publishEndpoint;
            _circuitBreakerPolicy = circuitBreakerPolicy;
        }


        [HttpPost("test-circuit")]
        public async Task<IActionResult> TestCircuitBreaker([FromBody] CreateProductDto dto)
        {
            var eventMessage = new ProductCreated(
                Id: 999,
                Name: "Simulado",
                Description: "Test",
                Price: 100,
                Stock: 5,
                Category: "Test"
            );

            try
            {
                await _circuitBreakerPolicy.ExecuteAsync(() =>
                {
                    // ⚠️ Simular error forzado
                    throw new Exception("❌ Falla simulada para probar Polly Circuit Breaker");
                });

                return Ok("✅ Ejecutado normalmente (no debería ocurrir más de 2 veces)");
            }
            catch (Polly.CircuitBreaker.BrokenCircuitException)
            {
                return StatusCode(503, "⛔ Circuito abierto - Polly está bloqueando la ejecución.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"💥 Error al ejecutar lógica: {ex.Message}");
            }
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

            var eventMessage = new ProductCreated(
                Id: newProductDto.Id,
                Name: newProductDto.Name,
                Description: newProductDto.Description,
                Price: newProductDto.Price,
                Stock: newProductDto.Stock,
                Category: newProductDto.Category.Name
            );

            await _circuitBreakerPolicy.ExecuteAsync(() =>
                _publishEndpoint.Publish(eventMessage, context =>
                {
                    context.SetRoutingKey("product.created");
                }));

            return CreatedAtAction(nameof(GetById), new { id = newProductDto.Id }, newProductDto);
        }

        // PUT /api/products/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductDto dto)
        {
            bool isUpdated = await _productService.UpdateAsync(id, dto);
            if (!isUpdated)
                return NotFound($"Producto con Id: {id} no encontrado");

            var eventMessage = new ProductUpdated(id, dto.Name, dto.Stock);

            await _circuitBreakerPolicy.ExecuteAsync(() =>
                _publishEndpoint.Publish(eventMessage, context =>
                {
                    context.SetRoutingKey("product.updated");
                }));

            return NoContent();
        }

        // DELETE /api/products/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var isDeleted = await _productService.DeleteAsync(id);
            if (!isDeleted)
                return NotFound($"Producto con Id: {id} no encontrado");

            var eventMessage = new ProductDeleted(id);

            await _circuitBreakerPolicy.ExecuteAsync(() =>
                _publishEndpoint.Publish(eventMessage, context =>
                {
                    context.SetRoutingKey("product.deleted");
                }));

            return NoContent();
        }
    }
}
