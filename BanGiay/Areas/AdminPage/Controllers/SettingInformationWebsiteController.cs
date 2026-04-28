using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
namespace BanGiay.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = "Quản trị")]
    public class SettingInformationWebsiteController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        public ActionResult Index()
        {
            updateInterface();
            return View();
        }
        [HttpPost]
        public ActionResult ChangeInformation(thongTinCuaHang inf)
        {
            if (ModelState.IsValid)
            {
                inf.thoiGian = DateTime.Now;
                inf.taiKhoan = (Session["LoginInformation"] as taiKhoanThanhVien).taiKhoan;
                db.thongTinCuaHangs.Add(inf);
                db.SaveChanges();
            }
            updateInterface();
            return View("Index");
        }
        public ActionResult DetailsChange(int id)
        {
            updateInterface();
            return View("Index");
        }
           
        public ActionResult DeleteHistoryChange(int id)
        {
            thongTinCuaHang inf = db.thongTinCuaHangs.FirstOrDefault(m => m.maThongTin == id);
            db.thongTinCuaHangs.Remove(inf);
            db.SaveChanges();
            updateInterface();
            return View("Index");
        }

        public void updateInterface()
        {
            ViewData["MostRecentChanges"] = db.thongTinCuaHangs.OrderByDescending(m => m.thoiGian).FirstOrDefault();
            ViewData["HistoryChanges"] = db.thongTinCuaHangs.OrderByDescending(m => m.maThongTin).ToList();
        }
    }
}