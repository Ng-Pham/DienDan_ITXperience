using DienDanThaoLuan.Attributes;
using DienDanThaoLuan.Models;
using Ganss.Xss;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace DienDanThaoLuan.Areas.Admin.Controllers
{
    //[SessionTimeout]
    [Authorize]
    public class QLBaiVietController : Controller
    {
        // GET: Admin/QLBaiViet
        DienDanEntities db = new DienDanEntities();
        // GET: Admin/BaiViet
        [AuthorizeRole("Admin")]
        public ActionResult Index()
        {
            return View();
        }
        private (string vanBan, string codeContent) XuLyNoiDungXML(string noiDungXml)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(noiDungXml);

            // Tìm mã code
            var codeNode = xmlDoc.SelectSingleNode("//Code");
            string codeContent = codeNode != null ? codeNode.InnerText : string.Empty;

            // Lấy nội dung văn bản, bỏ phần mã code
            var noiDungVanBanNode = xmlDoc.SelectSingleNode("//NoiDung");
            string noiDungVanBan = noiDungVanBanNode != null ? noiDungVanBanNode.InnerXml : string.Empty;
            // Loại bỏ các thẻ <Code> khỏi nội dung văn bản
            if (!string.IsNullOrEmpty(codeContent))
            {
                noiDungVanBan = noiDungVanBan.Replace(codeNode.OuterXml, string.Empty);
            }

            return (noiDungVanBan, codeContent);
        }
        [HttpGet]
        public ActionResult DuyetBai(int? page)
        {
            var dsbv = db.BaiViets.Where(bv => bv.TrangThai == "Chờ duyệt").OrderByDescending(n => n.NgayDang).ToList();
            int iSize = 10;
            int iPageNumber = (page ?? 1);
            if (!dsbv.Any())
                ViewBag.Message = "Không có bài viết chờ duyệt nào gần đây";
            return View(dsbv.ToPagedList(iPageNumber, iSize));
        }
        public ActionResult MarkAsRead(int id)
        {
            var tb = db.BaiViets.Find(id);
            if (tb != null)
            {
                tb.TrangThai = "Đã duyệt";
                db.SaveChanges();
            }
            return RedirectToAction("DuyetBai");
        }
        public ActionResult PartialThongTinTV(int id)
        {
            var tttv = db.NguoiDungs.Where(tv => tv.MaND == id).FirstOrDefault();

            return PartialView(tttv);
        }

        public ActionResult ChiTietBV(int id)
        {
            var ttbv = db.BaiViets.Where(bv => bv.MaBV == id).FirstOrDefault();
            var (noiDungVanBan, codeContent) = XuLyNoiDungXML(ttbv.NoiDung);
            ViewBag.NDVB = noiDungVanBan;
            ViewBag.Code = codeContent;
            return View(ttbv);
        }
        [ValidateInput(false)]
        public ActionResult LuuTTBai(int id, string trangthai, string lydo)
        {
            lydo = XuLyNoiDung(lydo);
            
            var baiviet = db.BaiViets.Find(id);
            if (trangthai == "tuChoi")
            {
                if (string.IsNullOrEmpty(lydo))
                {
                    TempData["Error"] = "Vui lòng nhập đúng định dạng!";
                    return RedirectToAction("ChiTietBV", new { id = id });
                }
                baiviet.TrangThai = "Từ chối";
                TempData["LyDoTuChoi"] = lydo;
                var idTV = db.BaiViets.Where(bv => bv.MaBV == id).Select(bv => bv.MaND).FirstOrDefault();
                GuiThongBao(Convert.ToInt32(idTV), id, "Thông báo từ chối bài viết");
                return RedirectToAction("DuyetBai");
            }
            baiviet.TrangThai = "Đã duyệt";
            db.SaveChanges();
            var maTV = db.BaiViets.Where(bv => bv.MaBV == id).Select(bv => bv.MaND).FirstOrDefault();
            GuiThongBao(Convert.ToInt32(maTV), id, "Thông báo duyệt bài viết");
            return RedirectToAction("DuyetBai");
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
        public void GuiThongBao(int maNguoiNhan, int maDoiTuong, string loaitb)
        {
            // Tạo thông báo
            ThongBao thongBao = new ThongBao
            {
                NgayTB = DateTime.Now,
                MaND = maNguoiNhan,
                MaLoaiTB = db.LoaiTBs.Where(ltb => ltb.TenLoai.Contains(loaitb)).Select(ltb => ltb.MaLoaiTB).FirstOrDefault(),
                MaDoiTuong = maDoiTuong,
                TrangThai = false
            };

            var tieuDeBV = db.BaiViets.Where(bv => bv.MaBV == maDoiTuong).Select(bv => bv.TieuDeBV).FirstOrDefault();
            if (loaitb.Contains("xóa bình luận"))
            {
                var lyDoXoa = TempData["lydoxoa"] as string;
                var maBL = db.BinhLuans.Where(bl => bl.MaBL == maDoiTuong).Select(bl => bl.MaBV).FirstOrDefault();
                thongBao.NoiDung = $"<NoiDung>Có bình luận của bạn ở bài viết '{tieuDeBV}' đã bị xóa vì {lyDoXoa}.</NoiDung>";
                db.SaveChanges();
            }

            if (loaitb.Contains("duyệt bài viết"))
            {
                thongBao.NoiDung = $"<NoiDung>Bài viết '{tieuDeBV}' của bạn đã được phê duyệt.</NoiDung>";
            }
            else if (loaitb.Contains("từ chối bài viết"))
            {
                var lyDoTuChoi = TempData["LyDoTuChoi"] as string;
                thongBao.NoiDung = $"<NoiDung>Bài viết '{tieuDeBV}' của bạn đã bị từ chối vì '{lyDoTuChoi}'</NoiDung>";
            }
            else if (loaitb.Contains("xóa bài viết"))
            {
                var lyDoXoaBai = TempData["lydoxoa"] as string;
                thongBao.NoiDung = $"<NoiDung>Bài viết '{tieuDeBV}' của bạn đã bị xóa vì '{lyDoXoaBai}'</NoiDung>";
            }
            // Lưu thông báo vào cơ sở dữ liệu
            db.ThongBaos.Add(thongBao);
            db.SaveChanges();
        }
        public ActionResult XoaBV_BL(int IdDoiTuong, string LyDoXoa)
        {
            TempData["lydoxoa"] = LyDoXoa;
            int? idTV;
            var bv = db.BaiViets.Find(IdDoiTuong);
            if (bv == null)
            {
                var bl = db.BinhLuans.Find(IdDoiTuong);
                bl.TrangThai = "Đã xóa";
                idTV = db.BinhLuans.Where(b => b.MaBL == IdDoiTuong).Select(b => b.MaND).FirstOrDefault();
                GuiThongBao(Convert.ToInt32(idTV), IdDoiTuong, "xóa bình luận");
                return RedirectToAction("NDBaiViet", "BaiViet", new { id = bl.MaBV, area = "" });
            }
            else
            {
                bv.TrangThai = "Đã xóa";
                idTV = db.BaiViets.Where(b => b.MaBV == IdDoiTuong).Select(b => b.MaND).FirstOrDefault();
                GuiThongBao(Convert.ToInt32(idTV), IdDoiTuong, "xóa bài viết");
            }
            db.SaveChanges();
            return RedirectToAction("BaiVietMoi", "BaiViet", new { area = "" });
        }
    }
}