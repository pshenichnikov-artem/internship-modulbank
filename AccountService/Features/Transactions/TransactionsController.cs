using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Transactions;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TransactionsController : Controller
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }
}
