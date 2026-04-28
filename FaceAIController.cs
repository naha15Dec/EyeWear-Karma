using System;
using System.Linq;
using System.Web.Mvc;
using BanGiay.Models;

namespace BanGiay.Controllers
{
    public class FaceAIController : Controller
    {
        private DoAnLTW2Entities db = new DoAnLTW2Entities();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Test()
        {
            return Content("FaceAI Controller OK");
        }

        [HttpGet]
        public JsonResult GetRecommendedProducts(string faceShape)
        {
            var result = db.sanPhams
                .Where(p => p.trangThai == true)
                .OrderByDescending(p => p.ngayDang)
                .Take(4)
                .Select(p => new
                {
                    id = p.maSP,
                    name = p.tenSP,
                    price = p.giaBan,
                    image = p.hinhDD
                })
                .ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}