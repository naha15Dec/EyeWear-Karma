using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using MatKinh.Models;

namespace MatKinh.Controllers
{
    public class ContactController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        public ActionResult Index()
        {
            ThongTinCuaHang storeInfo = db.ThongTinCuaHangs
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefault();

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