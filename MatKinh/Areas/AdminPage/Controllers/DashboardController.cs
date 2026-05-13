using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.ADMIN + "," + RoleConstants.STAFF + "," + RoleConstants.SHIPPER)]
    public class DashboardController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        [HttpGet]
        public ActionResult Index()
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var model = BuildDashboardViewModel(currentUser);
            return View(model);
        }

        private AdminDashboardVm BuildDashboardViewModel(TaiKhoan currentUser)
        {
            bool isAdmin = IsRole(currentUser, RoleConstants.ADMIN);
            bool isStaff = IsRole(currentUser, RoleConstants.STAFF);
            bool isShipper = IsRole(currentUser, RoleConstants.SHIPPER);

            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            DateTime tomorrow = today.AddDays(1);
            DateTime firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            DateTime firstDayNextMonth = firstDayOfMonth.AddMonths(1);

            var orderQuery = db.DonHangs.AsQueryable();

            if (isShipper)
            {
                orderQuery = orderQuery.Where(x => x.ShipperId == currentUser.TaiKhoanId);
            }

            var revenueOrders = db.DonHangs.Where(x =>
                x.TrangThai == OrderStatusConstants.DELIVERED &&
                x.TrangThaiThanhToan == PaymentConstants.PAID);

            var monthRevenueOrders = revenueOrders.Where(x =>
                (x.NgayHoanTat ?? x.NgayDat) >= firstDayOfMonth &&
                (x.NgayHoanTat ?? x.NgayDat) < firstDayNextMonth);

            var todayRevenueOrders = revenueOrders.Where(x =>
                (x.NgayHoanTat ?? x.NgayDat) >= today &&
                (x.NgayHoanTat ?? x.NgayDat) < tomorrow);

            var model = new AdminDashboardVm
            {
                RoleCode = currentUser.VaiTro != null ? currentUser.VaiTro.MaVaiTro : string.Empty,
                RoleName = currentUser.VaiTro != null ? currentUser.VaiTro.TenVaiTro : string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(currentUser.HoTen)
                    ? currentUser.TenDangNhap
                    : currentUser.HoTen,

                PendingOrders = orderQuery.Count(x => x.TrangThai == OrderStatusConstants.PENDING),
                ConfirmedOrders = orderQuery.Count(x => x.TrangThai == OrderStatusConstants.CONFIRMED),
                PreparingOrders = orderQuery.Count(x => x.TrangThai == OrderStatusConstants.PREPARING),
                AssignedOrders = orderQuery.Count(x => x.TrangThai == OrderStatusConstants.ASSIGNED_TO_SHIPPER),
                DeliveringOrders = orderQuery.Count(x => x.TrangThai == OrderStatusConstants.DELIVERING),
                DeliveredOrders = orderQuery.Count(x => x.TrangThai == OrderStatusConstants.DELIVERED),
                FailedOrders = orderQuery.Count(x => x.TrangThai == OrderStatusConstants.DELIVERY_FAILED),
                CancelledOrders = orderQuery.Count(x => x.TrangThai == OrderStatusConstants.CANCELLED)
            };

            if (isAdmin)
            {
                model.TotalRevenue = revenueOrders.Sum(x => (decimal?)x.TongThanhToan) ?? 0;
                model.MonthRevenue = monthRevenueOrders.Sum(x => (decimal?)x.TongThanhToan) ?? 0;
                model.TodayRevenue = todayRevenueOrders.Sum(x => (decimal?)x.TongThanhToan) ?? 0;

                model.ActiveAccounts = db.TaiKhoans.Count(x => x.IsActive);
                model.StaffAccounts = db.TaiKhoans.Count(x => x.IsActive && x.VaiTro.MaVaiTro == RoleConstants.STAFF);
                model.ShipperAccounts = db.TaiKhoans.Count(x => x.IsActive && x.VaiTro.MaVaiTro == RoleConstants.SHIPPER);

                model.PendingBlogs = db.BaiViets.Count(x => x.TrangThai == BlogStatusConstants.DRAFT);
                model.PublishedBlogs = db.BaiViets.Count(x => x.TrangThai == BlogStatusConstants.PUBLISHED);
            }

            if (isAdmin || isStaff)
            {
                model.ActiveProducts = db.SanPhams.Count(x => x.TrangThai == 1 && x.SoLuongTon > 0);
                model.OutOfStockProducts = db.SanPhams.Count(x => x.TrangThai == 1 && x.SoLuongTon <= 0);
                model.InactiveProducts = db.SanPhams.Count(x => x.TrangThai == 2);

                model.MyProducts = db.SanPhams.Count(x => x.CreatedById == currentUser.TaiKhoanId);
                model.MyBlogs = db.BaiViets.Count(x => x.CreatedById == currentUser.TaiKhoanId);
            }

            if (isShipper)
            {
                model.MyAssignedOrders = db.DonHangs.Count(x =>
                    x.ShipperId == currentUser.TaiKhoanId &&
                    x.TrangThai == OrderStatusConstants.ASSIGNED_TO_SHIPPER);

                model.MyDeliveringOrders = db.DonHangs.Count(x =>
                    x.ShipperId == currentUser.TaiKhoanId &&
                    x.TrangThai == OrderStatusConstants.DELIVERING);

                model.MyDeliveredOrders = db.DonHangs.Count(x =>
                    x.ShipperId == currentUser.TaiKhoanId &&
                    x.TrangThai == OrderStatusConstants.DELIVERED);

                model.MyFailedOrders = db.DonHangs.Count(x =>
                    x.ShipperId == currentUser.TaiKhoanId &&
                    x.TrangThai == OrderStatusConstants.DELIVERY_FAILED);
            }

            return model;
        }

        private TaiKhoan GetCurrentAccount()
        {
            var sessionAccount = Session["LoginInformation"] as TaiKhoan;
            if (sessionAccount == null)
            {
                return null;
            }

            return db.TaiKhoans
                .Include(x => x.VaiTro)
                .FirstOrDefault(x => x.TaiKhoanId == sessionAccount.TaiKhoanId && x.IsActive);
        }

        private bool IsRole(TaiKhoan account, string roleCode)
        {
            return account != null &&
                   account.VaiTro != null &&
                   string.Equals(account.VaiTro.MaVaiTro, roleCode, StringComparison.OrdinalIgnoreCase);
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