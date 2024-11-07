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

            // Create a search context for the Solr index (replace with your actual index name)
            var index = ContentSearchManager.GetIndex("sitecore_web_index"); // Or your custom index name

            using (var context = index.CreateSearchContext())
            {
                // Query the Solr index using ProductSearchItem class
                var query = context.GetQueryable<ProductSearchItem>()
                                   .Where(item => item.Title.Contains(searchTerm) || item.Description.Contains(searchTerm)) // Search in Title and Description
                                   .OrderBy(item => item.Title) // Sort by Title
                                   .Take(10); // Limit to 10 results for now

                var results = query.ToList();

                // Map results to the products model (if needed)
                var products = results.Select(item => new Prod
                {
                    SKU = item.SKU,
                    Brand = item.Brand,
                    Title = item.Title,
                    Description = item.Description,
                    AvailabilityStatus = item.AvailabilityStatus,
                    Price = item.Price,
                    CaseQuantity = item.CaseQuantity,
                    ItemType = item.ItemType
                }).ToList();

                return View("~/Views/ProductListing/Index.cshtml", products);
            }
        }



        public ActionResult Home()
        {
            return View("~/Views/ProductListing/master.cshtml");
        }
    }
}
