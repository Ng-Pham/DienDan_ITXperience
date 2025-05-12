using DienDanThaoLuan.Models;
using Ganss.Xss;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DienDanThaoLuan.Controllers
{
    public class HomeController : Controller
    {
        DienDanEntities db = new DienDanEntities();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult _PartialHeader()
        {
            return PartialView();
        }
        public ActionResult _PartialThongBao()
        {
            var tb = db.ThongBaos.Where(t => t.LoaiTB.TenLoai == "Thông báo hệ thống").OrderByDescending(t => t.NgayTB).ToList();
            
            return PartialView(tb);
        }
        public ActionResult _PartialMenu()
        {
            var userId = Session["UserId"] as string;
            var adId = Session["AdminId"] as string;
            if (userId != null)
            {
                int maND = Convert.ToInt32(userId);
                var dstb = db.ThongBaos.Where(n => n.MaND == maND).OrderByDescending(n => n.NgayTB).ToList();

                var slchuadoc = dstb.Count(n => n.TrangThai == false);
                if (slchuadoc != 0)
                    ViewBag.UnreadCount = slchuadoc;
            }
            if (adId != null)
            {
                int maND = Convert.ToInt32(adId);
                var dstb = db.ThongBaos.Where(t => t.MaND == maND || t.LoaiTB.TenLoai.Contains("tố cáo")).OrderByDescending(n => n.NgayTB).ToList();

                var slchuadoc = dstb.Count(n => n.TrangThai == false);
                if (slchuadoc != 0)
                    ViewBag.UnreadCount = slchuadoc;

                var tb = db.BaiViets.Where(n => n.TrangThai.Contains("Chờ duyệt")).OrderByDescending(n => n.NgayDang).ToList();

                var slchuaduyet = tb.Count();
                if (slchuaduyet != 0)
                    ViewBag.SLBV = slchuaduyet;
                var dsgy = db.Gopies.Where(l => l.TrangThai == false).OrderByDescending(l => l.NgayGui).ToList();
                var slgy = dsgy.Count();
                if (slgy != 0)
                    ViewBag.SLGY = slgy;
            }
            return PartialView();
        }
        public ActionResult _PartialChuDe()
        {
            var listcd = db.LoaiCDs.OrderBy(c => c.TenLoai);
            return PartialView(listcd);
        }
        public ActionResult _PartialCDThaoLuanNhieu()
        {
            var dsttcd = db.ChuDes
                .Select(cd => new SoBaiChuDe
                {
                    ChuDe = cd,
                    SoBai = db.BaiViets
                        .Where(bv => bv.TrangThai.Contains("Đã duyệt") && bv.MaCD == cd.MaCD)
                        .Count()
                })
                .ToList();

            // Lấy 3 chủ đề được thảo luận nhiều nhất
            var chuDeDuocThaoLuanNhieu = dsttcd
                .OrderByDescending(cd => cd.SoBai)
                .Take(3)
                .ToList();

            return PartialView(chuDeDuocThaoLuanNhieu);
        }
        public ActionResult _PartialBanner()
        {
            return PartialView();
        }
        public ActionResult _PartialMotSoCD()
        {
            // Gọi LayThongTinCD() một lần
            var danhSachChuDe = db.ChuDes
                .Select(cd => new SoBaiChuDe
                {
                    ChuDe = cd,
                    SoBai = db.BaiViets
                        .Where(bv => bv.TrangThai.Contains("Đã duyệt") && bv.MaCD == cd.MaCD)
                        .Count()
                })
                .ToList(); // List<SoBaiChuDe>

            // Lấy 2 loại có tổng số bài nhiều nhất
            var top2MaLoai = danhSachChuDe
                .GroupBy(cd => cd.ChuDe.MaLoai)
                .Select(group => new
                {
                    MaLoai = group.Key,
                    SoBaiTrongLoaiCD = group.Sum(cd => cd.SoBai)
                })
                .OrderByDescending(t => t.SoBaiTrongLoaiCD)
                .Take(2)
                .Select(x => x.MaLoai)
                .ToList();

            // Lọc các chủ đề thuộc 2 loại đó
            var lstloaicdtop2 = danhSachChuDe
                .Where(cd => top2MaLoai.Contains(cd.ChuDe.MaLoai))
                .ToList();

            return PartialView(lstloaicdtop2);

        }
        public ActionResult PartialQTV()
        {
            var q = db.NguoiDungs.Where(n => n.LoaiND.TenLoai == "admin").ToList();
            return PartialView(q);
        }
        public ActionResult _PartialFooter()
        {
            return PartialView();
        }
        [HttpGet]
        [Authorize]
        public ActionResult GopY()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult GopY(GopY gopY)
        {
            var userId = Session["UserId"] as string;
            if (ModelState.IsValid && !string.IsNullOrEmpty(gopY.NoiDung))
            {
                var nd = XuLyNoiDung(gopY.NoiDung);
                if (!string.IsNullOrEmpty(nd))
                {
                    gopY.NoiDung = nd;
                    gopY.NgayGui = DateTime.Now;
                    gopY.MaND = Convert.ToInt32(userId);
                    gopY.TrangThai = false;

                    db.Gopies.Add(gopY);
                    db.SaveChanges();
                    ViewBag.ThongBao = "Đã gửi góp ý! Cảm ơn bạn đã gửi góp ý!";
                }
                else
                {
                    ViewBag.Loi = "Vui lòng nhập nội dung góp ý hợp lệ!";
                    return View();
                }
            }
            else
            {
                ViewBag.Loi = "Vui lòng điền đầy đủ thông tin!";
            }
            return View();
        }
        public static string XuLyNoiDung(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear(); // Không cho phép bất kỳ thẻ HTML nào
            sanitizer.AllowedAttributes.Clear();

            return sanitizer.Sanitize(input);
        }
        public ActionResult ThongBao(int? page)
        {
            List<ThongBao> dstb;
            var Id = Session["UserId"] as string;
            
            if (string.IsNullOrEmpty(Id))
            {
                Id = Session["AdminId"] as string;
                int maND = Convert.ToInt32(Id);
                dstb = db.ThongBaos.Where(n => n.MaND == maND || n.LoaiTB.TenLoai.Contains("tố cáo")).OrderByDescending(n => n.NgayTB).ToList();
            }
            else
            {
                int maND = Convert.ToInt32(Id);
                dstb = db.ThongBaos.Where(n => n.MaND == maND).OrderByDescending(n => n.NgayTB).ToList();
            }
            
            int iSize = 10;
            int iPageNumber = (page ?? 1);
            if (!dstb.Any())
                ViewBag.Message = "Không có thông báo nào gần đây";
            return View(dstb.ToPagedList(iPageNumber, iSize));
        }
        public ActionResult MarkAsRead(int id)
        {
            var tb = db.ThongBaos.Find(id);
            if (tb != null)
            {
                tb.TrangThai = true;
                db.SaveChanges();
            }

            if (tb.LoaiTB.TenLoai.Contains("từ chối bài viết"))
            {
                return RedirectToAction("ChinhSuaBV", "BaiViet" , new { id = tb.MaDoiTuong });
            }
            else if (tb.LoaiTB.TenLoai.Contains("xóa bài viết"))
            {
                return RedirectToAction("XemLai", "BaiViet", new { id = tb.MaDoiTuong });
            }
            else if (tb.LoaiTB.TenLoai.Contains("duyệt bài viết") || tb.LoaiTB.TenLoai.Contains("tố cáo"))
            {
                return RedirectToAction("NDBaiViet", "BaiViet", new { id = tb.MaDoiTuong });
            }

            if (tb.LoaiTB.TenLoai.Contains("xóa bình luận"))
            {
                return RedirectToAction("XemLai","BaiViet", new { id = tb.MaDoiTuong });
            }
            else
            {
                var binhLuan = db.BinhLuans.FirstOrDefault(bv => bv.MaBL == tb.MaDoiTuong);
                TempData["BinhLuanId"] = tb.MaDoiTuong;
                return RedirectToAction("NDBaiViet", "BaiViet", new { id = binhLuan.MaBV });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaThongBao(int MaThongBao)
        {
            var tb = db.ThongBaos.Where(t => t.MaTB == MaThongBao).SingleOrDefault();
            db.ThongBaos.Remove(tb);
            db.SaveChanges();
            return RedirectToAction("ThongBao");
        }
        public ActionResult LuuLyDoToCao(int IdDoiTuong, string LyDoToCao)
        {
            // Tạo thông báo
            ThongBao thongBao = new ThongBao
            {
                NgayTB = DateTime.Now,
                MaLoaiTB = db.LoaiTBs.Where(ltb => ltb.TenLoai.Contains("tố cáo")).Select(ltb => ltb.MaLoaiTB).FirstOrDefault(),
                MaDoiTuong = IdDoiTuong,
                TrangThai = false
            };
            var dt = db.BaiViets.Where(b => b.MaBV == IdDoiTuong).Select(b => b.TieuDeBV).FirstOrDefault();
            int mabv;
            if (dt == null)
            {
                mabv = Convert.ToInt32(db.BinhLuans.Where(bl => bl.MaBL == IdDoiTuong).Select(bl => bl.MaBV).FirstOrDefault());
                dt = db.BinhLuans.Where(bl => bl.MaBL == IdDoiTuong).Select(bl => bl.BaiViet.TieuDeBV).FirstOrDefault();
                thongBao.NoiDung = $"<NoiDung>Bài viết '{dt}'có bình luận bị tố cáo vì lý do '{LyDoToCao}'</NoiDung>";
            }
            else
            {
                mabv = IdDoiTuong;
                thongBao.NoiDung = $"<NoiDung>Bài viết '{dt}' đã bị tố cáo vì lý do '{LyDoToCao}'</NoiDung>";
            }
            db.ThongBaos.Add(thongBao);
            db.SaveChanges();
            return RedirectToAction("NDBaiViet", "BaiViet", new { id = mabv });
        }
    }
}