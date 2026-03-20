using System.Web.Mvc;
using MatKinh.Models;

namespace MatKinh.Controllers
{
    [CustomAuthentication]
    public class ErrorController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}