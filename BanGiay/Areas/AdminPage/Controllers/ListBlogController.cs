using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
using BanGiay.ViewModel;

namespace BanGiay.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = "Quản trị,Nhân viên")]
    public class ListBlogController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        static bool check = false;

        // GET: AdminPage/ListBlog
        public ActionResult BlogList(string typeBlog = "not enable")
        {
            check = (typeBlog.Equals("enable"));
            UpdateInterface(null);
            return View();
        }

        // GET: Thêm bài viết
        public ActionResult AddBlog()
        {
            var loginInfo = Session["LoginInformation"] as taiKhoanThanhVien;

            var vm = new PostVM
            {
                DatePost = DateTime.Now,
                View = 0,
                Account = loginInfo != null ? loginInfo.taiKhoan : null
            };

            return View(vm);
        }

        /// <summary>
        /// Hàm này dùng để thêm bài viết
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBlog(PostVM pvm, HttpPostedFileBase imageAvatar)
        {
            // Lấy thông tin tài khoản từ Session
            var loginInfo = Session["LoginInformation"] as taiKhoanThanhVien;
            if (loginInfo == null)
            {
                ModelState.AddModelError("", "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.");
                return View(pvm);
            }

            // Gán các giá trị hệ thống (không cho người dùng nhập)
            pvm.DatePost = DateTime.Now;
            pvm.Account = loginInfo.taiKhoan;
            pvm.View = 0;

            // Những field này không nhập trực tiếp từ form nhưng có thể có [Required]
            // → bỏ khỏi ModelState để không bị Invalid.
            ModelState.Remove("DatePost");
            ModelState.Remove("Account");
            ModelState.Remove("View");

            if (!ModelState.IsValid)
            {
                // Xem ValidationSummary trên View để biết đang lỗi field nào
                return View(pvm);
            }

            try
            {
                var post = new baiViet
                {
                    maBV = string.Format("{0:MMddmmss}", DateTime.Now),
                    tenBV = pvm.NamePost,
                    ngayDang = pvm.DatePost,
                    luotXem = pvm.View,
                    ndTomTat = pvm.SummaryContent,
                    noiDung = pvm.Content,
                    taiKhoan = pvm.Account,
                    daDuyet = false
                };

                ImagePost(imageAvatar, post);

                db.baiViets.Add(post);
                db.SaveChanges();
                UpdateInterface(null);

                return RedirectToAction("BlogList");
            }
            catch (Exception ex)
            {
                // Không nuốt lỗi nữa, để dễ debug
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu bài viết: " + ex.Message);
                return View(pvm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string idPost)
        {
            baiViet baiViet = db.baiViets.Find(idPost);
            if (baiViet != null)
            {
                db.baiViets.Remove(baiViet);
                db.SaveChanges();
            }
            UpdateInterface(null);
            return View("BlogList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(string idPost, PostVM pvm, HttpPostedFileBase imageAvatar)
        {
            if (ModelState.IsValid)
            {
                baiViet bv = db.baiViets.FirstOrDefault(m => m.maBV == idPost);
                if (bv != null)
                {
                    bv.tenBV = pvm.NamePost;
                    bv.ndTomTat = pvm.SummaryContent;
                    bv.noiDung = pvm.Content;

                    ImagePost(imageAvatar, bv);
                    db.SaveChanges();
                    UpdateInterface(null);
                }
            }
            return View("BlogList");
        }

        public ActionResult FindPostByName(string namePost)
        {
            UpdateInterface(namePost);
            return View("BlogList");
        }

        private void UpdateInterface(string namePost)
        {
            taiKhoanThanhVien tk = Session["LoginInformation"] as taiKhoanThanhVien;
            ViewBag.HeaderList = (check == true ? "Danh sách bài viết" : "Danh sách bài viết chờ duyệt");

            if (tk != null)
            {
                ViewData["ArticleList"] = db.baiViets
                    .Where(m =>
                        m.taiKhoan.Equals(tk.taiKhoan) &&
                        m.daDuyet == check &&
                        (string.IsNullOrEmpty(namePost) || m.tenBV.Contains(namePost)))
                    .ToList();
            }
            else
            {
                ViewData["ArticleList"] = Enumerable.Empty<baiViet>().ToList();
            }
        }

        /// <summary>
        /// Hàm này dùng để lưu ảnh và set đường dẫn ảnh
        /// </summary>
        public void ImagePost(HttpPostedFileBase images, baiViet post)
        {
            if (images != null && images.ContentLength > 0)
            {
                string virtualPath = "/Asset/SaveImgBlog/";
                string fileName = Guid.NewGuid() + Path.GetExtension(images.FileName);
                string physicalPath = Server.MapPath(virtualPath);

                if (!Directory.Exists(physicalPath))
                {
                    Directory.CreateDirectory(physicalPath);
                }

                string fullPath = Path.Combine(physicalPath, fileName);
                images.SaveAs(fullPath);

                post.hinhDD = virtualPath + fileName;
            }
            else
            {
                post.hinhDD = "";
            }
        }
    }
}
