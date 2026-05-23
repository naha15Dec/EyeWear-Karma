using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace MatKinh.Controllers
{
    public class TryOnController : Controller
    {
        public ActionResult Index(string glass = null)
        {
            string folderPath = Server.MapPath("~/Content/tryon");

            var glasses = new List<string>();

            if (Directory.Exists(folderPath))
            {
                glasses = Directory
                    .GetFiles(folderPath, "*.png")
                    .Select(path => "/Content/tryon/" + Path.GetFileName(path))
                    .ToList();
            }

            ViewBag.InitialGlass = glass;

            return View(glasses);
        }
    }
}