using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quetzal.UI.ViewModels;

namespace Quetzal.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operador")]
    public class DashboardController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(new DashboardEstatisticasViewModel());
        }
    }
}
