using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShoesOnContainers.Services.ProductCatalogApi;
using ShoesOnContainers.Services.ProductCatalogApi.Data;
using ShoesOnContainers.Services.ProductCatalogApi.Domain;
using ShoesOnContainers.Services.ProductCatalogApi.ViewModels;

namespace ProductCatalogApi.Controllers
{
    [Produces("application/json")]
    [Route("api/Catalog")]
    public class CatalogController : Controller
    {
        private readonly CatalogContext _catalogContext;
        private readonly IOptionsSnapshot<CatalogSettings> _settings;

        public CatalogController(CatalogContext catalogContext, IOptionsSnapshot<CatalogSettings> settings)
        {
            _catalogContext = catalogContext;
            _settings = settings;
            ((DbContext)catalogContext).ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        }
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> CatalogTypes()
        {
            var items = await _catalogContext.CatalogTypes.ToListAsync();
            return Ok(items);

        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> CatalogBrands()
        {
            var items = await _catalogContext.CatalogBrands.ToListAsync();
            return Ok(items);

        }

        [HttpGet]
        [Route("items/{id:int}")]
        public async Task<IActionResult> GetItemById(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }
            var item = await _catalogContext.CatalogItems.SingleOrDefaultAsync(c => c.Id == id);
            if (item != null)
            {
                item.PictureUrl = item.PictureUrl.Replace("http://externalcatalogbaseurltobereplaced", _settings.Value.ExternalCatalogBaseUrl);
                return Ok(item);
            }
           return NotFound();
        }
        //GET api/Catalog/items[?pageSize=4&pageIndex=3]
        [HttpGet]
        // Gets name of method during run time
        [Route("[action]")]
        // Size items on a page
        public async Task<IActionResult> Items([FromQuery] int pageSize = 6, [FromQuery] int pageIndex = 0)
        {
            var totalItems = await _catalogContext.CatalogItems
                              .LongCountAsync();
            var itemsOnPage = await _catalogContext.CatalogItems                    
                              .OrderBy(c => c.Name)  // Order by name 
                              .Skip(pageSize * pageIndex)   // Skip pages before this page index
                              .Take(pageSize)
                              .ToListAsync();
            itemsOnPage = ChangeUrlPlaceHolder(itemsOnPage);
            var model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);

            return Ok(model);

        }

        //GET api/Catalog/items/withname/Wonder?pageSize=2&pageIndex=0
        [HttpGet]
        [Route("[action]/withname/{name:minlength(1)}")]
        public async Task<IActionResult> Items(string name, [FromQuery] int pageSize = 6, [FromQuery] int pageIndex = 0)
        {
            var totalItems = await _catalogContext.CatalogItems
                               .Where(c => c.Name.StartsWith(name))
                              .LongCountAsync();
            var itemsOnPage = await _catalogContext.CatalogItems
                              .Where(c => c.Name.StartsWith(name))
                              .OrderBy(c => c.Name)
                              .Skip(pageSize * pageIndex)
                              .Take(pageSize)
                              .ToListAsync();
            itemsOnPage = ChangeUrlPlaceHolder(itemsOnPage);
            var model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);

            return Ok(model);

        }

        // GET api/Catalog/Items/type/1/brand/null[?pageSize=4&pageIndex=0]
        [HttpGet]
        [Route("[action]/type/{catalogTypeId}/brand/{catalogBrandId}")]
        public async Task<IActionResult> Items(int? catalogTypeId, int? catalogBrandId, [FromQuery] int pageSize = 6, [FromQuery] int pageIndex = 0)
        {
            var root = (IQueryable<CatalogItem>)_catalogContext.CatalogItems;

            if (catalogTypeId.HasValue)
            {
                root = root.Where(c => c.CatalogTypeId == catalogTypeId);
            }
            if (catalogBrandId.HasValue)
            {
                root = root.Where(c => c.CatalogBrandId == catalogBrandId);
            }

            var totalItems = await root

                              .LongCountAsync();
            var itemsOnPage = await root

                              .OrderBy(c => c.Name)
                              .Skip(pageSize * pageIndex)
                              .Take(pageSize)
                              .ToListAsync();
            itemsOnPage = ChangeUrlPlaceHolder(itemsOnPage);
            var model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);

            return Ok(model);

        }




        private List<CatalogItem> ChangeUrlPlaceHolder(List<CatalogItem> items)
        {
            items.ForEach(
                x => x.PictureUrl = x.PictureUrl.Replace("http://externalcatalogbaseurltobereplaced",
                _settings.Value.ExternalCatalogBaseUrl));
            return items;
        }

    }
}