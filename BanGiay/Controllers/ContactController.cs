using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
namespace BanGiay.Controllers
{
    public class ContactController : Controller
    {
        public ActionResult Index()
        {
            DoAnLTW2Entities db = new DoAnLTW2Entities();
            thongTinCuaHang inf = db.thongTinCuaHangs.OrderByDescending(m=>m.thoiGian).FirstOrDefault();
            return View(inf);
        }
    }
}