using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using SharedKernel.Contracts;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly AsyncPolicy _resiliencePolicy;

        public ProductsController(IProductService productService, IPublishEndpoint publishEndpoint, AsyncPolicy resiliencePolicy)
        {
            _productService = productService;
            _publishEndpoint = publishEndpoint;
            _resiliencePolicy = resiliencePolicy;
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
            try
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


                await _resiliencePolicy.ExecuteAsync(cancellationToken =>
                    _publishEndpoint.Publish(eventMessage, publishCtx =>
                    {
                        publishCtx.SetRoutingKey("product.created");
                    }, cancellationToken),
                    CancellationToken.None);

                return CreatedAtAction(nameof(GetById), new { id = newProductDto.Id }, newProductDto);
            }
            catch (TimeoutRejectedException ex)
            {
                return StatusCode(504, "⏳ Tiempo de espera agotado al publicar el evento.");
            }
            catch (BrokenCircuitException ex)
            {
                return StatusCode(503, "⛔ Circuito abierto - el servicio de mensajería no está disponible.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"💥 Error inesperado: {ex.Message}");
            }
        }

        // PUT /api/products/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductDto dto)
        {
            try
            {
                bool isUpdated = await _productService.UpdateAsync(id, dto);
                if (!isUpdated)
                    return NotFound($"Producto con Id: {id} no encontrado");

                var eventMessage = new ProductUpdated(id, dto.Name, dto.Stock);

                await _resiliencePolicy.ExecuteAsync(cancellationToken =>
                    _publishEndpoint.Publish(eventMessage, context =>
                    {
                        context.SetRoutingKey("product.updated");
                    }, cancellationToken),
                    CancellationToken.None);

                return NoContent();
            }
            catch (TimeoutRejectedException ex)
            {
                return StatusCode(504, "⏳ Tiempo de espera agotado al publicar el evento.");
            }
            catch (BrokenCircuitException ex)
            {
                return StatusCode(503, "⛔ Circuito abierto - el servicio de mensajería no está disponible.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"💥 Error inesperado: {ex.Message}");
            }
        }

        // DELETE /api/products/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {

            try
            {
                var isDeleted = await _productService.DeleteAsync(id);
                if (!isDeleted)
                    return NotFound($"Producto con Id: {id} no encontrado");

                var eventMessage = new ProductDeleted(id);

                await _resiliencePolicy.ExecuteAsync(cancellationToken =>
                    _publishEndpoint.Publish(eventMessage, context =>
                    {
                        context.SetRoutingKey("product.deleted");
                    }, cancellationToken),
                    CancellationToken.None);

                return NoContent();
            }
            catch (TimeoutRejectedException ex)
            {
                return StatusCode(504, "⏳ Tiempo de espera agotado al publicar el evento.");
            }
            catch (BrokenCircuitException ex)
            {
                return StatusCode(503, "⛔ Circuito abierto - el servicio de mensajería no está disponible.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"💥 Error inesperado: {ex.Message}");
            }

        }
    }
}
