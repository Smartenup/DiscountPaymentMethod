using Nop.Core.Domain.Discounts;
using Nop.Plugin.DiscountRules.PaymentMethod.Models;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework.Controllers;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Nop.Plugin.DiscountRules.PaymentMethod.Controllers
{
    [AdminAuthorize]
    public class DiscountRulesPaymentMethodController : Controller
    {
        private readonly ILocalizationService _localizationService;
        private readonly IDiscountService _discountService;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;
        private readonly IPaymentService _paymentService;

        public DiscountRulesPaymentMethodController(IDiscountService discountService,
            ISettingService settingService, IPermissionService permissionService,
            ILocalizationService localizationService, IPaymentService paymentService)
        {
            this._discountService = discountService;
            this._settingService = settingService;
            this._permissionService = permissionService;
            this._localizationService = localizationService;
            this._paymentService = paymentService;
        }

        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            //little hack here
            //always set culture to 'en-US' (Telerik has a bug related to editing decimal values in other cultures). Like currently it's done for admin area in Global.asax.cs
            //var culture = new CultureInfo("en-US");
            //Thread.CurrentThread.CurrentCulture = culture;
            //Thread.CurrentThread.CurrentUICulture = culture;

            base.Initialize(requestContext);
        }

        public ActionResult Configure(int discountId, int? discountRequirementId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            var discount = _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            DiscountRequirement discountRequirement = null;
            if (discountRequirementId.HasValue)
            {
                discountRequirement = discount.DiscountRequirements.FirstOrDefault(dr => dr.Id == discountRequirementId.Value);
                if (discountRequirement == null)
                    return Content("Failed to load requirement.");
            }

            var paymentMethodSystemName = _settingService.GetSettingByKey<string>(string.Format("DiscountRequirement.PaymentMethod-{0}", discountRequirementId.HasValue ? discountRequirementId.Value : 0));

            var model = new RequirementModel();
            model.RequirementId = discountRequirementId.HasValue ? discountRequirementId.Value : 0;
            model.DiscountId = discountId;
            model.PaymentMethodSystemName = paymentMethodSystemName;

            //stores
            model.AvailablePaymentMethodSystemNames.Add(new SelectListItem() { Text = _localizationService.GetResource("Plugins.DiscountRules.PaymentMethod.Fields.SelectPaymentMethod"), Value = "0" });
            foreach (var s in _paymentService.LoadAllPaymentMethods())
                model.AvailablePaymentMethodSystemNames.Add(new SelectListItem() { Text = string.Format("{0} - {1}" , s.PluginDescriptor.FriendlyName,s.PluginDescriptor.SystemName) , Value = s.PluginDescriptor.SystemName, Selected = discountRequirement != null && s.PluginDescriptor.SystemName == paymentMethodSystemName });

            //add a prefix
            ViewData.TemplateInfo.HtmlFieldPrefix = string.Format("DiscountRulesPaymentMethod{0}", discountRequirementId.HasValue ? discountRequirementId.Value.ToString() : "0");

            //return View("Nop.Plugin.DiscountRules.PaymentMethod.Views.DiscountRulesPaymentMethod.Configure", model);
            return View("~/Plugins/DiscountRules.PaymentMethod/Views/DiscountRulesPaymentMethod/Configure.cshtml", model);
            
        }

        [HttpPost]
        public ActionResult Configure(int discountId, int? discountRequirementId, string paymentMethodSystemName)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            var discount = _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            DiscountRequirement discountRequirement = null;
            if (discountRequirementId.HasValue)
                discountRequirement = discount.DiscountRequirements.FirstOrDefault(dr => dr.Id == discountRequirementId.Value);

            if (discountRequirement != null)
            {
                //update existing rule
                _settingService.SetSetting(string.Format("DiscountRequirement.PaymentMethod-{0}", discountRequirement.Id), paymentMethodSystemName);
            }
            else
            {
                //save new rule
                discountRequirement = new DiscountRequirement()
                {
                    DiscountRequirementRuleSystemName = "DiscountRequirement.PaymentMethod"
                };
                discount.DiscountRequirements.Add(discountRequirement);
                _discountService.UpdateDiscount(discount);

                _settingService.SetSetting(string.Format("DiscountRequirement.PaymentMethod-{0}", discountRequirement.Id), paymentMethodSystemName);
            }
            return Json(new { Result = true, NewRequirementId = discountRequirement.Id }, JsonRequestBehavior.AllowGet);
        }
        
    }
}