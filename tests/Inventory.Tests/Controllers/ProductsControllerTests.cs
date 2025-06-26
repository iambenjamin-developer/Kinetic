using Inventory.API.Controllers;
using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Inventory.Tests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> _mockProductService;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _mockProductService = new Mock<IProductService>();
            _controller = new ProductsController(
                _mockProductService.Object
            );
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfProducts()
        {
            // Arrange
            var mockProducts = new List<ProductDto>
            {
                new ProductDto { Id = 1, Name = "Product 1", Price = 10.0m },
                new ProductDto { Id = 2, Name = "Product 2", Price = 20.0m }
            };

            _mockProductService.Setup(service => service.GetAllAsync()).ReturnsAsync(mockProducts);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, actionResult.StatusCode); // Evaluar con enumerador
            var returnValue = Assert.IsType<List<ProductDto>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WhenProductExists()
        {
            // Arrange
            var mockProduct = new ProductDto { Id = 1, Name = "Product 1", Price = 10.0m };
            _mockProductService.Setup(service => service.GetByIdAsync(1)).ReturnsAsync(mockProduct);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, actionResult.StatusCode); // Evaluar con enumerador
            var returnValue = Assert.IsType<ProductDto>(actionResult.Value);
            Assert.Equal("Product 1", returnValue.Name);
        }

        [Fact]
        public async Task GetById_ReturnsNotFoundResult_WhenProductDoesNotExist()
        {
            // Arrange
            _mockProductService.Setup(service => service.GetByIdAsync(1)).ReturnsAsync((ProductDto)null);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode); // Evaluar con enumerador
            Assert.Equal("Producto con Id: 1 no encontrado", actionResult.Value);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtActionResult_WhenProductIsCreated()
        {
            // Arrange
            var createProductDto = new CreateProductDto { Name = "Product 1", Price = 10.0m };
            var createdProduct = new ProductDto { Id = 1, Name = "Product 1", Price = 10.0m };

            _mockProductService.Setup(service => service.CreateAsync(createProductDto)).ReturnsAsync(createdProduct);

            // Act
            var result = await _controller.Create(createProductDto);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, actionResult.StatusCode); // Evaluar con enumerador
            var returnValue = Assert.IsType<ProductDto>(actionResult.Value);
            Assert.Equal("Product 1", returnValue.Name);
            Assert.Equal(1, returnValue.Id);
        }

        [Fact]
        public async Task Update_ReturnsNoContentResult_WhenProductIsUpdated()
        {
            // Arrange
            var updateProductDto = new UpdateProductDto { Name = "Updated Product", Price = 15.0m };
            _mockProductService.Setup(service => service.UpdateAsync(1, updateProductDto)).ReturnsAsync(true);

            // Act
            var result = await _controller.Update(1, updateProductDto);

            // Assert
            var actionResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, actionResult.StatusCode); // Evaluar con enumerador
        }

        [Fact]
        public async Task Update_ReturnsNotFoundResult_WhenProductDoesNotExist()
        {
            // Arrange
            var updateProductDto = new UpdateProductDto { Name = "Updated Product", Price = 15.0m };
            _mockProductService.Setup(service => service.UpdateAsync(1, updateProductDto)).ReturnsAsync(false);

            // Act
            var result = await _controller.Update(1, updateProductDto);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode); // Evaluar con enumerador
            Assert.Equal("Producto con Id: 1 no encontrado", actionResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsNoContentResult_WhenProductIsDeleted()
        {
            // Arrange
            _mockProductService.Setup(service => service.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var actionResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, actionResult.StatusCode); // Evaluar con enumerador
        }

        [Fact]
        public async Task Delete_ReturnsNotFoundResult_WhenProductDoesNotExist()
        {
            // Arrange
            _mockProductService.Setup(service => service.DeleteAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode); // Evaluar con enumerador
            Assert.Equal("Producto con Id: 1 no encontrado", actionResult.Value);
        }
    }
}
