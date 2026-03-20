using INKO.Models;
using INKO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace INKO.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("/Admin/[controller]/{action=Index}/{id?}")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly int pageSize = 5;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            this.context = context;
            this.environment = environment;
        }
        public IActionResult Index(int pageIndex, string? search, string? column, string? orderBy)
        {
            IQueryable<Product> query = context.Products;

            //funkcionalnost za sortovanje
            string[] validColumns = { "Id", "Name", "Publisher", "Genre", "Price", "CreatedAt" };
            string[] validOrderBy = { "desc", "asc" };

            if (!validColumns.Contains(column))
            {
                column = "Id";
            }

            if (!validOrderBy.Contains(orderBy))
            {
                orderBy = "desc";
            }

            if(column == "Name")
            {
                if(orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Name);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Name);
                }
            }

            else if (column == "Publisher")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Publisher);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Publisher);
                }
            }

            else if (column == "Genre")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Genre);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Genre);
                }
            }

            else if (column == "Price")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Price);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Price);
                }
            }

            else if (column == "CreatedAt")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.CreatedAt);
                }
                else
                {
                    query = query.OrderByDescending(p => p.CreatedAt);
                }
            }

            else
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Id);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Id);
                }
            }
            //query = query.OrderByDescending(p => p.Id);






            //funkcionalnost za pretragu
            if (search != null)
            {
                query = query.Where(p => p.Name.Contains(search) || p.Publisher.Contains(search));
            }



            






            //funkcionalnost za stranice
            if(pageIndex<1)
            {
                pageIndex = 1;
            }

            decimal count = query.Count();
            int totalPages = (int)Math.Ceiling(count / pageSize);
            query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

            var products = query.ToList();

            ViewData["PageIndex"] = pageIndex;
            ViewData["TotalPages"] = totalPages;
            ViewData["Search"] = search ?? "";


            ViewData["Column"] = column;
            ViewData["OrderBy"] = orderBy;





            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(ProductDto productDto)
        {
            if (productDto.ImageFile == null)
            {
                ModelState.AddModelError("ImageFile", "The image file is required");
            }

            if (!ModelState.IsValid)
            {
                return View(productDto);
            }

            //cuvanje image fajla
            string newFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            newFileName += Path.GetExtension(productDto.ImageFile!.FileName);

            string imageFullPath = environment.WebRootPath + "/products/" + newFileName;
            using (var stream = System.IO.File.Create(imageFullPath))
            {
                productDto.ImageFile.CopyTo(stream);
            }

            //cuvanje mange u bazu podataka
            Product product = new Product()
            {
                Name = productDto.Name,
                Publisher = productDto.Publisher,
                Genre = string.Join(", ", productDto.Genres),
                Price = productDto.Price,
                Description = productDto.Description,
                ImageFileName = newFileName,
                CreatedAt = DateTime.Now,
            };

            context.Products.Add(product);
            context.SaveChanges();


            return RedirectToAction("Index", "Products");



        }

        public IActionResult Edit(int id)
        {
            var product = context.Products.Find(id);

            if (product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            //kreiranje productDto iz product
            var productDto = new ProductDto()
            {
                Name = product.Name,
                Publisher = product.Publisher,
                Genres = product.Genre.Split(',').ToList(),
                Price = product.Price,
                Description = product.Description,

            };

            ViewData["ProductId"] = product.Id;
            ViewData["ImageFileName"] = product.ImageFileName;
            ViewData["CreatedAt"] = product.CreatedAt.ToString("MM/dd/yyyy");


            return View(productDto);
        }

        [HttpPost]
        public IActionResult Edit(int id, ProductDto productDto)
        {
            var product = context.Products.Find(id);

            if(product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            if(!ModelState.IsValid)
            {

                ViewData["ProductId"] = product.Id;
                ViewData["ImageFileName"] = product.ImageFileName;
                ViewData["CreatedAt"] = product.CreatedAt.ToString("MM/dd/yyyy");
                return View(productDto);
            }

            // azuriramo sliku ukoliko je stavljena nova slika
            string newFileName = product.ImageFileName;
            if (productDto.ImageFile != null)
            {
                newFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                newFileName += Path.GetExtension(productDto.ImageFile.FileName);

                string imageFullPath = environment.WebRootPath + "/products/" + newFileName;
                using (var stream = System.IO.File.Create(imageFullPath))
                {
                    productDto.ImageFile.CopyTo(stream);
                }

                // brisanje stare slike
                string oldImageFullPath = environment.WebRootPath + "/products/" + product.ImageFileName;
                System.IO.File.Delete(oldImageFullPath);
            }


            //azuriramo mangu u bazi
                 product.Name = productDto.Name;
                 product.Publisher = productDto.Publisher;
                 product.Genre = string.Join(", ", productDto.Genres);
                 product.Price = productDto.Price;
                 product.Description = productDto.Description;
                 product.ImageFileName = newFileName;

            context.SaveChanges();

            return RedirectToAction("Index", "Products");


        }


        public IActionResult Delete(int id)
        {
            var product = context.Products.Find(id);
            if(product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            string imageFullPath = environment.WebRootPath + "/products/" + product.ImageFileName;
            System.IO.File.Delete(imageFullPath);

            context.Products.Remove(product);
            context.SaveChanges(true);


            return RedirectToAction("Index", "Products");

        }

    }
}
