using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
namespace BanGiay.Controllers
{
    public class HomeController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        public ActionResult Index()
        {
            UpdateInterface();
            return View();
        }
        /// <summary>
        /// Hàm này tìm kiếm sản phẩm bằng mã sản phẩm
        /// </summary>
        /// <param name="idProduct"></param>
        /// <returns></returns>
        public ActionResult FindProductByID(string idProduct)
        {
            return View();
        }
        /// <summary>
        /// Hàm này dùng để cập nhật lại các danh sách sản phẩm
        /// </summary>
        private void UpdateInterface()
        {
            ViewData["listDiscountProduct"] = db.sanPhams.Where(m => m.giamGia > 0).Take(8).ToList(); ;
            ViewData["listNewProduct"] = db.sanPhams.Where(m => m.giamGia <= 0).OrderByDescending(m => m.ngayDang).Take(8).ToList();
            ViewData["listDealHot"] = db.sanPhams.OrderByDescending(m => m.giamGia).Take(2).ToList();
        }
    }
}