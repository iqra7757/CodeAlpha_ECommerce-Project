using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeAlpha_EcommerceStore.Models;

namespace CodeAlpha_EcommerceStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly CodeAlphaEcommerceDbContext _context;

        public OrderController(CodeAlphaEcommerceDbContext context)
        {
            _context = context;
        }

        // 1. Checkout Page (Show Form)
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            int currentUserId = 1;
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == currentUserId)
                .ToListAsync();

            if (cartItems == null || !cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            return View(cartItems);
        }

        // 2. Place Order (Logic to save TotalAmount and OrderItems)
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(Order model)
        {
            int currentUserId = 1; // Static UserId

            // Cart se items fetch karein Product table ke saath taake price mil sakay
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == currentUserId)
                .ToListAsync();

            if (cartItems == null || !cartItems.Any())
            {
                ModelState.AddModelError("", "Cart is empty.");
                return RedirectToAction("Index", "Cart");
            }

            // Total Amount Calculation
            decimal calculatedTotal = 0;
            foreach (var item in cartItems)
            {
                if (item.Product != null)
                {
                    calculatedTotal += item.Product.Price * item.Quantity;
                }
            }

            // Order Model ki properties set karein (Aapke schema ke mutabiq)
            model.UserId = currentUserId;
            model.OrderDate = DateTime.Now;
            model.Status = "Pending";
            model.TotalAmount = calculatedTotal; // Dashboard par 0 nahi aayega

            _context.Orders.Add(model);
            await _context.SaveChangesAsync(); // OrderId auto-generate hogi

            // OrderItems table mein entry transfer karein
            foreach (var item in cartItems)
            {
                if (item.Product != null)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = model.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.Price // OrderItem model ka column UnitPrice hai
                    };
                    _context.OrderItems.Add(orderItem);
                }
            }

            await _context.SaveChangesAsync();

            // Cart khali kar dein
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // Success Page par bhejain jahan Track Details dekh saken
            return RedirectToAction("OrderSuccess", new { id = model.OrderId });
        }

        // 3. Order Success Page
        public IActionResult OrderSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        // 4. User Order History (Tracking Page)
        public async Task<IActionResult> Index()
        {
            int userId = 1;
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // 5. Detailed Tracking for Single Order
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // 6. Admin Panel Index
        public async Task<IActionResult> AdminIndex()
        {
            var allOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(allOrders);
        }

        // 7. Admin Status Update
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}