using CodeAlpha_EcommerceStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeAlpha_EcommerceStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly CodeAlphaEcommerceDbContext _context;

        public AccountController(CodeAlphaEcommerceDbContext context)
        {
            _context = context;
        }

        // ✅ GET: /Account/Login (Default Page)
        [HttpGet]
        public IActionResult Login()
        {
            // Agar already logged in hai toh Home pe bhejo
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // ✅ POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ViewBag.Error = "Invalid email or password";
                return View();
            }

            // Session create
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.FullName?.Split(' ')[0] ?? "User");
            HttpContext.Session.SetString("UserEmail", user.Email);

            return RedirectToAction("Index", "Home");
        }

        // ✅ GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // ✅ POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(User user, string password, string confirmPassword)
        {
            // Validation
            if (password != confirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
                return View(user);
            }

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered");
                return View(user);
            }

            // Save user
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.CreatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Account created! Please login.";
            return RedirectToAction("Login");
        }

        // ✅ Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Logged out successfully!";
            return RedirectToAction("Login");
        }
    }
}