using Microsoft.AspNetCore.Mvc;
using INKO.Services;
using INKO.Models;


namespace INKO.Controllers
{
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly int pageSize = 8;

        public StoreController(ApplicationDbContext context)
        {
            this.context = context;
        }
        public IActionResult Index(int pageIndex, string? search, string? genre, string? publisher, string? sort)
        {
            IQueryable<Product> query = context.Products;

            // funkcionalnost za pretragu
            if(search != null && search.Length>0)
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            // funkcionalnost za filter
            if(genre != null && genre.Length > 0)
            {
                query = query.Where(p => p.Genre.Contains(genre));
            }
            if (publisher != null && publisher.Length > 0)
            {
                query = query.Where(p => p.Publisher.Contains(publisher));
            }

            // funkcionalnost za sort po ceni
            if(sort =="price_asc")
            {
                query = query.OrderBy(p => p.Price);

            }
            else if (sort == "price_desc")
            {
                query = query.OrderByDescending(p => p.Price);
            }

            else
            {
                //sort za najnovije mange
                query = query.OrderByDescending(p => p.Id);
            }

                

            // funkcionalnost za stranice

            if(pageIndex < 1)
            {
                pageIndex = 1;
            }

            decimal count = query.Count();
            int totalPages = (int)Math.Ceiling(count / pageSize);
            query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

            var products = query.ToList();

            ViewBag.Products = products;
            ViewBag.PageIndex = pageIndex;
            ViewBag.TotalPages = totalPages;

            var storeSearchModel = new StoreSearchModel()
            {
                Search = search,
                Genre = genre,
                Publisher = publisher,
                Sort = sort
            };

            return View(storeSearchModel);
        }

        public IActionResult Details(int id)
        {
            var product = context.Products.Find(id);
            if(product == null)
            {
                return RedirectToAction("Index", "Store");
            }
            return View(product);
        }




    }
}
