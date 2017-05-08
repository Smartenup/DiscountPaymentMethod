using System;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Plugins;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;

namespace Nop.Plugin.DiscountRules.PaymentMethod
{
    public partial class PaymentMethodDiscountRequirementRule : BasePlugin, IDiscountRequirementRule
    {
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        

        public PaymentMethodDiscountRequirementRule(ISettingService settingService,
            IOrderService orderService, ILocalizationService localizationService)
        {
            this._localizationService = localizationService;
            this._settingService = settingService;
            this._orderService = orderService;
        }

        /// <summary>
        /// Check discount requirement
        /// </summary>
        /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc)</param>
        /// <returns>Result</returns>
        public DiscountRequirementValidationResult CheckRequirement(DiscountRequirementValidationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            //invalid by default
            var result = new DiscountRequirementValidationResult();

            var paymentMethodSystemName = _settingService.GetSettingByKey<string>(string.Format("DiscountRequirement.PaymentMethod-{0}", request.DiscountRequirementId));

            if (string.IsNullOrWhiteSpace(paymentMethodSystemName))
                return result;
            
            var customerSelectedPaymentMethodSystemName = request.Customer.GetAttribute<string>(SystemCustomerAttributeNames.SelectedPaymentMethod, request.Store.Id);

            if (string.IsNullOrWhiteSpace(customerSelectedPaymentMethodSystemName))
                return result;

            result.UserError = _localizationService.GetResource("Plugins.DiscountRules.HasSpentAmount.NotEnough");

            if (customerSelectedPaymentMethodSystemName == paymentMethodSystemName)
                result.IsValid = true;
            else
                result.UserError = _localizationService.GetResource("Plugins.DiscountRules.HasSpentAmount.NotEnough");

            return result;
        }

        /// <summary>
        /// Get URL for rule configuration
        /// </summary>
        /// <param name="discountId">Discount identifier</param>
        /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
        /// <returns>URL</returns>
        public string GetConfigurationUrl(int discountId, int? discountRequirementId)
        {
            //configured in RouteProvider.cs
            string result = "Plugins/DiscountRulesPaymentMethod/Configure/?discountId=" + discountId;
            if (discountRequirementId.HasValue)
                result += string.Format("&discountRequirementId={0}", discountRequirementId.Value);
            return result;
        }

        public override void Install()
        {
            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.PaymentMethod.Fields.SelectPaymentMethod", "Select Payment Method");
            this.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.PaymentMethod.Fields.Method", "Payment Method to be discounted");
            this.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.PaymentMethod.Fields.Method.Hint", "Discount will be applied if customer selected this payment method.");
            this.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.PaymentMethod.NotEnough", "Sorry, this offer requires that you use the exclusive Payment Method");
            base.Install();
        }

        public override void Uninstall()
        {
            //locales
            this.DeletePluginLocaleResource("Plugins.DiscountRules.PaymentMethod.Fields.SelectPaymentMethod");
            this.DeletePluginLocaleResource("Plugins.DiscountRules.PaymentMethod.Fields.Method");
            this.DeletePluginLocaleResource("Plugins.DiscountRules.PaymentMethod.Fields.Method.Hint");
            this.DeletePluginLocaleResource("Plugins.DiscountRules.PaymentMethod.NotEnough");
            base.Uninstall();
        }
    }
}