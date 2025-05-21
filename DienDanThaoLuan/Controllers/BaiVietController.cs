using AngleSharp.Text;
using DienDanThaoLuan.Filters;
using DienDanThaoLuan.Models;
using Ganss.Xss;
using PagedList;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;

namespace DienDanThaoLuan.Controllers
{
    [SessionTimeout]
    public class BaiVietController : Controller
    {
        DienDanEntities db = new DienDanEntities();
        // GET: BaiViet
        public ActionResult BaiVietMoi(int? page)
        {
            var dsbv = db.BaiViets.Select(bv => new BaiVietView
            {
                BaiViet = bv,
                SoBL = db.BinhLuans.Count(bl => bl.MaBV == bv.MaBV),
                CodeContent = null,
                IsAdmin = db.NguoiDungs.Any(n => n.MaND == bv.MaND && n.LoaiND.TenLoai == "admin"),
            }).Where(bv => bv.BaiViet.TrangThai.Contains("Đã duyệt")).OrderByDescending(n => n.BaiViet.NgayDang).ToList();
            int iSize = 8;
            int iPageNumber = (page ?? 1);
            return View(dsbv.ToPagedList(iPageNumber, iSize));
        }
        public ActionResult Loc(int? page, string sortOrder)
        {
            ViewBag.NewestSort = sortOrder == "newest" ? "newest_desc" : "newest";
            ViewBag.OldestSort = sortOrder == "oldest" ? "oldest_desc" : "oldest";
            var baiVietViewList = db.BaiViets.Select(bv => new BaiVietView
            {
                BaiViet = bv,
                SoBL = db.BinhLuans.Count(bl => bl.MaBV == bv.MaBV),
                CodeContent = null,
                IsAdmin = db.NguoiDungs.Any(n => n.MaND == bv.MaND && n.LoaiND.TenLoai == "admin"),
            }).ToList();
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
                ViewBag.Message = "Chưa có bài viết nào";
            }
            int iSize = 8;
            int iPageNumber = (page ?? 1);
            return View("BaiVietMoi",baiVietViewList.ToPagedList(iPageNumber, iSize));
        }
        public ActionResult NDBaiViet(int id)
        {
            var nd = db.BaiViets.FirstOrDefault(ndct => ndct.MaBV == id);
            ViewBag.PostTitle = nd.TieuDeBV;
            ViewBag.PostURL = Request.Url.AbsoluteUri;
            var (noiDungVanBan, codeContent) = XuLyNoiDungXML(nd.NoiDung);

            // Lưu nội dung vào ViewBag
            ViewBag.NoiDungVanBan = noiDungVanBan;
            ViewBag.CodeContent = codeContent;
            var tvViet = db.NguoiDungs.FirstOrDefault(tv => tv.MaND == nd.MaND);
            
            var chuDe = db.ChuDes
                .Select(cd => new SoBaiChuDe
                {
                    ChuDe = cd,
                    SoBai = db.BaiViets
                        .Where(bv => bv.TrangThai.Contains("Đã duyệt") && bv.MaCD == cd.MaCD)
                        .Count()
                }).Where(cd => cd.ChuDe.MaCD == nd.MaCD).SingleOrDefault();
            if (chuDe != null)
            {
                ViewBag.maloai = chuDe.ChuDe.MaLoai;
                ViewBag.tenloai = chuDe.ChuDe.LoaiCD.TenLoai;
                ViewBag.macd = chuDe.ChuDe.MaCD;
                ViewBag.tencd = chuDe.ChuDe.TenCD;
            }
            return View(nd);
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
        public ActionResult PartialBinhLuan(string id)
        {
            var userId = Session["UserId"] as string;
            var adId = Session["AdminId"] as string;
            ViewBag.MaBV = id;
            NguoiDung tk = null;
            if (userId == null && adId == null)
            {
                ViewBag.User = null;
                return PartialView();
            }
            else if (userId != null)
            {
                int maND = Convert.ToInt32(userId);
                tk = db.NguoiDungs.FirstOrDefault(tv => tv.MaND == maND);
                return PartialView(tk);
            }
            else
            {
                int maND = Convert.ToInt32(adId);
                tk = db.NguoiDungs.FirstOrDefault(tv => tv.MaND == maND);
            }
            return PartialView(tk);
        }
        public ActionResult PartialDSBL(int? page, string id)
        {
            var dsbl = LayDanhSachBinhLuan(id).OrderByDescending(bl => bl.BinhLuan.NgayGui).ToList();
            ViewData["MaBV"] = id;
            int iSize = 6;
            int iPageNumber = (page ?? 1);
            return PartialView(dsbl.ToPagedList(iPageNumber, iSize));
        }
        private List<BaiVietView> LayDanhSachBinhLuan(string maBV)
        {
            var blList = db.Database.SqlQuery<BinhLuan>(
                @"WITH RecursiveComments AS (
                    SELECT MaBL, IDCha, CAST(NoiDung AS NVARCHAR(MAX)) AS NoiDung, NgayGui, MaND, MaBV, TrangThai
                    FROM BinhLuan
                    WHERE MaBV = @maBV AND IDCha IS NULL AND TrangThai <> N'Đã xóa'

                    UNION ALL

                    SELECT bl.MaBL, bl.IDCha, CAST(bl.NoiDung AS NVARCHAR(MAX)) AS NoiDung, bl.NgayGui, bl.MaND, bl.MaBV, bl.TrangThai
                    FROM BinhLuan bl
                    INNER JOIN RecursiveComments rc ON bl.IDCha = rc.MaBL
                )

                SELECT MaBL, IDCha, NoiDung, NgayGui, MaND, MaBV, TrangThai
                FROM RecursiveComments
                WHERE TrangThai <> N'Đã xóa'
                ",
                new SqlParameter("@maBV", maBV)
            ).ToList();

            var dsbl = blList.Select(bl => {
                return new BaiVietView
                {
                    BinhLuan = new BinhLuan
                    {
                        MaBL = bl.MaBL,
                        IDCha = bl.IDCha,
                        NoiDung = bl.NoiDung,
                        NgayGui = bl.NgayGui,
                        MaND = bl.MaND,
                        MaBV = bl.MaBV,
                        TrangThai = bl.TrangThai,
                        NguoiDung = db.NguoiDungs.FirstOrDefault(nd => nd.MaND == bl.MaND)
                    },
                    ReplyToContent = db.BinhLuans.Where(r => r.MaBL == bl.IDCha).Select(r => r.NoiDung).FirstOrDefault(),
                    IsAdmin = db.NguoiDungs.Any(n => n.MaND == bl.MaND && n.LoaiND.TenLoai == "admin")
                };
            }).ToList();

            foreach (var bl in dsbl)
            {
                var (noiDungVanBan, codeContent) = XuLyNoiDungXML(bl.BinhLuan.NoiDung);
                bl.BinhLuan.NoiDung = noiDungVanBan;
                bl.CodeContent = codeContent;

                if (!string.IsNullOrEmpty(bl.ReplyToContent))
                {
                    var (noiDung, _) = XuLyNoiDungXML(bl.ReplyToContent);
                    bl.ReplyToContent = noiDung;
                }
            }

            return dsbl;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [ValidateInput(false)]
        public ActionResult ThemBL(BinhLuan bl)
        {
            var userId = Session["UserId"] as string;
            var username = User.Identity.Name;
            if (ModelState.IsValid && !string.IsNullOrEmpty(bl.NoiDung))
            {
                try
                {
                    var nd = XuLyNoiDung(bl.NoiDung, Request.Unvalidated.Form["CodeContent"]); // Store as string in database
                    if (nd != "<NoiDung></NoiDung>")
                    { 
                        bl.NgayGui = DateTime.Now;
                        bl.TrangThai = "Hiển thị";
                        if (userId != null)
                        {
                            bl.MaND = Convert.ToInt32(userId);
                        }
                        else
                        {
                            bl.MaND = Convert.ToInt32(Session["AdminId"]);
                        }
                        bl.NoiDung = nd;

                        string idChaRaw = Request.Form["IDCha"];
                        if (string.IsNullOrEmpty(idChaRaw))
                        {
                            bl.IDCha = null;
                        }
                        else
                        {
                            bl.IDCha = int.Parse(idChaRaw);
                        }
                        bl.MaBV = bl.MaBV;
                        Log.Information("User đã bình luận có mã là: {bl.MaBL}", bl.MaBL);

                        db.BinhLuans.Add(bl);
                        db.SaveChanges();
                        
                        var maNguoiNhan = db.BaiViets
                                        .Where(bv => bv.MaBV == bl.MaBV)
                                        .Select(bv => bv.MaND) // Gộp MaTV và MaQTV vào MaND nếu đã sửa DB theo hướng chung
                                        .FirstOrDefault();

                        if (maNguoiNhan != 0)
                        {
                            GuiThongBao(Convert.ToInt32(maNguoiNhan), bl.MaBL, "Thông báo bình luận");
                        }

                    }
                    else
                    {
                        TempData["Loi"] = "Vui lòng nhập đúng định dạng!";
                        return RedirectToAction("NDBaiViet", new { id = bl.MaBV });
                    }
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi trong quá trình xử lý XML hoặc lưu vào database
                    ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình lưu: " + ex.Message);
                }
            }
            else
            {
                TempData["Loi"] = "Vui lòng điền đầy đủ thông tin!";
            }
            return RedirectToAction("NDBaiViet", new { id = bl.MaBV });
        }
        public string XuLyNoiDung(string noiDung, string codeContent)
        {
            var sanitizer = new HtmlSanitizer();

            // Xóa hết tag mặc định
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();

            // Cho phép một số tag an toàn
            sanitizer.AllowedTags.Add("b");
            sanitizer.AllowedTags.Add("i");
            sanitizer.AllowedTags.Add("u");
            sanitizer.AllowedTags.Add("strong");
            sanitizer.AllowedTags.Add("em");
            sanitizer.AllowedTags.Add("br");
            sanitizer.AllowedTags.Add("p");
            sanitizer.AllowedTags.Add("ul");
            sanitizer.AllowedTags.Add("ol");
            sanitizer.AllowedTags.Add("li");
            sanitizer.AllowedTags.Add("a");
            sanitizer.AllowedTags.Add("span");
            sanitizer.AllowedTags.Add("code");
            sanitizer.AllowedTags.Add("pre");
            sanitizer.AllowedTags.Add("div");
            sanitizer.AllowedTags.Add("section");
            sanitizer.AllowedTags.Add("article");
            sanitizer.AllowedTags.Add("header");
            sanitizer.AllowedTags.Add("footer");
            sanitizer.AllowedTags.Add("aside");
            sanitizer.AllowedTags.Add("img");

            // Các thẻ inline formatting thêm
            sanitizer.AllowedTags.Add("small");
            sanitizer.AllowedTags.Add("mark");
            sanitizer.AllowedTags.Add("del");
            sanitizer.AllowedTags.Add("ins");
            sanitizer.AllowedTags.Add("sub");
            sanitizer.AllowedTags.Add("sup");

            // Các thẻ tiêu đề
            sanitizer.AllowedTags.Add("h1");
            sanitizer.AllowedTags.Add("h2");
            sanitizer.AllowedTags.Add("h3");
            sanitizer.AllowedTags.Add("h4");
            sanitizer.AllowedTags.Add("h5");
            sanitizer.AllowedTags.Add("h6");
            // Thẻ bảng cơ bản
            sanitizer.AllowedTags.Add("table");
            sanitizer.AllowedTags.Add("thead");
            sanitizer.AllowedTags.Add("tbody");
            sanitizer.AllowedTags.Add("tfoot");
            sanitizer.AllowedTags.Add("tr");
            sanitizer.AllowedTags.Add("th");
            sanitizer.AllowedTags.Add("td");

            // Các attribute thêm cho thẻ img, table
            sanitizer.AllowedAttributes.Add("alt");
            sanitizer.AllowedAttributes.Add("width");
            sanitizer.AllowedAttributes.Add("height");
            sanitizer.AllowedAttributes.Add("class");
            sanitizer.AllowedAttributes.Add("id");
            sanitizer.AllowedAttributes.Add("title");
            sanitizer.AllowedAttributes.Add("target");
            sanitizer.AllowedAttributes.Add("href");
            sanitizer.AllowedAttributes.Add("src");
            sanitizer.AllowedAttributes.Add("data-mce-src"); // cho phép thuộc tính của TinyMCE

            // Ngăn ngừa XSS qua src hoặc href
            sanitizer.PostProcessNode += (s, e) =>
            {
                var el = e.Node as AngleSharp.Html.Dom.IHtmlElement;
                if (el != null)
                {
                    // Kiểm tra src
                    if (el.HasAttribute("src"))
                    {
                        var src = el.GetAttribute("src");
                        if (!IsSafeUrl(src))
                        {
                            el.RemoveAttribute("src");
                        }
                    }

                    // Kiểm tra data-mce-src
                    if (el.HasAttribute("data-mce-src"))
                    {
                        var dataSrc = el.GetAttribute("data-mce-src");
                        if (!IsSafeUrl(dataSrc))
                        {
                            el.RemoveAttribute("data-mce-src");
                        }
                    }

                    // Kiểm tra href nếu có
                    if (el.HasAttribute("href"))
                    {
                        var href = el.GetAttribute("href");
                        if (!IsSafeUrl(href))
                        {
                            el.RemoveAttribute("href");
                        }
                    }
                }
            };


            // Sanitize phần nội dung chính (không decode trước để tránh XSS)
            string safeNoiDung = sanitizer.Sanitize(noiDung ?? string.Empty);
            safeNoiDung = safeNoiDung.Replace("&nbsp;", " ");
            // Sanitize codeContent trước khi đưa vào CDATA
            //string safeCodeContent = sanitizer.Sanitize(codeContent ?? string.Empty);

            // Decode the content from the request
            var decodedCodeContent = codeContent ?? string.Empty; // Ensure not null
            //var decodedNoiDung = HttpUtility.HtmlEncode(safeNoiDung);

            safeNoiDung = ConvertHtmlToXml(safeNoiDung);

            string xmlString;
            if (!string.IsNullOrEmpty(decodedCodeContent))
            {
                xmlString = $"<NoiDung>{safeNoiDung}<Code><![CDATA[{decodedCodeContent}]]></Code></NoiDung>";
            }
            else
            {
                xmlString = $"<NoiDung>{safeNoiDung}</NoiDung>";
            }

            // Parse the XML string
            XElement xmlContent = XElement.Parse(xmlString);
            return xmlContent.ToString(); // Return as string
        }

        // Hàm kiểm tra URL an toàn (chỉ cho phép http/https và đường dẫn tương đối)
        private bool IsSafeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                 || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                 || url.StartsWith("/")             // đường dẫn tương đối /Upload_images/...
                 || url.StartsWith("../")          // cho TinyMCE dùng đường dẫn tương đối
                 || url.StartsWith("..\\"))        // hỗ trợ thêm Windows-style (ít dùng)
                && !url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("data:", StringComparison.OrdinalIgnoreCase); // chặn base64
        }

        // Hàm chuyển HTML thành XML hợp lệ
        private string ConvertHtmlToXml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            try
            {
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                // Danh sách các thẻ tự đóng trong HTML
                var voidElements = new[] { "img", "br", "hr", "input", "meta", "link" };
                foreach (var tag in voidElements)
                {
                    var nodes = doc.DocumentNode.SelectNodes($"//{tag}");
                    if (nodes != null)
                    {
                        foreach (var node in nodes)
                        {
                            // Đảm bảo thẻ là tự đóng bằng cách thêm thuộc tính xml:space="preserve" hoặc sửa cấu trúc
                            node.InnerHtml = ""; // Xóa nội dung bên trong (nếu có)
                            node.Attributes.Remove("xml:space"); // Xóa thuộc tính không cần thiết
                            foreach (var attr in node.Attributes)
                            {
                                attr.Value = HttpUtility.HtmlEncode(HttpUtility.HtmlDecode(attr.Value));
                            }
                        }
                    }
                }

                // Thoát các ký tự đặc biệt trong nội dung
                var textNodes = doc.DocumentNode.SelectNodes("//text()");
                if (textNodes != null)
                {
                    foreach (var node in textNodes)
                    {
                        if (!node.ParentNode.Name.Equals("code", StringComparison.OrdinalIgnoreCase) &&
                            !node.ParentNode.Name.Equals("pre", StringComparison.OrdinalIgnoreCase))
                        {
                            node.InnerHtml = HttpUtility.HtmlEncode(node.InnerHtml);
                        }
                    }
                }

                // Cấu hình để đảm bảo thẻ tự đóng
                doc.OptionWriteEmptyNodes = true; // Tự động xuất <img /> thay vì <img>
                doc.OptionCheckSyntax = true; // Kiểm tra cú pháp HTML
                doc.OptionAutoCloseOnEnd = true; // Tự động đóng các thẻ mở

                return doc.DocumentNode.OuterHtml;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi chuyển HTML sang XML: {ex.Message}");
                return html; // Trả về nguyên bản nếu có lỗi
            }
        }

        [Authorize]
        [HttpGet]
        public ActionResult ThemBV()
        {
            var cd = db.ChuDes.ToList();
            ViewBag.MaCD = new SelectList(cd, "MaCD", "TenCD");
            return View();
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult ThemBV(BaiViet post)
        {
            var userId = Session["UserId"] as string;
            if (ModelState.IsValid && !string.IsNullOrEmpty(post.NoiDung))
            {
                try
                {

                    var nd = XuLyNoiDung(post.NoiDung, Request.Unvalidated.Form["CodeContent"]); // Store as string in database
                    if (nd != "<NoiDung></NoiDung>")
                    {
                        post.NgayDang = DateTime.Now;
                        if (userId != null)
                        {
                            post.TrangThai = "Chờ duyệt";
                            post.MaND = Convert.ToInt32(userId);
                        }
                        else
                        {
                            var adId = Session["AdminId"] as string;
                            post.TrangThai = "Đã duyệt";
                            post.MaND = Convert.ToInt32(adId);
                        }
                        post.NoiDung = nd;

                        db.BaiViets.Add(post);
                        db.SaveChanges();

                        TempData["SuccessMessage"] = "Bài viết đã được gửi thành công!";

                        // Quay lại trang trước đó (nếu có)
                        if (Request.UrlReferrer != null)
                        {
                            return Redirect(Request.UrlReferrer.ToString());
                        }
                        else
                        {
                            // Nếu không có trang trước, chuyển hướng đến Index hoặc một trang mặc định
                            return RedirectToAction("Index");
                        }
                    }
                    ViewBag.Loi = "Có lỗi xảy ra trong quá trình lưu bài viết";
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi trong quá trình xử lý XML hoặc lưu vào database
                    ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình lưu bài viết: " + ex.Message);
                }
            }
            ViewBag.Loi = "Vui lòng điền đầy đủ thông tin!";

            // Nếu model không hợp lệ hoặc có lỗi, quay lại view
            var cd = db.ChuDes.ToList();
            ViewBag.MaCD = new SelectList(cd, "MaCD", "TenCD");
            return View(post);
        }
        [HttpPost]
        public JsonResult Upload(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                // Chỉ cho phép 1 số định dạng file an toàn (ví dụ: hình ảnh)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp" };
                // Kiểm tra loại MIME và phần mở rộng
                if (!allowedMimeTypes.Contains(file.ContentType))
                {
                    return Json(new { error = "Loại MIME không hợp lệ." });
                }
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { error = "Định dạng file không được phép." });
                }

                // Đổi tên file thành tên mới tránh bị XSS/đụng độ tên
                var safeFileName = Guid.NewGuid().ToString() + fileExtension;

                // Kiểm tra thêm kích thước file nếu muốn (ví dụ tối đa 5MB)
                const int maxFileSize = 5 * 1024 * 1024; // 5 MB
                if (file.ContentLength > maxFileSize)
                {
                    return Json(new { error = "Kích thước file vượt quá giới hạn cho phép." });
                }

                var path = Path.Combine(Server.MapPath("~/Upload_images/"), safeFileName);
                file.SaveAs(path);

                // Trả về đường dẫn tuyệt đối bắt đầu bằng /
                return Json(new { location = "/Upload_images/" + safeFileName });
            }
            return Json(new { error = "File upload failed." });
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

            if (loaitb.Contains("Thông báo bình luận"))
            {
                var maBaiViet = db.BinhLuans
                                  .Where(bl => bl.MaBL == maDoiTuong)
                                  .Select(bl => bl.MaBV)
                                  .FirstOrDefault();

                var baiViet = db.BaiViets.Where(bv => bv.MaBV == maBaiViet).FirstOrDefault();

                thongBao.NoiDung = $"<NoiDung>Bài viết '{baiViet.TieuDeBV}' của bạn vừa có bình luận mới.</NoiDung>";

                // Nếu có bình luận cha => gửi thêm thông báo cho người viết bình luận đó
                var idCha = db.BinhLuans
                              .Where(bl => bl.MaBL == maDoiTuong)
                              .Select(bl => bl.IDCha)
                              .FirstOrDefault();

                if (idCha.HasValue)
                {
                    var replyNguoiNhan = db.BinhLuans
                                           .Where(bl => bl.MaBL == idCha)
                                           .Select(bl => bl.MaND)
                                           .FirstOrDefault();

                    if (replyNguoiNhan != null)
                    {
                        ThongBao replyThongBao = new ThongBao
                        {
                            NgayTB = DateTime.Now,
                            MaND = replyNguoiNhan.Value,
                            MaLoaiTB = db.LoaiTBs.Where(ltb => ltb.TenLoai.Contains(loaitb)).Select(ltb => ltb.MaLoaiTB).FirstOrDefault(),
                            MaDoiTuong = maDoiTuong,
                            TrangThai = false,
                            NoiDung = $"<NoiDung>Bình luận của bạn ở bài viết '{baiViet.TieuDeBV}' đã có phản hồi mới.</NoiDung>"
                        };

                        db.ThongBaos.Add(replyThongBao);
                    }
                }
            }

            db.ThongBaos.Add(thongBao);
            db.SaveChanges();
        }
        [Authorize]
        public ActionResult ChinhSuaBV(int id)
        {
            var bv = db.BaiViets.Where(b => b.MaBV == id).SingleOrDefault();
            if (bv.TrangThai == "Từ chối")
            {
                var (noiDungVanBan, codeContent) = XuLyNoiDungXML(bv.NoiDung);
                ViewBag.NDVB = noiDungVanBan;
                ViewBag.Code = codeContent;
                return View(bv);
            }
            else
            { return RedirectToAction("ThongBao", "Home"); }

        }
        [Authorize]
        [ValidateInput(false)]
        public ActionResult CapNhapBV(BaiViet post)
        {
            var bv = db.BaiViets.Find(post.MaBV);
            if (!string.IsNullOrEmpty(post.NoiDung))
            {
                var nd = XuLyNoiDung(post.NoiDung, Request.Unvalidated.Form["CodeContent"]);
                if (nd != "<NoiDung></NoiDung>")
                {
                    bv.NoiDung = nd;
                    bv.TrangThai = "Chờ duyệt";
                    db.SaveChanges();
                }
                else
                {
                    ViewBag.Loi = "Có lỗi xảy ra trong quá trình lưu. Vui lòng nhập đúng định dạng!";
                    return View();
                }
            }
            else
            {
                ViewBag.Loi = "Vui lòng điền đầy đủ thông tin!";
                return View();
            }

            return RedirectToAction("ThongBao", "Home");
        }
        public ActionResult XemLai(int id)
        {
            var bv = db.BaiViets.Where(b => b.MaBV == id).SingleOrDefault();
            if (bv == null)
            {
                var bl = db.BinhLuans.Where(b => b.MaBL == id).SingleOrDefault();
                var baiVietLienQuan = db.BaiViets.SingleOrDefault(b => b.MaBV == bl.MaBV);
                var (noiDungVanBan, codeContent) = XuLyNoiDungXML(bl.NoiDung);
                ViewBag.NoiDung = noiDungVanBan;
                ViewBag.Code = codeContent;
                // Khởi tạo mô hình BaiViet_BinhLuan
                var model = new BaiViet_BinhLuan
                {
                    BinhLuan = bl,
                    BaiViet = baiVietLienQuan // Gán bài viết liên quan
                };
                return View(model);
            }
            else
            {
                var model = new BaiViet_BinhLuan
                {
                    BaiViet = bv
                };

                var (noiDungVanBan, codeContent) = XuLyNoiDungXML(bv.NoiDung);
                ViewBag.NoiDung = noiDungVanBan;
                ViewBag.Code = codeContent;
                return View(model);
            }
        }
        [HttpPost]
        public ActionResult XoaBai(int maBV)
        {
            var baiViet = db.BaiViets.SingleOrDefault(b => b.MaBV == maBV);
            if (baiViet != null)
            {
                db.BaiViets.Remove(baiViet);

                var tb = db.ThongBaos.Where(t => t.MaDoiTuong == maBV).ToList();
                if (tb.Any())
                {
                    db.ThongBaos.RemoveRange(tb);
                }

                db.SaveChanges();
            }

            return RedirectToAction("ThongBao", "Home");
        }
    }
}