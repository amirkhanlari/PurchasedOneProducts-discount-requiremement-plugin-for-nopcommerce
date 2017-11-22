using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Nop.Plugin.DiscountRules.PurchasedOneProduct
{
    public partial class PurchasedOneProductDiscountRequirementRule : BasePlugin, IDiscountRequirementRule
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IUrlHelperFactory _urlHelperFactory;

        #endregion

        #region Ctor

        public PurchasedOneProductDiscountRequirementRule(ISettingService settingService,
            IRepository<OrderItem> orderItemRepository,
            IActionContextAccessor actionContextAccessor,
            IUrlHelperFactory urlHelperFactory)
        {
            this._settingService = settingService;
            this._orderItemRepository = orderItemRepository;
            this._actionContextAccessor = actionContextAccessor;
            this._urlHelperFactory = urlHelperFactory;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Check discount requirement
        /// </summary>
        /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc)</param>
        /// <returns>Result</returns>
        public DiscountRequirementValidationResult CheckRequirement(DiscountRequirementValidationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            //invalid by default
            var result = new DiscountRequirementValidationResult();

            var restrictedProductVariantIdsStr = _settingService.GetSettingByKey<string>($"DiscountRequirement.RestrictedProductVariantIds-{request.DiscountRequirementId}");

            if (string.IsNullOrWhiteSpace(restrictedProductVariantIdsStr))
                return result;

            if (request.Customer == null)
                return result;

            List<int> restrictedProductIds;

            try
            {
                restrictedProductIds = restrictedProductVariantIdsStr
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Convert.ToInt32(x))
                    .ToList();
            }
            catch
            {
                //error parsing
                return result;
            }

            if (restrictedProductIds.Count == 0)
                return result;

            var customerId = request.Customer.Id;
            const int orderStatusId = (int)OrderStatus.Complete;
            //purchased product
            var purchasedProducts = _orderItemRepository.Table.Where(oi => oi.Order.CustomerId == customerId && !oi.Order.Deleted && oi.Order.OrderStatusId == orderStatusId).ToList();

            var found = false;

            foreach (var restrictedProductId in restrictedProductIds)
            {
                if (purchasedProducts.Any(purchasedProduct => restrictedProductId == purchasedProduct.ProductId))
                {
                    found = true;
                }

                if (found)
                {
                    break;
                }
            }

            if (found)
            {
                result.IsValid = true;
                return result;
            }

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
            return urlHelper.Action("Configure", "DiscountRulesPurchasedOneProduct",
                new { discountId = discountId, discountRequirementId = discountRequirementId }).TrimStart('/');
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.PurchasedOneProduct.Fields.Products", "Restricted products");
            this.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.PurchasedOneProduct.Fields.Products.Hint", "The comma-separated list of product identifiers (e.g. 77, 123, 156). You can find a product variant ID on its details page.");
            this.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.PurchasedOneProduct.Fields.Products.AddNew", "Add product");
            this.AddOrUpdatePluginLocaleResource("Plugins.DiscountRules.PurchasedOneProduct.Fields.Products.Choose", "Choose");

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //locales
            this.DeletePluginLocaleResource("Plugins.DiscountRules.PurchasedOneProduct.Fields.Products");
            this.DeletePluginLocaleResource("Plugins.DiscountRules.PurchasedOneProduct.Fields.Products.Hint");
            this.DeletePluginLocaleResource("Plugins.DiscountRules.PurchasedOneProduct.Fields.Products.AddNew");
            this.DeletePluginLocaleResource("Plugins.DiscountRules.PurchasedOneProduct.Fields.Products.Choose");

            base.Uninstall();
        }

        #endregion
    }
}