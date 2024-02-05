using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Discounts;
using Nop.Plugin.DiscountRules.PaymentMethod.Models;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.DiscountRules.PaymentMethod.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class DiscountRulesPaymentMethodController : BasePluginController
    {
        private readonly ISettingService _settingService;
        private readonly ICustomerService _customerService;
        private readonly IDiscountService _discountService;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly IPaymentPluginManager _paymentPluginManager;

		public DiscountRulesPaymentMethodController(IDiscountService discountService,
            ISettingService settingService,
            IPermissionService permissionService,
            ICustomerService customerService,
            ILocalizationService localizationService, 
            IPaymentPluginManager paymentPluginManager)

        {
            
            _customerService = customerService;
            _localizationService = localizationService;
            _discountService = discountService;
            _settingService = settingService;
            _permissionService = permissionService;
			_paymentPluginManager = paymentPluginManager;
		}

        
        public async Task<IActionResult> Configure(int discountId, int? discountRequirementId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

             // load the discount
            var discount = await _discountService.GetDiscountByIdAsync(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            //check whether the discount requirement exists
            if (discountRequirementId.HasValue && await _discountService.GetDiscountRequirementByIdAsync(discountRequirementId.Value) is null)
                return Content("Failed to load requirement.");

            var paymentMethodSystemName = _settingService.GetSettingByKey<string>(string.Format("DiscountRequirement.PaymentMethod-{0}", discountRequirementId.HasValue ? discountRequirementId.Value : 0));

            var model = new RequirementModel();
            model.RequirementId = discountRequirementId.HasValue ? discountRequirementId.Value : 0;
            model.DiscountId = discountId;
            model.PaymentMethodSystemName = paymentMethodSystemName;

			//stores

			model.AvailablePaymentMethodSystemNames.Insert(0, new SelectListItem
			{
				Text = await _localizationService.GetResourceAsync("Plugins.DiscountRules.PaymentMethod.Fields.SelectPaymentMethod"),
				Value = "0"
			});

			var paymentMethods = (await _paymentPluginManager.LoadActivePluginsAsync());

			foreach (var s in paymentMethods)
				model.AvailablePaymentMethodSystemNames.Add(
                    new SelectListItem() 
                    { 
                        Text = string.Format("{0} - {1}", s.PluginDescriptor.FriendlyName, s.PluginDescriptor.SystemName), 
                        Value = s.PluginDescriptor.SystemName, 
                        Selected = paymentMethodSystemName != null && s.PluginDescriptor.SystemName == paymentMethodSystemName 
                    });

            ViewData.TemplateInfo.HtmlFieldPrefix = string.Format(DiscountRequirementDefaults.HtmlFieldPrefix, discountRequirementId ?? 0);

            return View("~/Plugins/DiscountRules.PaymentMethod/Views/Configure.cshtml", model);

          
        }

        [HttpPost]
        public async Task<IActionResult> Configure(RequirementModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            if (ModelState.IsValid)
            {
                //load the discount
                var discount = await _discountService.GetDiscountByIdAsync(model.DiscountId);
                if (discount == null)
                    return NotFound(new { Errors = new[] { "Discount could not be loaded" } });

                //get the discount requirement
                var discountRequirement = await _discountService.GetDiscountRequirementByIdAsync(model.RequirementId);

                //the discount requirement does not exist, so create a new one
                if (discountRequirement == null)
                {
                    discountRequirement = new DiscountRequirement
                    {
                        DiscountId = discount.Id,
                        DiscountRequirementRuleSystemName = DiscountRequirementDefaults.SystemName
                    };

                    await _discountService.InsertDiscountRequirementAsync(discountRequirement);
                }

                //save restricted customer role identifier
                await _settingService.SetSettingAsync(string.Format(DiscountRequirementDefaults.SettingsKey, discountRequirement.Id), model.PaymentMethodSystemName);

                return Ok(new { NewRequirementId = discountRequirement.Id });
            }

            return Ok(new { Errors = GetErrorsFromModelState(ModelState) });
        }

        private IEnumerable<string> GetErrorsFromModelState(ModelStateDictionary modelState)
        {
            return modelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
        }
    }
}