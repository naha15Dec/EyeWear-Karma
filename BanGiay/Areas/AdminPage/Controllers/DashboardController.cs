using BanGiay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BanGiay.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize (Roles = "Quản trị,Nhân viên")]
    public class DashboardController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}