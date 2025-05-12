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
    public class QLChuDeController : Controller
    {
        DienDanEntities db = new DienDanEntities();
        // GET: Admin/QLChuDe
        [AuthorizeRole("Admin")]
        public ActionResult QLLoaiCD(int? page)
        {
            var ds = db.LoaiCDs.OrderBy(l => l.TenLoai).ToList();
            int iSize = 10;
            int iPageNumber = (page ?? 1);

            return View(ds.ToPagedList(iPageNumber, iSize));
        }
        [AuthorizeRole("Admin")]
        [HttpGet]
        public ActionResult TaoLoaiCD()
        {
            return View();
        }
        [HttpPost]
        public ActionResult TaoLoaiCD(LoaiCD lcd)
        {
            if (string.IsNullOrWhiteSpace(lcd.TenLoai))
            {
                // Thêm thông báo lỗi vào ModelState
                ModelState.AddModelError("TenLoai", "Vui lòng nhập tên loại.");
            }
            if (ModelState.IsValid)
            {
                var tenloaicd = db.LoaiCDs.Where(l => l.TenLoai.Contains(lcd.TenLoai));
                if (tenloaicd.Any())
                {
                    TempData["ErrorMessage"] = "Loại chủ đề này đã tồn tại!";
                    return View();
                }
                
                lcd.TenLoai = lcd.TenLoai;
                db.LoaiCDs.Add(lcd);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Thêm loại chủ đề thành công!";
                return RedirectToAction("TaoLoaiCD", new LoaiCD());
            }
            return View();
        }
        [HttpGet]
        public ActionResult SuaLoaiCD(int id)
        {
            var ds = db.LoaiCDs.Where(l => l.MaLoai == id).SingleOrDefault();
            return View(ds);
        }
        [HttpPost]
        public ActionResult SuaLoaiCD(LoaiCD lcd, string MaLoai)
        {
            if (string.IsNullOrWhiteSpace(lcd.TenLoai))
            {
                // Thêm thông báo lỗi vào ModelState
                ModelState.AddModelError("TenLoai", "Vui lòng nhập tên loại.");
            }
            if (ModelState.IsValid)
            {
                var tenloaicd = db.LoaiCDs.Where(l => l.TenLoai.Contains(lcd.TenLoai));
                if (tenloaicd.Any())
                {
                    TempData["ErrorMessage"] = "Loại chủ đề này đã tồn tại!";
                    return View(lcd);
                }
                var loaicd = db.LoaiCDs.Find(MaLoai);
                loaicd.TenLoai = lcd.TenLoai;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Thông tin loại chủ đề đã được cập nhập!";
                return View(loaicd);
            }
            return View(lcd);
        }

        public ActionResult ChuDe(int? page)
        {
            var ds = db.LoaiCDs.OrderByDescending(l => l.TenLoai).ToList();
            int iSize = 3;
            int iPageNumber = (page ?? 1);
            return View(ds.ToPagedList(iPageNumber, iSize));
        }
        [HttpGet]
        public ActionResult SuaCD(int id)
        {
            var ds = db.ChuDes.Include("LoaiCD").Where(c => c.MaCD == id).SingleOrDefault();
            return View(ds);
        }
        [HttpPost]
        public ActionResult SuaCD(ChuDe cd, int MaCD, int MaLoai)
        {
            if (string.IsNullOrWhiteSpace(cd.TenCD))
            {
                // Thêm thông báo lỗi vào ModelState
                ModelState.AddModelError("TenCD", "Vui lòng nhập tên chủ đề.");
            }
            if (ModelState.IsValid)
            {
                var tenlcd = db.ChuDes.Where(c => c.TenCD.Contains(cd.TenCD) && c.LoaiCD.MaLoai == MaLoai).Select(c => c.LoaiCD.TenLoai).FirstOrDefault();
                if (tenlcd != null)
                {
                    TempData["ErrorMessage"] = $"Chủ đề này đã tồn tại trong '{tenlcd}'!";
                    cd.LoaiCD = db.LoaiCDs.Find(MaLoai);
                    return View(cd);
                }
                var cds = db.ChuDes.Find(MaCD);
                cds.TenCD = cd.TenCD;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Thông tin chủ đề đã được cập nhập!";
                return View(cds);
            }
            cd.LoaiCD = db.LoaiCDs.Find(MaLoai);
            return View(cd);
        }
        [HttpGet]
        public ActionResult ThemCD(int id)
        {
            var ds = db.LoaiCDs.Where(l => l.MaLoai == id).SingleOrDefault();
            ViewBag.TenLoai = ds.TenLoai;
            ViewBag.MaLoai = ds.MaLoai;
            return View();
        }
        [HttpPost]
        public ActionResult ThemCD(ChuDe cd, int MaLoai)
        {
            if (string.IsNullOrWhiteSpace(cd.TenCD))
            {
                // Thêm thông báo lỗi vào ModelState
                ModelState.AddModelError("TenCD", "Vui lòng nhập tên chủ đề.");
            }
            if (ModelState.IsValid)
            {
                var tenlcd = db.ChuDes.Where(c => c.TenCD.Contains(cd.TenCD) && c.LoaiCD.MaLoai == MaLoai).Select(c => c.LoaiCD.TenLoai).FirstOrDefault();
                if (tenlcd != null)
                {
                    TempData["ErrorMessage"] = "Chủ đề này đã tồn tại!";
                }
                else
                {
                    cd.TenCD = cd.TenCD;
                    cd.MaLoai = MaLoai;
                    db.ChuDes.Add(cd);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Thêm chủ đề thành công!";

                    return RedirectToAction("ThemCD", new { id = MaLoai });
                }
            }
            var ds = db.LoaiCDs.Where(l => l.MaLoai == MaLoai).SingleOrDefault();
            ViewBag.TenLoai = ds.TenLoai;
            ViewBag.MaLoai = ds.MaLoai;
            return View(cd);
        }
    }
}