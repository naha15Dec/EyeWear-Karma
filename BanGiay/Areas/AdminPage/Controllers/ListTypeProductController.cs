using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
namespace BanGiay.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize (Roles = "Quản trị")]
    public class ListTypeProductController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        static bool CheckUpdate;
        public ActionResult ProductType()
        {
            UpdateInterface();
            return View();
        }
        /// <summary>
        /// Dùng để thêm loại sản phẩm
        /// </summary>
        /// <param name="lsp"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddTypeProduct(loaiSP lsp)
        {
            if (!CheckUpdate)
            {
                db.loaiSPs.Add(lsp);
            }
            else
            {
                loaiSP loaisp = db.loaiSPs.Find(lsp.maLoai);
                loaisp.tenLoai = lsp.tenLoai;
                loaisp.ghiChu = lsp.ghiChu;
                CheckUpdate = false;
            }
            ModelState.Clear();
            db.SaveChanges();
            UpdateInterface();
            return View("ProductType");
        }
        /// <summary>
        /// Dùng để xóa 1 loại sản phẩm
        /// </summary>
        /// <param name="ml"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string ml)
        {
            try{
                int maloai = int.Parse(ml);
                loaiSP lsp = db.loaiSPs.Where(m => m.maLoai == maloai).FirstOrDefault();
                db.loaiSPs.Remove(lsp);
                db.SaveChanges();
                UpdateInterface();
                return View("ProductType");
            }
            catch
            {
                return RedirectToAction("ProductType");
            }
        }
        /// <summary>
        /// Dùng để cập nhật 1 loại sản phẩm
        /// </summary>
        /// <param name="ml"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(string ml)
        {
            int maloai = int.Parse(ml);
            loaiSP lsp = db.loaiSPs.Where(m => m.maLoai == maloai).FirstOrDefault();
            CheckUpdate = true;
            db.SaveChanges();
            UpdateInterface();
            return View("ProductType",lsp) ;
        }
        /// <summary>
        /// Cập nhật lại danh sách kiểu sản phẩm
        /// </summary>
        private void UpdateInterface()
        {
            ViewData["typeProductList"] = db.loaiSPs.ToList();
        }

    }
}