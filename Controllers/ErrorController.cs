using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers;

[Route("Error")]
public class ErrorController : Controller
{
    [Route("404")]
    public IActionResult NotFound404() => View("Error404");

    [Route("500")]
    public IActionResult ServerError500()
    {
        ViewBag.ErrorId = HttpContext.TraceIdentifier;
        return View("Error500");
    }

    [Route("{code:int}")]
    public IActionResult ShowError(int code)
    {
        if (code == 404) return View("Error404");
        if (code >= 500) return View("Error500");
        return View("Error404");
    }
}