﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Customers;
using Nop.Services.Tax;
using Nop.Services.Common;

namespace Nop.Plugin.DiscountRules.PaymentMethod


{
    public partial class PaymentMethodDiscountRequirementRule : BasePlugin, IDiscountRequirementRule
    {
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductService _productServise;
        protected readonly IDiscountService _discountService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IWebHelper _webHelper;
        private readonly IActionContextAccessor _actionContextAccessor;
		private readonly IGenericAttributeService _genericAttributeService;

		public PaymentMethodDiscountRequirementRule(ISettingService settingService,
            IOrderService orderService, IPriceCalculationService priceCalculationService,
            ILocalizationService localizationService, IShoppingCartService shoppingCartService, IProductService productServise,
            IDiscountService discountService, IUrlHelperFactory urlHelperFactory,
            IWebHelper webHelper, IActionContextAccessor actionContextAccessor, IGenericAttributeService genericAttributeService)
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _orderService = orderService;
            _priceCalculationService = priceCalculationService;
            _shoppingCartService = shoppingCartService;
            _productServise = productServise;
            _discountService = discountService;
            _actionContextAccessor = actionContextAccessor;
            _urlHelperFactory = urlHelperFactory;
            _webHelper = webHelper;
			_genericAttributeService = genericAttributeService;

		}

        /// <summary>
        /// Check discount requirement
        /// </summary>
        /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc)</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public async Task<DiscountRequirementValidationResult> CheckRequirementAsync(DiscountRequirementValidationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            //invalid by default
            var result = new DiscountRequirementValidationResult();

			var paymentMethodSystemName = _settingService.GetSettingByKey<string>(string.Format("DiscountRequirement.PaymentMethod-{0}", request.DiscountRequirementId));

			if (string.IsNullOrWhiteSpace(paymentMethodSystemName))
				return result;

			var customerSelectedPaymentMethodSystemName = await _genericAttributeService
            .GetAttributeAsync<string>(request.Customer, NopCustomerDefaults.SelectedPaymentMethodAttribute, request.Store.Id);

			if (string.IsNullOrWhiteSpace(customerSelectedPaymentMethodSystemName))
				return result;

			result.UserError = await _localizationService.GetResourceAsync("Plugins.DiscountRules.HasSpentAmount.NotEnough");

			if (customerSelectedPaymentMethodSystemName == paymentMethodSystemName)
				result.IsValid = true;
			else
				result.UserError = await _localizationService.GetResourceAsync("Plugins.DiscountRules.HasSpentAmount.NotEnough");

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
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            return urlHelper.Action("Configure", "DiscountRulesPaymentMethod",
                new { discountId = discountId, discountRequirementId = discountRequirementId }, _webHelper.GetCurrentRequestProtocol());

            ////configured in RouteProvider.cs
            //string result = "Plugins/DiscountRulesHasSpentAmount/Configure/?discountId=" + discountId;
            //if (discountRequirementId.HasValue)
            //    result += string.Format("&discountRequirementId={0}", discountRequirementId.Value);
            //return result;
        }

            public override async Task InstallAsync()
        {
            
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
				//["Plugins.DiscountRules.HasSpentAmount.Fields.Amount"] = "Required spent amount",
				//["Plugins.DiscountRules.HasSpentAmount.Fields.Amount.Hint"] = "Discount will be applied if customer has spent/purchased x.xx amount.",
				//["Plugins.DiscountRules.HasSpentAmount.NotEnough"] = "Sorry, this offer requires more money spent"

				["Plugins.DiscountRules.PaymentMethod.Fields.SelectPaymentMethod"] = "Select Payment Method",
				["Plugins.DiscountRules.PaymentMethod.Fields.Method"] = "Payment Method to be discounted",
				["Plugins.DiscountRules.PaymentMethod.Fields.Method.Hint"] = "Discount will be applied if customer selected this payment method.",
				["Plugins.DiscountRules.PaymentMethod.NotEnough"] = "Sorry, this offer requires that you use the exclusive Payment Method"

			});

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //discount requirements
            var discountRequirements = (await _discountService.GetAllDiscountRequirementsAsync())
                .Where(discountRequirement => discountRequirement.DiscountRequirementRuleSystemName == DiscountRequirementDefaults.SystemName);
            foreach (var discountRequirement in discountRequirements)
            {
                await _discountService.DeleteDiscountRequirementAsync(discountRequirement, false);
            }

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.DiscountRules.PaymentMethod");

            await base.UninstallAsync();
        }


    }
}