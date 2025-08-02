﻿using Application.Dto;
using Application.Dto.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extentions
{
    public static class ValidatorCollectionExtension
    {
        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            return services
                .AddScoped<IValidator<CreateUserDto>, CreateUserValidator>()
                .AddScoped<IValidator<LoginUserDto>, LoginUserValidator>();
        }
    }
}