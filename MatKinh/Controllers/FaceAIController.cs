using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;

namespace MatKinh.Controllers
{
    public class FaceAIController : Controller
    {
        private BanMatKinhEntities db = new BanMatKinhEntities();

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetRecommendedProducts(string faceShape)
        {
            var query = db.SanPhams.Where(p => p.TrangThai == 1);

            // Gợi ý theo dáng mặt
            if (faceShape == "round")
            {
                query = query.Where(p =>
                    p.TenSanPham.Contains("vuông") ||
                    p.TenSanPham.Contains("Wayfarer") ||
                    p.TenSanPham.Contains("Clubmaster")
                );
            }
            else if (faceShape == "square")
            {
                query = query.Where(p =>
                    p.TenSanPham.Contains("tròn") ||
                    p.TenSanPham.Contains("oval") ||
                    p.TenSanPham.Contains("bo tròn")
                );
            }
            else if (faceShape == "long")
            {
                query = query.Where(p =>
                    p.TenSanPham.Contains("oversize") ||
                    p.TenSanPham.Contains("aviator") ||
                    p.TenSanPham.Contains("tròn")
                );
            }
            else if (faceShape == "heart")
            {
                query = query.Where(p =>
                    p.TenSanPham.Contains("oval") ||
                    p.TenSanPham.Contains("rimless") ||
                    p.TenSanPham.Contains("mỏng")
                );
            }
            else
            {
                query = db.SanPhams.Where(p => p.TrangThai == 1);
            }

            var products = query
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToList();

            // fallback nếu không có dữ liệu phù hợp
            if (products.Count == 0)
            {
                products = db.SanPhams
                    .Where(p => p.TrangThai == 1)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(4)
                    .ToList();
            }

            var result = products.Select(p => new
            {
                id = p.SanPhamId,
                name = p.TenSanPham,
                price = p.GiaBan,
                image = p.HinhAnhChinh
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}