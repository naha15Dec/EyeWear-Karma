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
    public class ListBrandController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        static bool checkUpdate;
        public ActionResult Brand()
        {
            UpdateInterface();
            return View();
        }
        /// <summary>
        /// Hàm này để thêm thương hiệu vào
        /// </summary>
        /// <param name="th"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBrand(thuongHieu th)
        {
            if (ModelState.IsValid)
            {
                if (!checkUpdate)
                {
                    db.thuongHieux.Add(th);
                }
                else
                {
                    thuongHieu thuonghieu = db.thuongHieux.Find(th.maThuongHieu);
                    thuonghieu.tenThuongHieu = th.tenThuongHieu;
                    thuonghieu.xuatXu = th.xuatXu;
                    thuonghieu.ghiChu = th.ghiChu;
                    checkUpdate = false;
                }
            }
            ModelState.Clear();
            db.SaveChanges();
            UpdateInterface();
            return View("Brand");
        }
        /// <summary>
        /// Dùng để xóa thương hiệu
        /// </summary>
        /// <param name="mth"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string mth)
        {
            try
            {
                int ma = int.Parse(mth);
                thuongHieu th = db.thuongHieux.Find(ma);
                db.thuongHieux.Remove(th);
                db.SaveChanges();
                UpdateInterface();
                return View("Brand");
            }
            catch
            {
                return RedirectToAction("Brand");
            }
        }
        /// <summary>
        /// Dùng để cập nhật lại thương hiệu
        /// </summary>
        /// <param name="mth"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(string mth)
        {
            int ma = int.Parse(mth);
            thuongHieu th = db.thuongHieux.Find(ma);
            checkUpdate = true;
            UpdateInterface();
            return View("Brand",th);
        }
        /// <summary>
        /// Cập nhật giao diện brand
        /// </summary>
        private void UpdateInterface()
        {
            List<thuongHieu> l = db.thuongHieux.ToList();
            ViewData["danhSachThuongHieu"] = l;
        }
        
    }
}