using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeAlpha_EcommerceStore.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CodeAlpha_EcommerceStore.Controllers
{
    public class CartController : Controller
    {
        private readonly CodeAlphaEcommerceDbContext _context;

        public CartController(CodeAlphaEcommerceDbContext context)
        {
            _context = context;
        }

        // 1. DISPLAY CART ITEMS
        public async Task<IActionResult> Index()
        {
            // Filhal static user ID 1 use ho rahi hai
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == 1)
                .ToListAsync();

            return View(cartItems);
        }

        // 2. ADD PRODUCT TO CART (Updated with Success Message)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            int tempUserId = 1;

            // Product check karein ke mojood hai ya nahi
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == tempUserId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                _context.Update(existingItem);
            }
            else
            {
                var newItem = new CartItem
                {
                    UserId = tempUserId,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedAt = DateTime.Now
                };
                _context.Add(newItem);
            }

            await _context.SaveChangesAsync();

            // SweetAlert ke liye TempData mein message set karein
            TempData["Success"] = $"{product.ProductName} has been added to your cart!";

            return RedirectToAction("Index", "Products");
        }

        // 3. CHECKOUT PAGE (GET)
        [HttpGet]
        public IActionResult Checkout()
        {
            int userId = 1;
            var cartItems = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToList();

            if (!cartItems.Any()) return RedirectToAction("Index");

            return View(cartItems);
        }

        // 4. PLACE ORDER (POST)
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string shippingAddress = "Default Address")
        {
            int userId = 1;

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (cartItems.Any())
            {
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    TotalAmount = cartItems.Sum(x => (x.Product?.Price ?? 0) * x.Quantity),
                    Status = "Pending",
                    ShippingAddress = shippingAddress
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product?.Price ?? 0
                    };
                    _context.OrderItems.Add(orderItem);

                    // Stock kam karne ka logic (Optional but recommended)
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                    }
                }

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                TempData["OrderPlaced"] = "Your order has been placed successfully!";
                return RedirectToAction("OrderSuccess");
            }

            return RedirectToAction("Index");
        }

        // 5. SUCCESS PAGE (GET)
        [HttpGet]
        public IActionResult OrderSuccess()
        {
            return View();
        }

        // 6. REMOVE ITEM FROM CART
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Item removed from cart.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}