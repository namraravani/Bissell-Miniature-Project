using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SolrNetExtension;
using BisselProject.Feature.Product.Models;
using Sitecore.Mvc.Presentation;
using Sitecore.Data;
using Sitecore.ContentSearch.SearchTypes;

namespace BisselProject.Feature.Product.Controllers
{
    public class ProductListingController : Controller
    {
        public ActionResult Index(string searchTerm)
        {
            // Default to an empty searchTerm if not provided
            searchTerm = searchTerm ?? string.Empty;

            // Define templateId (for products)
            var templateId = "{76036F5E-CBCE-46D1-AF0A-4143F9B557AA}";

            // Create a search context for the Solr index (replace with your actual index name)
            var index = ContentSearchManager.GetIndex("sitecore_web_index"); // Or your custom index name

            // Initialize products list
            var products = new List<Prod>();

            using (var context = index.CreateSearchContext())
            {
                // If the searchTerm is empty, fetch items by templateId
                if (string.IsNullOrEmpty(searchTerm))
                {
                    // Query by templateId (direct match for template items)
                    var itemsUsingTemplate = Sitecore.Context.Database.SelectItems($"fast:/sitecore/content//*[@@templateid='{templateId}']");

                    // If no items found, fetch items that inherit from the templateId
                    if (itemsUsingTemplate == null || !itemsUsingTemplate.Any())
                    {
                        itemsUsingTemplate = Sitecore.Context.Database.SelectItems($"fast:/sitecore/content//*[@@templateinherits='{templateId}']");
                    }

                    // Map these items to the product model
                    if (itemsUsingTemplate != null)
                    {
                        foreach (var item in itemsUsingTemplate)
                        {
                            var product = new Prod
                            {
                                SKU = item["SKU"],
                                Brand = item["Brand"],
                                ProductTitle = item["ProductTitle"],
                                Description = item["Description"],
                                AvailabilityStatus = item["Availability Status"],
                                Price = decimal.TryParse(item["Price"], out var price) ? price : 0,
                                CaseQuantity = int.TryParse(item["Case Quantity"], out var qty) ? qty : 0,
                                ItemType = item["ItemType"]
                            };

                            products.Add(product);
                        }
                    }

                    // Log how many items were found
                    Sitecore.Diagnostics.Log.Info($"Found {itemsUsingTemplate?.Count() ?? 0} items matching the template.", this);
                }
                else
                {
                    // If there's a search term, search the index for Title, Description, and Brand
                    var query = context.GetQueryable<ProductSearchItem>()
                        .Where(item => item.ProductTitle.Contains(searchTerm) || item.Description.Contains(searchTerm) || item.Brand.Contains(searchTerm))
                        .Take(1000);  // You can adjust the number of results returned here

                    // Execute the query
                    var results = query.ToList();

                    // Map the search results to the products model
                    products = results.Select(item => new Prod
                    {
                        SKU = item.SKU,
                        Brand = item.Brand,
                        ProductTitle = item.ProductTitle,
                        Description = item.Description,
                        AvailabilityStatus = item.AvailabilityStatus,
                        Price = item.Price,
                        CaseQuantity = item.CaseQuantity,
                        ItemType = item.ItemType
                    }).ToList();
                }
            }

            // Return the products to the view
            return View("~/Views/ProductListing/Index.cshtml", products);
        }

        public ActionResult Home()
        {
            return View("~/Views/ProductListing/master.cshtml");
        }
    }
}
