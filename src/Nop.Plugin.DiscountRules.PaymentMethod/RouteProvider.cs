using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.DiscountRules.PaymentMethod
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.DiscountRules.PaymentMethod.Configure",
                 "Plugins/DiscountRulesPaymentMethod/Configure",
                 new { controller = "DiscountRulesPaymentMethod", action = "Configure" },
                 new[] { "Nop.Plugin.DiscountRules.PaymentMethod.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
