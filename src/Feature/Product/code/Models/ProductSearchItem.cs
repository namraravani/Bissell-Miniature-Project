using Sitecore.ContentSearch.Attributes;
using Sitecore.ContentSearch;

namespace BisselProject.Feature.Product.Models
{
    public class ProductSearchItem
    {
        [IndexField("templateid")]
        public string TemplateId { get; set; }

        [IndexField("sku")]
        public string SKU { get; set; }

        [IndexField("brand")]
        public string Brand { get; set; }

        [IndexField("title")]
        public string Title { get; set; }

        [IndexField("description")]
        public string Description { get; set; }

        [IndexField("availabilitystatus")]
        public string AvailabilityStatus { get; set; }

        [IndexField("price")]
        public decimal Price { get; set; }

        [IndexField("casequantity")]
        public int CaseQuantity { get; set; }

        [IndexField("itemtype")]
        public string ItemType { get; set; }
    }
}
