using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CodeAlpha_EcommerceStore.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CodeAlpha_EcommerceStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly CodeAlphaEcommerceDbContext _context;

        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductsController(CodeAlphaEcommerceDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // 1. SHOP/INDEX PAGE (With Search & Pagination)
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            // Search filter ko view ke liye save karein
            ViewData["CurrentFilter"] = searchString;

            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

            // Search Logic
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p => p.ProductName.Contains(searchString)
                                                     || p.Category.CategoryName.Contains(searchString));
            }

            // Hamesha latest products pehle dikhayein
            productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);

            // Pagination Settings
            int pageSize = 12; // Ek page par 12 images
            int pageIndex = pageNumber ?? 1;

            var totalItems = await productsQuery.CountAsync();
            var items = await productsQuery
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Pagination data for View
            ViewBag.CurrentPage = pageIndex;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.HasPreviousPage = pageIndex > 1;
            ViewBag.HasNextPage = pageIndex < ViewBag.TotalPages;

            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName");

            return View(items);
        }

        // 2. AJAX Method: Get Product for Quick View
        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Select(p => new {
                    p.ProductId,
                    p.ProductName,
                    p.Price,
                    p.StockQuantity,
                    p.ImageUrl,
                    Category = new { p.Category.CategoryName }
                })
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            return Json(product);
        }

        // 3. CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    product.ImageUrl = await SaveImageAsync(ImageFile);
                }

                product.CreatedAt = DateTime.Now;
                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product added successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            return RedirectToAction(nameof(Index));
        }

        // 4. EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int ProductId, Product product, IFormFile? ImageFile)
        {
            if (ProductId != product.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        product.ImageUrl = await SaveImageAsync(ImageFile);
                    }
                    else
                    {
                        _context.Entry(product).Property(x => x.ImageUrl).IsModified = false;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Product updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        // 5. DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                var relatedOrderItems = _context.OrderItems.Where(oi => oi.ProductId == id);
                if (relatedOrderItems.Any())
                {
                    _context.OrderItems.RemoveRange(relatedOrderItems);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper Method: Image Save Logic
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            string folder = "images/products/";
            string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string serverPath = Path.Combine(_hostEnvironment.WebRootPath, folder, fileName);

            string? directory = Path.GetDirectoryName(serverPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(serverPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/" + folder + fileName;
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}