using FluentValidation;
using Nop.Plugin.DiscountRules.PaymentMethod.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.DiscountRules.PaymentMethod.Validators
{
    /// <summary>
    /// Represents an <see cref="RequirementModel"/> validator.
    /// </summary>
    public class RequirementModelValidator : BaseNopValidator<RequirementModel>
    {
        public RequirementModelValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.DiscountId)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.DiscountRules.PaymentMethod.Fields.DiscountId.Required"));
            RuleFor(model => model.PaymentMethodSystemName)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.DiscountRules.PaymentMethod.Fields.Method"));
        }
    }
}
