using DienDanThaoLuan.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DienDanThaoLuan.Controllers
{
    public class ChuDeController : Controller
    {
        // GET: ChuDe
        DienDanEntities db = new DienDanEntities();
        public ActionResult ChuDe(int? page, int id)
        {
            var dscd = db.ChuDes
                .Select(cd => new SoBaiChuDe
                {
                    ChuDe = cd,
                    SoBai = db.BaiViets
                        .Where(bv => bv.TrangThai.Contains("Đã duyệt") && bv.MaCD == cd.MaCD)
                        .Count()
                }).Where(cd => cd.ChuDe.LoaiCD.MaLoai == id).OrderBy(cd => cd.ChuDe.TenCD).ToList();
            if (!dscd.Any())
            {
                ViewBag.Message = "Chưa có chủ đề nào cho loại chủ đề này";
                var ttcd = db.LoaiCDs.Where(l => l.MaLoai == id).FirstOrDefault();
                ViewBag.MaLoai = ttcd.MaLoai;
                ViewBag.TenLoai = ttcd.TenLoai;
            }
            
            int iSize = 14;
            int iPageNumber = (page ?? 1);
            return View(dscd.ToPagedList(iPageNumber, iSize));
        }
        [HttpGet]
        public ActionResult BaiVietTheoCD(int? page, int id)
        {
            var dsbv = db.BaiViets.Select(bv => new BaiVietView
            {
                BaiViet = bv,
                SoBL = db.BinhLuans.Count(bl => bl.MaBV == bv.MaBV),
                IsAdmin = db.NguoiDungs.Any(n => n.MaND == bv.MaND && n.LoaiND.TenLoai == "admin"),
            })
            .Where(b => b.BaiViet.MaCD == id && b.BaiViet.TrangThai.Contains("Đã duyệt"))
            .OrderByDescending(bv => bv.BaiViet.NgayDang)
            .ToList();
            if (!dsbv.Any())
            {
                ViewBag.Message = "Chưa có bài viết nào cho chủ đề này";
                var cd = db.ChuDes.FirstOrDefault(c => c.MaCD == id);
                ViewBag.MaCD = cd.MaCD;
                ViewBag.TenCD = cd.TenCD;
                ViewBag.TenLoai = cd.LoaiCD.TenLoai;
                ViewBag.MaLoai = cd.MaLoai;
            }            
            int iSize = 14;
            int iPageNumber = (page ?? 1);
            return View(dsbv.ToPagedList(iPageNumber, iSize));
        }
        public ActionResult Loc(int? page, string sortOrder, int id)
        {
            ViewBag.NewestSort = sortOrder == "newest" ? "newest_desc" : "newest";
            ViewBag.OldestSort = sortOrder == "oldest" ? "oldest_desc" : "oldest";
            var baiVietViewList = db.BaiViets.Select(bv => new BaiVietView
            {
                BaiViet = bv,
                SoBL = db.BinhLuans.Count(bl => bl.MaBV == bv.MaBV),
                CodeContent = null,
                IsAdmin = db.NguoiDungs.Any(n => n.MaND == bv.MaND && n.LoaiND.TenLoai == "admin"),
            }).Where(b => b.BaiViet.MaCD == id).ToList();
            
            switch (sortOrder)
            {
                case "newest":
                    baiVietViewList = baiVietViewList.Where(b => b.BaiViet.TrangThai == "Đã duyệt").OrderByDescending(b => b.BaiViet.NgayDang).ToList();
                    break;
                case "oldest":
                    baiVietViewList = baiVietViewList.Where(b => b.BaiViet.TrangThai == "Đã duyệt").OrderBy(b => b.BaiViet.NgayDang).ToList();
                    break;
                case "az":
                    baiVietViewList = baiVietViewList.Where(b => b.BaiViet.TrangThai == "Đã duyệt").OrderBy(b => b.BaiViet.TieuDeBV).ToList();
                    break;
                case "za":
                    baiVietViewList = baiVietViewList.Where(b => b.BaiViet.TrangThai == "Đã duyệt").OrderByDescending(b => b.BaiViet.TieuDeBV).ToList();
                    break;
                default:
                    break;
            }

            if (!baiVietViewList.Any())
            {
                var cd = db.ChuDes.FirstOrDefault(c => c.MaCD == id);
                if (cd != null)
                {
                    ViewBag.MaCD = cd.MaCD;
                    ViewBag.TenCD = cd.TenCD;
                    ViewBag.TenLoai = cd.LoaiCD.TenLoai;
                    ViewBag.MaLoai = cd.MaLoai;
                }
                ViewBag.Message = "Chưa có bài viết nào cho chủ đề này";
            }
            int iSize = 8;
            int iPageNumber = (page ?? 1);
            return View("BaiVietTheoCD", baiVietViewList.ToPagedList(iPageNumber, iSize));
        }
    }
}