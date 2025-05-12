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
    public class ThongBaoTongController : Controller
    {
        DienDanEntities db = new DienDanEntities();
        // GET: Admin/ThongBaoTong
        [AuthorizeRole("Admin")]
        public ActionResult Index(int? page)
        {
            var ds = db.ThongBaos.Where(l => l.LoaiTB.TenLoai.Contains("hệ thống")).ToList();
            int iSize = 10;
            int iPageNumber = (page ?? 1);
            return View(ds.ToPagedList(iPageNumber, iSize));
        }
        [AuthorizeRole("Admin")]
        [HttpPost]
        public ActionResult Them(string noidung)
        {
            try
            {
                var tbnew = new ThongBao();
                tbnew.NoiDung = $"<NoiDung>{noidung}</NoiDung>";
                tbnew.NgayTB = DateTime.Now;
                tbnew.MaLoaiTB = db.LoaiTBs.Where(ltb => ltb.TenLoai.Contains("hệ thống")).Select(ltb => ltb.MaLoaiTB).FirstOrDefault();
                tbnew.TrangThai = true;
                db.ThongBaos.Add(tbnew);
                db.SaveChanges();

                return Json(new { success = true, message = "Thông báo đã được thêm thành công!" });
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu xảy ra
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        [HttpPost]
        public ActionResult Sua(string maTB, string noidung)
        {
            try
            {
                var tb = db.ThongBaos.Find(maTB);
                tb.NoiDung = $"<NoiDung>{noidung}</NoiDung>";
                tb.NgayTB = DateTime.Now;
                db.SaveChanges();

                return Json(new { success = true, message = "Thông báo đã được sửa thành công!" });
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu xảy ra
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        [HttpPost]
        public ActionResult Xoa(int maTB)
        {
            try
            {
                var tb = db.ThongBaos.SingleOrDefault(t => t.MaTB == maTB);
                db.ThongBaos.Remove(tb);
                db.SaveChanges();

                return Json(new { success = true, message = "Thông báo đã được xóa thành công!" });
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu xảy ra
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        [HttpPost]
        public ActionResult ChonHien(int maTB)
        {
            try
            {
                var tb = db.ThongBaos.SingleOrDefault(t => t.MaTB == maTB);
                tb.NgayTB = DateTime.Now;
                db.SaveChanges();

                return Json(new { success = true, message = "Thông báo đã được thay thành công!" });
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu xảy ra
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}