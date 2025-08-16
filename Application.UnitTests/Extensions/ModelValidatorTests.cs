using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Xunit;
using global::Application.Extentions;

namespace Application.UnitTests.Extensions
{
    public class ModelValidatorTests
    {
        public class TestModel
        {
            public string? Property { get; set; }
        }

        [Fact]
        public async Task ValidationCheck_ReturnsItem_WhenValid()
        {
            // Arrange
            var mockValidator = new Mock<IValidator<TestModel>>();
            mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TestModel>(), default))
                         .ReturnsAsync(new ValidationResult()); // IsValid == true

            var model = new TestModel { Property = "value" };

            // Act
            var result = await mockValidator.Object.ValidationCheck(model);

            // Assert
            Assert.Equal(model, result);
        }

        [Fact]
        public async Task ValidationCheck_ThrowsValidationException_WhenInvalid()
        {
            // Arrange
            var failures = new List<ValidationFailure>
                {
                    new ValidationFailure("Property", "Error 1"),
                    new ValidationFailure("Property", "Error 2")
                };
            var validationResult = new ValidationResult(failures);

            var mockValidator = new Mock<IValidator<TestModel>>();
            mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TestModel>(), default))
                         .ReturnsAsync(validationResult); // IsValid == false

            var model = new TestModel { Property = "value" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => mockValidator.Object.ValidationCheck(model));
            Assert.Contains("Error 1", ex.Message);
            Assert.Contains("Error 2", ex.Message);
        }
    }
}