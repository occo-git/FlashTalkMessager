using FluentValidation;

namespace Application.Extentions
{
    public static class ModelValidator
    {
        public static async Task<T> ValidationCheck<T>(this IValidator<T> validator, T item)
        {
            var validationResult = await validator.ValidateAsync(item);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                throw new ValidationException(string.Join("; ", errors));
            }
            return item;
        }
    }
}
