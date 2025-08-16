using Application.Dto;
using Application.Dto.Validation;
using Application.Extentions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UnitTests.Extensions
{
    public class ValidatorCollectionExtensionTests
    {
        [Fact]
        public void AddValidators_RegistersValidators()
        {
            var services = new ServiceCollection();

            services.AddValidators();

            var provider = services.BuildServiceProvider();

            var createUserValidator = provider.GetService<IValidator<CreateUserDto>>();
            var loginUserValidator = provider.GetService<IValidator<LoginUserDto>>();

            Assert.NotNull(createUserValidator);
            Assert.NotNull(loginUserValidator);

            Assert.IsType<CreateUserValidator>(createUserValidator);
            Assert.IsType<LoginUserValidator>(loginUserValidator);
        }
    }
}
