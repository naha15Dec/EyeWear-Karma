using System;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MatKinh.Controllers
{
    public class FaceAIController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetRecommendedProducts(string faceShape)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(faceShape))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Thiếu dáng mặt."
                    }, JsonRequestBehavior.AllowGet);
                }

                faceShape = faceShape.Trim().ToUpper();

                var validShapes = new[] { "ROUND", "OVAL", "SQUARE", "HEART", "LONG" };

                if (!validShapes.Contains(faceShape))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Dáng mặt không hợp lệ."
                    }, JsonRequestBehavior.AllowGet);
                }

                var products = (
                    from rule in db.RuleGoiYKinhTheoMats
                    join kg in db.KieuGongs
                        on rule.KieuGongId equals kg.KieuGongId
                    join sp in db.SanPhams
                        on kg.KieuGongId equals sp.KieuGongId
                    where rule.MaHinhDangMat == faceShape
                          && rule.IsActive == true
                          && kg.IsActive == true
                          && sp.TrangThai == 1
                          && sp.SoLuongTon > 0
                    orderby rule.DiemPhuHop descending,
                            sp.IsFeatured descending,
                            sp.GiaBan ascending
                    select new
                    {
                        id = sp.SanPhamId,
                        name = sp.TenSanPham,
                        price = sp.GiaBan,
                        image = string.IsNullOrEmpty(sp.HinhAnhChinh)
                            ? "/Content/images/no-image.png"
                            : sp.HinhAnhChinh,
                        frameCode = kg.MaKieuGong,
                        frameName = kg.TenKieuGong,
                        score = rule.DiemPhuHop,
                        reason = rule.GiaiThich
                    }
                ).Take(8).ToList();

                if (products.Count == 0)
                {
                    var fallbackProducts = db.SanPhams
                        .Where(p => p.TrangThai == 1 && p.SoLuongTon > 0)
                        .OrderByDescending(p => p.IsFeatured)
                        .ThenByDescending(p => p.CreatedAt)
                        .Take(8)
                        .Select(p => new
                        {
                            id = p.SanPhamId,
                            name = p.TenSanPham,
                            price = p.GiaBan,
                            image = string.IsNullOrEmpty(p.HinhAnhChinh)
                                ? "/Content/images/no-image.png"
                                : p.HinhAnhChinh,
                            frameCode = "",
                            frameName = "Sản phẩm nổi bật",
                            score = 0,
                            reason = "Chưa có sản phẩm khớp dáng mặt, hệ thống hiển thị sản phẩm nổi bật."
                        })
                        .ToList();

                    return Json(new
                    {
                        success = true,
                        faceShape = faceShape,
                        total = fallbackProducts.Count,
                        isFallback = true,
                        products = fallbackProducts
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    faceShape = faceShape,
                    total = products.Count,
                    isFallback = false,
                    products = products
                }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi khi lấy sản phẩm gợi ý."
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<ActionResult> PredictByModel()
        {
            try
            {
                if (Request.Files.Count == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không có ảnh gửi lên."
                    });
                }

                var file = Request.Files[0];

                using (var client = new System.Net.Http.HttpClient())
                using (var content = new System.Net.Http.MultipartFormDataContent())
                {
                    client.Timeout = TimeSpan.FromSeconds(20);

                    var streamContent = new System.Net.Http.StreamContent(file.InputStream);

                    if (!string.IsNullOrEmpty(file.ContentType))
                    {
                        streamContent.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    }

                    content.Add(streamContent, "image", file.FileName);

                    var response = await client.PostAsync("http://127.0.0.1:8000/predict", content);
                    var json = await response.Content.ReadAsStringAsync();

                    return Content(json, "application/json");
                }
            }
            catch
            {
                return Json(new
                {
                    success = false,
                    message = "Không gọi được AI model."
                });
            }
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