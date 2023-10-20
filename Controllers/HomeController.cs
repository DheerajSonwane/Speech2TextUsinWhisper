using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Speak2Type.Models;

namespace Speak2Type.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveAudio(IFormFile audioData)
        {
            if (audioData != null && audioData.Length > 0)
            {
                try
                {
                    var fileName = $"{Guid.NewGuid()}.wav"; // Generate a unique file name
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/AudioFiles", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await audioData.CopyToAsync(stream);
                    }

                    return Json(new { success = true, message = "Audio saved successfully!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error: " + ex.Message });
                }
            }
            else
            {
                return Json(new { success = false, message = "Please select an audio file to save." });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
