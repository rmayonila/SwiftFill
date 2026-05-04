using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;
using System.Threading.Tasks;
using SwiftFill.Helpers;

namespace SwiftFill.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Demo()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Track(string? trackingId)
    {
        if (string.IsNullOrWhiteSpace(trackingId))
        {
            return View(null);
        }

        var order = await _context.Orders
            .Include(o => o.AssignedRider)
            .FirstOrDefaultAsync(o => o.TrackingId == trackingId.Trim());

        if (order == null)
        {
            ViewBag.ErrorMessage = "We couldn't find a package with that tracking ID. Please check and try again.";
            return View(null);
        }

        return View(order);
    }

    [HttpPost]
    public IActionResult Contact(string name, string email, string subject, string message)
    {
        // Sanitization: Clean all inputs before processing
        name = InputSanitizer.Sanitize(name) ?? "Anonymous";
        email = InputSanitizer.Sanitize(email) ?? "No Email";
        subject = InputSanitizer.Sanitize(subject) ?? "No Subject";
        message = InputSanitizer.Sanitize(message) ?? "No Message";

        // In a real app, you would send an email or save to DB here.
        TempData["SuccessMessage"] = $"Thank you, {name}! Your message regarding \"{subject}\" has been sent. We will get back to you at {email} soon.";
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        if (statusCode == 404)
        {
            return View("NotFound");
        }
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
