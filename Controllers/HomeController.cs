using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;
using System.Threading.Tasks;

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
}
