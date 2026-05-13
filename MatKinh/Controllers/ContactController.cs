using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using MatKinh.Models;

namespace MatKinh.Controllers
{
    public class ContactController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        [HttpGet]
        public ActionResult Index()
        {
            ThongTinCuaHang storeInfo = db.ThongTinCuaHangs
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefault();

            if (storeInfo == null)
            {
                ViewBag.ContactNotice = "Thông tin cửa hàng đang được cập nhật.";
            }

            return View(storeInfo);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}