using System;
using System.Web;

namespace MatKinh.Models
{
    public static class UserBehaviorLogger
    {
        public static void Log(
            BanMatKinhEntities db,
            HttpSessionStateBase session,
            int sanPhamId,
            string loaiHanhVi,
            decimal trongSo,
            string nguon = null,
            string ghiChu = null)
        {
            if (db == null || session == null || sanPhamId <= 0 || string.IsNullOrWhiteSpace(loaiHanhVi))
            {
                return;
            }

            int? khachHangId = GetCurrentKhachHangId(session);
            string sessionId = GetOrCreateBehaviorSessionId(session);

            var entity = new HanhViNguoiDung
            {
                KhachHangId = khachHangId,
                SanPhamId = sanPhamId,
                LoaiHanhVi = loaiHanhVi,
                Nguon = nguon,
                SessionId = khachHangId.HasValue ? null : sessionId,
                TrongSo = trongSo,
                GhiChu = ghiChu,
                CreatedAt = DateTime.Now
            };

            db.HanhViNguoiDungs.Add(entity);
        }

        public static int? GetCurrentKhachHangId(HttpSessionStateBase session)
        {
            if (session == null)
            {
                return null;
            }

            if (session["KhachHangId"] == null)
            {
                return null;
            }

            return session["KhachHangId"] as int?;
        }

        public static string GetOrCreateBehaviorSessionId(HttpSessionStateBase session)
        {
            if (session == null)
            {
                return Guid.NewGuid().ToString("N");
            }

            if (session["BehaviorSessionId"] == null)
            {
                session["BehaviorSessionId"] = Guid.NewGuid().ToString("N");
            }

            return session["BehaviorSessionId"].ToString();
        }
    }
}