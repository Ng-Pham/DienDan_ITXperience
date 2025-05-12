using DienDanThaoLuan.Attributes;
using DienDanThaoLuan.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DienDanThaoLuan.Areas.Admin.Controllers
{
    //[SessionTimeout]
    [Authorize]
    public class HomeAdminController : Controller
    {
        DienDanEntities db = new DienDanEntities();
        // GET: Admin/Home
        [AuthorizeRole("Admin")]
        public ActionResult Index()
        {
            return View();
        }
        [AuthorizeRole("Admin")]
        public ActionResult GopY(int? page)
        {
            var ds = db.Gopies.OrderByDescending(l => l.NgayGui).ToList();
            int iSize = 3;
            int iPageNumber = (page ?? 1);

            return View(ds.ToPagedList(iPageNumber, iSize));
        }
        [HttpPost]
        public JsonResult CapNhatTrangThai()
        {
            var ds = db.Gopies.Where(d => d.TrangThai == false).ToList();
            foreach (var d in ds)
            {
                d.TrangThai = true;
            }
            db.SaveChanges();
            return Json(new { success = true });
        }
    }
}