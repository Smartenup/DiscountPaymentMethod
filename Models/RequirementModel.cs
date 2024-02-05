using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.DiscountRules.PaymentMethod.Models
{
    public class RequirementModel
    {
		public RequirementModel()
		{
			AvailablePaymentMethodSystemNames = new List<SelectListItem>();
		}

		[NopResourceDisplayName("Plugins.DiscountRules.PaymentMethod.Fields.Method")]
        public string PaymentMethodSystemName { get; set; }

        public int DiscountId { get; set; }

        public int RequirementId { get; set; }

		public IList<SelectListItem> AvailablePaymentMethodSystemNames { get; set; }
	}
}