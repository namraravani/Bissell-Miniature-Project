using Sitecore.ContentSearch.Attributes;
using Sitecore.ContentSearch;

namespace BisselProject.Feature.Product.Models
{
    public class ProductSearchItem
    {      
        [IndexField("SKU")]
        public string SKU { get; set; }

        [IndexField("Brand")]
        public string Brand { get; set; }

        [IndexField("ProductTitle")]
        public string ProductTitle { get; set; }

        [IndexField("Description")]
        public string Description { get; set; }

        [IndexField("AvailabilityStatus")]
        public string AvailabilityStatus { get; set; }

        [IndexField("Price")]
        public decimal Price { get; set; }

        [IndexField("CaseQuantity")]
        public int CaseQuantity { get; set; }

        [IndexField("ItemType")]
        public string ItemType { get; set; }
    }
}
