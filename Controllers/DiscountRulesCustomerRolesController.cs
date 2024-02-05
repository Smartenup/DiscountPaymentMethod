using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Stores;
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
        private int restrictedRoleId;
		private readonly IPaymentService _paymentService;
        private readonly IPaymentPluginManager _paymentPluginManager;

		public DiscountRulesPaymentMethodController(IDiscountService discountService,
            ISettingService settingService,
            IPermissionService permissionService,
            ICustomerService customerService,
            ILocalizationService localizationService, IPaymentService paymentService, IPaymentPluginManager paymentPluginManager)

        {
            
            this._customerService = customerService;
            this._localizationService = localizationService;
            this._discountService = discountService;
            this._settingService = settingService;
            this._permissionService = permissionService;
			this._paymentService = paymentService;
            this._paymentPluginManager = paymentPluginManager;
		}

        
        #region Utilities

        /// <summary>
        /// Get errors message from model state
        /// </summary>
        /// <param name="modelState">Model state</param>
        /// <returns>Errors message</returns>
        /// 

        //comentado 28/11 V
        //protected IEnumerable<string> GetErrorsFromModelState(ModelStateDictionary modelState)
        //{
        //    return ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
        //}

        #endregion
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

            //set available customer roles
            //    //novo V
            //    model.AvailableCustomerRoles = (await _customerService.GetAllCustomerRolesAsync(true)).Select(role => new SelectListItem
            //    {
            //        Text = role.Name,
            //        Value = role.Id.ToString(),
            //        Selected = role.Id == restrictedRoleId
            //    }).ToList();
            //    model.AvailableCustomerRoles.Insert(0, new SelectListItem
            //    {
            //        Text = await _localizationService.GetResourceAsync("Plugins.DiscountRules.CustomerRoles.Fields.CustomerRole.Select"),
            //        Value = "0"
            //    });

            //    //add a prefix
            //    ViewData.TemplateInfo.HtmlFieldPrefix = string.Format("DiscountRulesHasSpentAmount{0}", discountRequirementId.HasValue ? discountRequirementId.Value.ToString() : "0");

            //    return View("~/Plugins/DiscountRules.HasSpentAmount/Views/Configure.cshtml", model);
        }


        // comentado 30/11
            //[HttpPost]
            //public async Task<IActionResult> Configure(int discountId, int? discountRequirementId, decimal spentAmount)
            //{
            //    if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageDiscounts))
            //        return Content("Access denied");

            //    var discount = _discountService.GetDiscountByIdAsync(discountId);
            //    if (discount == null)
            //        throw new ArgumentException("Discount could not be loaded");

            //    DiscountRequirement discountRequirement = null;
            //    if (discountRequirementId.HasValue)
            //        discountRequirement = discount.DiscountRequirements.FirstOrDefault(dr => dr.Id == discountRequirementId.Value);

            //    if (discountRequirement != null)
            //    {
            //        //update existing rule
            //        _settingService.SetSetting(string.Format("DiscountRequirement.HasSpentAmount-{0}", discountRequirement.Id), spentAmount);
            //    }
            //    else
            //    {
            //        //save new rule
            //        discountRequirement = new DiscountRequirement
            //        {
            //            DiscountRequirementRuleSystemName = "DiscountRequirement.HasSpentAmount"
            //        };
            //        discount.DiscountRequirements.Add(discountRequirement);
            //        _discountService.UpdateDiscountAsync(discount);

            //        _settingService.SetSetting(string.Format("DiscountRequirement.HasSpentAmount-{0}", discountRequirement.Id), spentAmount);
            //    }
            //    return Json(new { Result = true, NewRequirementId = discountRequirement.Id }, JsonRequestBehavior.AllowGet);
            //}

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