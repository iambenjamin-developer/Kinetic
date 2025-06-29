using Inventory.API.Controllers;
using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Polly.CircuitBreaker;
using Polly.Timeout;
using SharedKernel.Contracts;

namespace Inventory.Tests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IResilientMessagePublisher> _mockResilientMessagePublisher;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _mockProductService = new Mock<IProductService>();
            _mockResilientMessagePublisher = new Mock<IResilientMessagePublisher>();

            _controller = new ProductsController(
                _mockProductService.Object,
                _mockResilientMessagePublisher.Object
            );
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfProducts()
        {
            // Arrange
            var mockProducts = new List<ProductDto>
            {
                new ProductDto { Id = 1, Name = "Product 1", Price = 10.0m, Description = "Description 1", Stock = 100, Category = new CategoryDto { Id = 1, Name = "Electronics" } },
                new ProductDto { Id = 2, Name = "Product 2", Price = 20.0m, Description = "Description 2", Stock = 200, Category = new CategoryDto { Id = 2, Name = "Books" } }
            };

            _mockProductService.Setup(service => service.GetAllAsync()).ReturnsAsync(mockProducts);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, actionResult.StatusCode);
            var returnValue = Assert.IsType<List<ProductDto>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WhenProductExists()
        {
            // Arrange
            var mockProduct = new ProductDto 
            { 
                Id = 1, 
                Name = "Product 1", 
                Price = 10.0m, 
                Description = "Description 1", 
                Stock = 100,
                Category = new CategoryDto { Id = 1, Name = "Electronics" }
            };
            _mockProductService.Setup(service => service.GetByIdAsync(1)).ReturnsAsync(mockProduct);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, actionResult.StatusCode);
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
            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode);
            Assert.Equal("Producto con Id: 1 no encontrado", actionResult.Value);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtActionResult_WhenProductIsCreated()
        {
            // Arrange
            var createProductDto = new CreateProductDto 
            { 
                Name = "Product 1", 
                Price = 10.0m, 
                Description = "Description 1", 
                Stock = 100, 
                CategoryId = 1 
            };
            var createdProduct = new ProductDto
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.0m,
                Description = "Description 1",
                Stock = 100,
                Category = new CategoryDto { Id = 1, Name = "Electronics" }
            };

            _mockProductService.Setup(service => service.CreateAsync(createProductDto)).ReturnsAsync(createdProduct);
            _mockResilientMessagePublisher.Setup(p => p.PublishWithResilienceAsync(It.IsAny<ProductCreated>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(createProductDto);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, actionResult.StatusCode);
            var returnValue = Assert.IsType<ProductDto>(actionResult.Value);
            Assert.Equal("Product 1", returnValue.Name);
            Assert.Equal(1, returnValue.Id);

            _mockResilientMessagePublisher.Verify(p => 
                p.PublishWithResilienceAsync(It.IsAny<ProductCreated>(), "product.created", It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task Create_ReturnsTimeoutResult_WhenPublisherThrowsTimeoutRejectedException()
        {
            // Arrange
            var createProductDto = new CreateProductDto 
            { 
                Name = "Product 1", 
                Price = 10.0m, 
                Description = "Description 1", 
                Stock = 100, 
                CategoryId = 1 
            };
            var createdProduct = new ProductDto
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.0m,
                Description = "Description 1",
                Stock = 100,
                Category = new CategoryDto { Id = 1, Name = "Electronics" }
            };

            _mockProductService.Setup(service => service.CreateAsync(createProductDto)).ReturnsAsync(createdProduct);
            _mockResilientMessagePublisher.Setup(p => p.PublishWithResilienceAsync(It.IsAny<ProductCreated>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutRejectedException("Timeout"));

            // Act
            var result = await _controller.Create(createProductDto);

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status504GatewayTimeout, actionResult.StatusCode);
            Assert.Contains("Tiempo de espera agotado", actionResult.Value.ToString());
        }

        [Fact]
        public async Task Create_ReturnsServiceUnavailableResult_WhenPublisherThrowsBrokenCircuitException()
        {
            // Arrange
            var createProductDto = new CreateProductDto 
            { 
                Name = "Product 1", 
                Price = 10.0m, 
                Description = "Description 1", 
                Stock = 100, 
                CategoryId = 1 
            };
            var createdProduct = new ProductDto
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.0m,
                Description = "Description 1",
                Stock = 100,
                Category = new CategoryDto { Id = 1, Name = "Electronics" }
            };

            _mockProductService.Setup(service => service.CreateAsync(createProductDto)).ReturnsAsync(createdProduct);
            _mockResilientMessagePublisher.Setup(p => p.PublishWithResilienceAsync(It.IsAny<ProductCreated>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new BrokenCircuitException("Circuit broken"));

            // Act
            var result = await _controller.Create(createProductDto);

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, actionResult.StatusCode);
            Assert.Contains("Circuito abierto", actionResult.Value.ToString());
        }

        [Fact]
        public async Task Create_ReturnsInternalServerError_WhenPublisherThrowsGenericException()
        {
            // Arrange
            var createProductDto = new CreateProductDto 
            { 
                Name = "Product 1", 
                Price = 10.0m, 
                Description = "Description 1", 
                Stock = 100, 
                CategoryId = 1 
            };
            var createdProduct = new ProductDto
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.0m,
                Description = "Description 1",
                Stock = 100,
                Category = new CategoryDto { Id = 1, Name = "Electronics" }
            };

            _mockProductService.Setup(service => service.CreateAsync(createProductDto)).ReturnsAsync(createdProduct);
            _mockResilientMessagePublisher.Setup(p => p.PublishWithResilienceAsync(It.IsAny<ProductCreated>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.Create(createProductDto);

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, actionResult.StatusCode);
            Assert.Contains("Error inesperado", actionResult.Value.ToString());
        }

        [Fact]
        public async Task Update_ReturnsNoContentResult_WhenProductIsUpdated()
        {
            // Arrange
            var updateProductDto = new UpdateProductDto 
            { 
                Name = "Updated Product", 
                Price = 15.0m, 
                Description = "Updated Description", 
                Stock = 150 
            };
            _mockProductService.Setup(service => service.UpdateAsync(1, updateProductDto)).ReturnsAsync(true);
            _mockResilientMessagePublisher.Setup(p => p.PublishWithResilienceAsync(It.IsAny<ProductUpdated>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(1, updateProductDto);

            // Assert
            var actionResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, actionResult.StatusCode);

            _mockResilientMessagePublisher.Verify(p => 
                p.PublishWithResilienceAsync(It.IsAny<ProductUpdated>(), "product.updated", It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task Update_ReturnsNotFoundResult_WhenProductDoesNotExist()
        {
            // Arrange
            var updateProductDto = new UpdateProductDto 
            { 
                Name = "Updated Product", 
                Price = 15.0m, 
                Description = "Updated Description", 
                Stock = 150 
            };
            _mockProductService.Setup(service => service.UpdateAsync(1, updateProductDto)).ReturnsAsync(false);

            // Act
            var result = await _controller.Update(1, updateProductDto);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode);
            Assert.Equal("Producto con Id: 1 no encontrado", actionResult.Value);
        }

        [Fact]
        public async Task Update_ReturnsTimeoutResult_WhenPublisherThrowsTimeoutRejectedException()
        {
            // Arrange
            var updateProductDto = new UpdateProductDto 
            { 
                Name = "Updated Product", 
                Price = 15.0m, 
                Description = "Updated Description", 
                Stock = 150 
            };
            _mockProductService.Setup(service => service.UpdateAsync(1, updateProductDto)).ReturnsAsync(true);
            _mockResilientMessagePublisher.Setup(p => p.PublishWithResilienceAsync(It.IsAny<ProductUpdated>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutRejectedException("Timeout"));

            // Act
            var result = await _controller.Update(1, updateProductDto);

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status504GatewayTimeout, actionResult.StatusCode);
            Assert.Contains("Tiempo de espera agotado", actionResult.Value.ToString());
        }

        [Fact]
        public async Task Delete_ReturnsNoContentResult_WhenProductIsDeleted()
        {
            // Arrange
            _mockProductService.Setup(service => service.DeleteAsync(1)).ReturnsAsync(true);
            _mockResilientMessagePublisher.Setup(p => p.PublishWithResilienceAsync(It.IsAny<ProductDeleted>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var actionResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, actionResult.StatusCode);

            _mockResilientMessagePublisher.Verify(p => 
                p.PublishWithResilienceAsync(It.IsAny<ProductDeleted>(), "product.deleted", It.IsAny<CancellationToken>()), 
                Times.Once);
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
            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode);
            Assert.Equal("Producto con Id: 1 no encontrado", actionResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsTimeoutResult_WhenPublisherThrowsTimeoutRejectedException()
        {
            // Arrange
            _mockProductService.Setup(service => service.DeleteAsync(1)).ReturnsAsync(true);
            _mockResilientMessagePublisher.Setup(p => p.PublishWithResilienceAsync(It.IsAny<ProductDeleted>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutRejectedException("Timeout"));

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status504GatewayTimeout, actionResult.StatusCode);
            Assert.Contains("Tiempo de espera agotado", actionResult.Value.ToString());
        }

        [Fact]
        public async Task Delete_ReturnsServiceUnavailableResult_WhenPublisherThrowsBrokenCircuitException()
        {
            // Arrange
            _mockProductService.Setup(service => service.DeleteAsync(1)).ReturnsAsync(true);
            _mockResilientMessagePublisher.Setup(p => p.PublishWithResilienceAsync(It.IsAny<ProductDeleted>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new BrokenCircuitException("Circuit broken"));

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, actionResult.StatusCode);
            Assert.Contains("Circuito abierto", actionResult.Value.ToString());
        }
    }
}
