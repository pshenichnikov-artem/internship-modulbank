using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Accounts;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AccountsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
