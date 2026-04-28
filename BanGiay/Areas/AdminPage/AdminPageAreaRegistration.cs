using BanGiay.Models;
using System.Web.Mvc;

namespace BanGiay.Areas.AdminPage
{
    public class AdminPageAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "AdminPage";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "AdminPage_default",
                "AdminPage/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}