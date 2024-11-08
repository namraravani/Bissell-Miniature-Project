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
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using System.Data.SqlClient;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Publishing;

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
                                AvailabilityStatus = item["AvailabilityStatus"],
                                Price = decimal.TryParse(item["Price"], out var price) ? price : 0,
                                CaseQuantity = int.TryParse(item["CaseQuantity"], out var qty) ? qty : 0,
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

        public void InsertProductIntoSitecore(Prod product)
        {
            // Ensure you're working with the Master database
            Database masterDb = Sitecore.Configuration.Factory.GetDatabase("master");

            // Define the template ID of the Product template in Sitecore
            TemplateID templateID = new TemplateID(new ID("{76036F5E-CBCE-46D1-AF0A-4143F9B557AA}"));  // Replace with your template ID

            // Get the parent item under which products will be created (e.g., /sitecore/content/Products)
            Item parentItem = masterDb.GetItem("/sitecore/content/Home");  // Replace with your correct path

            if (parentItem != null)
            {
                using (new SecurityDisabler())  // Temporarily disable security to bypass access checks
                {
                    try
                    {
                        // Start editing the parent item
                        parentItem.Editing.BeginEdit();

                        // Create a new product item under the parent item
                        Item newProductItem = parentItem.Add(product.ProductTitle, templateID);

                        // Ensure you're editing the new item
                        newProductItem.Editing.BeginEdit();

                        // Set each field if editable
                        SetFieldIfEditable(newProductItem, "ProductTitle", product.ProductTitle);
                        SetFieldIfEditable(newProductItem, "SKU", product.SKU);
                        SetFieldIfEditable(newProductItem, "Brand", product.Brand);
                        SetFieldIfEditable(newProductItem, "Description", product.Description);
                        SetFieldIfEditable(newProductItem, "AvailabilityStatus", product.AvailabilityStatus);
                        SetFieldIfEditable(newProductItem, "Price", product.Price.ToString("F2"));
                        SetFieldIfEditable(newProductItem, "CaseQuantity", product.CaseQuantity.ToString());
                        SetFieldIfEditable(newProductItem, "ItemType", product.ItemType);

                        // Save the changes to the new product item
                        newProductItem.Editing.EndEdit();
                        parentItem.Editing.EndEdit();

                        // Now, publish the item to the web database
                        PublishItemToWeb(newProductItem);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error inserting product {product.ProductTitle} into Sitecore", ex, this);
                        // Optionally: You could cancel editing if something fails
                        parentItem.Editing.CancelEdit();
                    }
                }
            }
        }


        private void SetFieldIfEditable(Item item, string fieldName, string fieldValue)
        {
            // Check if the field exists and is editable before setting its value
            if (item.Fields[fieldName] != null && item.Fields[fieldName].CanWrite)
            {
                item.Fields[fieldName].Value = fieldValue;
            }
            else
            {
                Log.Warn($"Field {fieldName} cannot be written to, either due to lack of permission or being readonly.", this);
            }
        }

        public List<Prod> GetProductsFromExternalDatabase()
        {
            var products = new List<Prod>();

            string connectionString = "Data Source=EV-LAP-00122;Initial Catalog=Bissell_external_database;User ID=sa;Password=Welcome@123;Encrypt=False"; // Update this with your connection string

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT SKU, Brand, ProductTitle, Description, AvailabilityStatus, Price, CaseQuantity, ItemType FROM ProductsOrParts";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var product = new Prod
                        {
                            SKU = reader.GetInt32(0).ToString(),
                            Brand = reader.GetString(1),
                            ProductTitle = reader.GetString(2),
                            Description = reader.GetString(3),
                            AvailabilityStatus = reader.GetString(4),
                            Price = reader.GetDecimal(5),
                            CaseQuantity = reader.GetInt32(6),
                            ItemType = reader.GetString(7)
                        };

                        products.Add(product);
                    }
                }
            }

            return products;
        }

            private void PublishItemToWeb(Item item)
            {
                // Get the web database
                Database webDb = Sitecore.Configuration.Factory.GetDatabase("web");

                if (webDb != null)
                {
                    // Get the current language of the context (default language for the session)
                    Language currentLanguage = Sitecore.Context.Language;

                    // Get the parent item of the item being published. You can use the item's parent or ancestor.
                    // Here, we will use the parent item as the root item for publishing.
                    Item rootItem = item.Parent;

                    // Create the PublishOptions object
                    PublishOptions publishOptions = new PublishOptions(
                        item.Database,        // Source database (master)
                        webDb,                // Target database (web)
                        PublishMode.SingleItem, // Publish mode (single item)
                        currentLanguage,      // Language to publish (default current session language)
                        DateTime.Now          // Start publishing immediately
                    )
                    {
                        RootItem = rootItem // Specify the root item for publishing
                    };

                    // Create the Publisher object with the PublishOptions
                    Publisher publisher = new Publisher(publishOptions);

                    try
                    {
                    // Publish the item to the web database
                        publisher.Options.Deep = true;
                        publisher.PublishAsync();
                        item.Publishing.ClearPublishingCache();
                    Log.Info($"Successfully published item {item.ID} to the web database.", this);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error publishing item to the web database", ex, this);
                    }
                }
            }

        public void ImportProductsToSitecore()
        {
            // Step 1: Get products from external database
            List<Prod> products = GetProductsFromExternalDatabase();

            // Step 2: Insert each product into Sitecore
            foreach (var product in products)
            {
                InsertProductIntoSitecore(product);
            }
        }

        




        public ActionResult Home()
        {
            return View("~/Views/ProductListing/master.cshtml");
        }
    }
}
