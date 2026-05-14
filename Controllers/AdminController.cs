using CodeAlpha_EcommerceStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeAlpha_EcommerceStore.Controllers
{
   
    public class AdminController : Controller
    {
        private readonly CodeAlphaEcommerceDbContext _context;

        public AdminController(CodeAlphaEcommerceDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            // 1. REVENUE LOGIC: Product delete hone se revenue par farq nahi parega 
            // kyunki hum OrderItems se real sales calculate kar rahe hain.
            var revenue = await _context.OrderItems.SumAsync(oi => oi.UnitPrice * oi.Quantity);

            ViewBag.TotalRevenue = revenue;
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();

            // 2. LOW STOCK ALERT: Un products ki count jo 5 se kam hain
            ViewBag.LowStock = await _context.Products.CountAsync(p => p.StockQuantity < 5);

            // 3. GRAPH DATA: Monthly Sales ka sample data (Aap isay dynamic bhi bana sakte hain)
            // Filhal hum current revenue ko last month ke data ke tor par bhej rahe hain.
            ViewBag.ChartLabels = new string[] { "Jan", "Feb", "Mar", "Apr", "May" };
            ViewBag.ChartData = new decimal[] { 5000, 12000, 8000, 20000, revenue };

            // 4. RECENT PRODUCTS: Dashboard par table dikhane ke liye
            var recentProducts = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(recentProducts);
        }
    }
}