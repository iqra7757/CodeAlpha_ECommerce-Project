using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeAlpha_EcommerceStore.Models;
using System.IO;

namespace CodeAlpha_EcommerceStore.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly CodeAlphaEcommerceDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public CategoriesController(CodeAlphaEcommerceDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return Json(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null)
                {
                    category.ImageUrl = await SaveImageAsync(category.CategoryName, ImageFile);
                }
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View("Index", await _context.Categories.ToListAsync());
        }

        // FIXED: Route handle karne ke liye attribute add kiya
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Categories/Edit/{id?}")]
        public async Task<IActionResult> Edit(int? id, Category category, IFormFile? ImageFile)
        {
            // Agar URL mein ID nahi hai toh Model se lein
            int finalId = id ?? category.CategoryId;

            if (finalId != category.CategoryId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (ImageFile != null)
                    {
                        category.ImageUrl = await SaveImageAsync(category.CategoryName, ImageFile);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View("Index", await _context.Categories.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category != null)
            {
                if (category.Products != null) _context.Products.RemoveRange(category.Products);
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImageAsync(string categoryName, IFormFile file)
        {
            string folderName = categoryName.Replace(" ", "_");
            string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "images/categories", folderName);
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            string fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadDir, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return $"/images/categories/{folderName}/{fileName}";
        }

        private bool CategoryExists(int id) => _context.Categories.Any(e => e.CategoryId == id);
    }
}