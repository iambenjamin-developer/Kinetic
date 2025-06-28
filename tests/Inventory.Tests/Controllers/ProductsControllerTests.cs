using Inventory.API.Controllers;
using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Inventory.Tests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly Mock<IResiliencePolicy> _mockResiliencePolicy;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _mockProductService = new Mock<IProductService>();
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            _mockResiliencePolicy = new Mock<IResiliencePolicy>();

            // Simula ejecución directa del método
            _mockResiliencePolicy
                .Setup(p => p.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>((action, token) => action(token));

            _controller = new ProductsController(
                _mockProductService.Object,
                _mockPublishEndpoint.Object,
                _mockResiliencePolicy.Object
            );
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfProducts()
        {
            var mockProducts = new List<ProductDto>
            {
                new ProductDto { Id = 1, Name = "Product 1", Price = 10.0m },
                new ProductDto { Id = 2, Name = "Product 2", Price = 20.0m }
            };

            _mockProductService.Setup(service => service.GetAllAsync()).ReturnsAsync(mockProducts);

            var result = await _controller.GetAll();

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, actionResult.StatusCode);
            var returnValue = Assert.IsType<List<ProductDto>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WhenProductExists()
        {
            var mockProduct = new ProductDto { Id = 1, Name = "Product 1", Price = 10.0m };
            _mockProductService.Setup(service => service.GetByIdAsync(1)).ReturnsAsync(mockProduct);

            var result = await _controller.GetById(1);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, actionResult.StatusCode);
            var returnValue = Assert.IsType<ProductDto>(actionResult.Value);
            Assert.Equal("Product 1", returnValue.Name);
        }

        [Fact]
        public async Task GetById_ReturnsNotFoundResult_WhenProductDoesNotExist()
        {
            _mockProductService.Setup(service => service.GetByIdAsync(1)).ReturnsAsync((ProductDto)null);

            var result = await _controller.GetById(1);

            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode);
            Assert.Equal("Producto con Id: 1 no encontrado", actionResult.Value);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtActionResult_WhenProductIsCreated()
        {
            var createProductDto = new CreateProductDto { Name = "Product 1", Price = 10.0m };
            var createdProduct = new ProductDto
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.0m,
                Category = new CategoryDto { Name = "Electronics" }
            };

            _mockProductService.Setup(service => service.CreateAsync(createProductDto)).ReturnsAsync(createdProduct);

            var result = await _controller.Create(createProductDto);

            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, actionResult.StatusCode);
            var returnValue = Assert.IsType<ProductDto>(actionResult.Value);
            Assert.Equal("Product 1", returnValue.Name);
            Assert.Equal(1, returnValue.Id);

            _mockResiliencePolicy.Verify(p =>
                p.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_ReturnsNoContentResult_WhenProductIsUpdated()
        {
            var updateProductDto = new UpdateProductDto { Name = "Updated Product", Price = 15.0m };
            _mockProductService.Setup(service => service.UpdateAsync(1, updateProductDto)).ReturnsAsync(true);

            var result = await _controller.Update(1, updateProductDto);

            var actionResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, actionResult.StatusCode);

            _mockResiliencePolicy.Verify(p =>
                p.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_ReturnsNotFoundResult_WhenProductDoesNotExist()
        {
            var updateProductDto = new UpdateProductDto { Name = "Updated Product", Price = 15.0m };
            _mockProductService.Setup(service => service.UpdateAsync(1, updateProductDto)).ReturnsAsync(false);

            var result = await _controller.Update(1, updateProductDto);

            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode);
            Assert.Equal("Producto con Id: 1 no encontrado", actionResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsNoContentResult_WhenProductIsDeleted()
        {
            _mockProductService.Setup(service => service.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _controller.Delete(1);

            var actionResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, actionResult.StatusCode);

            _mockResiliencePolicy.Verify(p =>
                p.ExecuteAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnsNotFoundResult_WhenProductDoesNotExist()
        {
            _mockProductService.Setup(service => service.DeleteAsync(1)).ReturnsAsync(false);

            var result = await _controller.Delete(1);

            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode);
            Assert.Equal("Producto con Id: 1 no encontrado", actionResult.Value);
        }
    }
}
