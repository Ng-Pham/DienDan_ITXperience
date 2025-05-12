using Ganss.Xss;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using DienDanThaoLuan.Models;
using PagedList;

namespace DienDanThaoLuan.Controllers
{
    public class AccountController : Controller
    {
        DienDanEntities db = new DienDanEntities();

        [Authorize]
        [HttpPost]
        public ActionResult KeepAlive()
        {
            Session["LastActivity"] = DateTime.Now;
            return Json(new { success = true });
        }
        //Dang Nhap && Dang Ky
        [HttpGet]
        public ActionResult Login()
        {
            // Kiểm tra nếu đã đăng nhập
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "DienDanThaoLuan");
            }

            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Login(string username, string password)
        {
            string captchaResponse = Request["g-recaptcha-response"];
            bool isCaptchaValid = await IsCaptchaValid(captchaResponse);

            if (!isCaptchaValid)
            {
                ViewBag.error = "Vui lòng xác minh bạn không phải là robot.";
                return View();
            }
            //check null tài khoản && mật khẩu
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.error = "*Không được để trống tài khoản hoặc mật khẩu!!!";
                return View();
            }
            //Lấy dữ liệu tài khoản mật khẩu
            var memberAcc = db.NguoiDungs.SingleOrDefault(m => m.TenDangNhap.ToLower() == username.ToLower());

            //Check tồn tại tài khoản
            if (memberAcc == null)
            {
                ViewBag.error = "Tài khoản không tồn tại!!";
                ViewBag.username = username;
                return View();
            }
            if (memberAcc.KhoaDenKhi != null && memberAcc.KhoaDenKhi > DateTime.Now)
            {
                ViewBag.error = $"Tài khoản bị khóa đến {memberAcc.KhoaDenKhi.Value.ToString("HH:mm:ss")}. Vui lòng thử lại sau.";
                ViewBag.username = username;
                return View();
            }
            if (memberAcc.MatKhau == null)
            {
                ViewBag.error = "Tài khoản này đã bị khóa!!";
                ViewBag.username = username;
                return View();
            }
            //Check đúng sai tài khoản mật khẩu
            if (!BCrypt.Net.BCrypt.Verify(password, memberAcc.MatKhau) || memberAcc.TenDangNhap != username)
            {
                memberAcc.SoLanDNThatBai++;
                memberAcc.LanDNThatBaiCuoi = DateTime.Now;

                if (memberAcc.SoLanDNThatBai >= 5)
                {
                    memberAcc.KhoaDenKhi = DateTime.Now.AddMinutes(5);
                    db.SaveChanges();
                    SendLockoutEmail(memberAcc.Email, memberAcc.TenDangNhap, memberAcc.KhoaDenKhi.Value);
                    ViewBag.error = "Bạn đã nhập sai 5 lần. Tài khoản bị khóa 5 phút.";
                }
                else
                {
                    ViewBag.error = $"Sai tên tài khoản hoặc mật khẩu!! Lần nhập sai {memberAcc.SoLanDNThatBai}/5. Vui lòng thử lại.";
                    db.SaveChanges();
                }
                ViewBag.username = username;
                return View();
            }
            //Đăng nhập thành công
            memberAcc.SoLanDNThatBai = 0;
            memberAcc.KhoaDenKhi = null;
            db.SaveChanges();
            FormsAuthentication.SetAuthCookie(username, false);
            if (memberAcc.LoaiND.TenLoai == "admin")
            {
                Session["AdminId"] = memberAcc.MaND.ToString();
                Session["Role"] = "Admin"; // lưu quyền
                Log.Information("AdminId {Username} đã đăng nhập thành công", username);
                return RedirectToAction("Index", "Home");
            }
            Session["UserId"] = memberAcc.MaND.ToString();
            Session["Role"] = "Member"; // lưu quyền
            Log.Information("UserId {Username} đã đăng nhập thành công", username);
            return RedirectToAction("Index", "Home");
        }
        private void SendLockoutEmail(string toEmail, string username, DateTime lockoutUntil)
        {
            var fromAddress = new MailAddress("tnn231223@gmail.com", "Diễn Đàn Thảo Luận Cảnh Báo");
            var toAddress = new MailAddress(toEmail);
            const string fromPassword = "njic xpiv pzwm dugg"; // dùng app password, không dùng password thật
            string subject = "Thông báo: Tài khoản bị khóa tạm thời";
            string body = $@"
            Xin chào {username},

            Tài khoản của bạn đã bị khóa tạm thời do nhập sai mật khẩu quá 5 lần.
            Vui lòng thử đăng nhập lại sau thời gian khóa: {lockoutUntil:HH:mm:ss dd/MM/yyyy}.

            Nếu bạn không thực hiện các lần đăng nhập này, vui lòng liên hệ quản trị viên ngay lập tức.

            Trân trọng,
            Bộ phận hỗ trợ";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com", // hoặc mail server khác
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }
        private bool IsPasswordStrongEnough(string password)
        {
            // Kiểm tra mật khẩu (ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt)
            bool lengthOK = password.Length >= 8;
            bool hasLower = password.Any(c => char.IsLower(c));
            bool hasUpper = password.Any(c => char.IsUpper(c));
            bool hasNumber = password.Any(c => char.IsDigit(c));
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            return lengthOK && hasLower && hasUpper && hasNumber && hasSpecial;
        }
        private async Task<bool> IsCaptchaValid(string response)
        {
            var secretKey = "6LdIEiQrAAAAAAbw05kiLJf_xeo-CQntWAKRCg17";
            using (var httpClient = new HttpClient())
            {
                var parameters = new Dictionary<string, string>
                {
                    { "secret", secretKey },
                    { "response", response }
                };

                var encoded = new FormUrlEncodedContent(parameters);
                var result = await httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", encoded);
                var jsonResult = await result.Content.ReadAsStringAsync();

                dynamic jsonData = JsonConvert.DeserializeObject(jsonResult);
                return jsonData.success == true;
            }
        }
        //---Hoàn thành chức năng đăng nhập
        //Chức năng Đăng xuất 
        public ActionResult Logout()
        {
            var username = User.Identity.Name;
            Session["UserId"] = null;
            Session["AdminId"] = null;
            Session["Role"] = null;
            FormsAuthentication.SignOut();

            Log.Information("User {Username} đã đăng xuất thành công", username);

            return RedirectToAction("Index", "Home");
        }

        //Chức năng Đăng ký
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }
        [ValidateInput(false)]
        [HttpPost]
        public ActionResult Register(NguoiDung tv)
        {
            if (ModelState.IsValid)
            {
                tv.TenDangNhap = XuLyNoiDung(tv.TenDangNhap);
                tv.Email = XuLyNoiDung(tv.Email);
                tv.MatKhau = XuLyNoiDung(tv.MatKhau);
                tv.SDT = XuLyNoiDung(tv.SDT);
                tv.HoTen = XuLyNoiDung(tv.HoTen);
                if (string.IsNullOrEmpty(tv.Email) || string.IsNullOrEmpty(tv.TenDangNhap) || string.IsNullOrEmpty(tv.MatKhau) || string.IsNullOrEmpty(tv.SDT) || string.IsNullOrEmpty(tv.HoTen))
                {

                    ViewBag.error = "Có thông tin chứa ký tự không họp lệ. Vui lòng thử lại!";
                    return View(tv);
                }
                else
                {
                    if (!IsPasswordStrongEnough(tv.MatKhau))
                    {
                        ViewBag.error = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.";
                        return View();
                    }
                    try
                    {
                        
                        // Kiểm tra xem tên đăng nhập đã tồn tại chưa
                        var existingUser = db.NguoiDungs.FirstOrDefault(x => x.TenDangNhap == tv.TenDangNhap);
                        var existingEmail = db.NguoiDungs.FirstOrDefault(x => x.Email == tv.Email);
                        if (existingUser != null)
                        {
                            ViewBag.error = "Tên đăng nhập đã tồn tại!! Vui lòng thử lại";
                            ViewBag.tv.TenDangNhap = tv.TenDangNhap;
                            return View(tv);
                        }
                        else if (existingEmail != null)
                        {
                            ViewBag.error = "Email đã được sử dụng!! Vui lòng thử lại";
                            ViewBag.tv.Email = tv.Email;
                            return View(tv);
                        }
                        else if (tv.MatKhau.Length < 8)
                        {
                            ViewBag.error = "Mật khẩu phải có độ dài ít nhất 8 ký tự";
                            ViewBag.tv.TenDangNhap = tv.TenDangNhap;
                            return View(tv);
                        }
                        tv.NgayThamGia = DateTime.Now;
                        tv.AnhDaiDien = "avatar.jpg";
                        tv.MatKhau = BCrypt.Net.BCrypt.HashPassword(tv.MatKhau);
                        tv.SoLanDNThatBai = 0;
                        tv.MaLoaiND = db.LoaiNDs.Where(l => l.TenLoai == "user").Select(l => l.MaLoaiND).FirstOrDefault(); ; // Mặc định là thành viên
                        // Thêm thành viên mới vào database
                        db.NguoiDungs.Add(tv);
                        db.SaveChanges();

                        // Điều hướng đến trang thành công hoặc đăng nhập
                        return RedirectToAction("Login", "Account");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Có lỗi xảy ra, vui lòng thử lại! " + ex.Message);
                    }
                }
            }
            return View(tv);
        }//---Hoàn thành chức năng đăng ký
        public static string XuLyNoiDung(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear(); // Không cho phép bất kỳ thẻ HTML nào
            sanitizer.AllowedAttributes.Clear();

            return sanitizer.Sanitize(input);
        }
        //Chức năng quên mật khẩu---
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Vui lòng nhập email." });
            }

            // Kiểm tra email có tồn tại trong hệ thống hay không
            var user = db.NguoiDungs.SingleOrDefault(m => m.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                return Json(new { success = false, message = "Email không tồn tại trong hệ thống!" });
            }

            if (user.MatKhau == null)
            {
                return Json(new { success = false, message = "Tài khoản với email này đã bị khóa!!" });
            }
            // Kiểm tra thời gian từ lần gửi yêu cầu cuối cùng
            if (user.LastPasswordResetRequest.HasValue &&
                (DateTime.Now - user.LastPasswordResetRequest.Value).TotalSeconds < 30)
            {
                int remainingTime = 30 - (int)(DateTime.Now - user.LastPasswordResetRequest.Value).TotalSeconds;
                return Json(new { success = false, message = "Vui lòng đợi 30 giây trước khi gửi yêu cầu mới.", remainingTime = remainingTime });
            }

            // Tạo mật khẩu mới ngẫu nhiên
            var newPassword = GenerateRandomPassword();
            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(newPassword); // Cập nhật mật khẩu mới vào database
            user.LastPasswordResetRequest = DateTime.Now; // Cập nhật thời gian gửi yêu cầu cuối cùng
            db.SaveChanges();
            Log.Information("User {user.TenDangNhap} đã đổi mật khẩu thành công", user.TenDangNhap);

            // Gửi email mật khẩu mới cho người dùng
            SendPasswordResetEmail(user.Email, newPassword);

            return Json(new { success = true, message = "Mật khẩu mới đã được gửi đến email của bạn!", remainingTime = 30 });
        }//---Hoàn thành chức năng quên mật khẩu

        // Hàm tạo mật khẩu ngẫu nhiên
        private string GenerateRandomPassword()
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            for (int i = 0; i < 8; i++)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }
        // Hàm gửi email chứa mật khẩu mới
        private void SendPasswordResetEmail(string toEmail, string newPassword)
        {
            var fromEmail = "2224802010927@student.tdmu.edu.vn"; // Email của bạn
            var fromPassword = "juak jlej ftgf ncpk"; // Mật khẩu của bạn
            var subject = "Mật khẩu mới của bạn";
            var body = $"Mật khẩu mới của bạn là: {newPassword}";

            // Cấu hình SMTP trong controller
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                EnableSsl = true
            };

            // Tạo và gửi email
            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true, // Nếu muốn gửi mail dạng HTML
            };

            mailMessage.To.Add(toEmail);

            // Gửi mail
            try
            {
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu việc gửi mail thất bại
                ViewBag.Message = $"Đã xảy ra lỗi khi gửi email: {ex.Message}";
            }
        }//--- Hoàn thành chức năng gửi mail khi quên mật khẩu

        [Authorize]
        public ActionResult BaiVietCuaToi(int? page)
        {
            // Lấy UserID từ Session
            string userId = Session["UserId"]?.ToString();
            // Lấy AdminID từ Session
            string adminId = Session["AdminId"]?.ToString();
            var baiVietCuaToi = db.BaiViets.Select(bv => new BaiVietView
            {
                BaiViet = bv,
                SoBL = db.BinhLuans.Count(bl => bl.MaBV == bv.MaBV),
                IsAdmin = db.NguoiDungs.Any(n => n.MaND == bv.MaND && n.LoaiND.TenLoai == "admin"),
            }).Where(bv => bv.BaiViet.TrangThai.Contains("Đã duyệt")).OrderByDescending(n => n.BaiViet.NgayDang).ToList();

            // Phân trang
            int iSize = 8;
            int iPageNumber = (page ?? 1);
            return View(baiVietCuaToi.ToPagedList(iPageNumber, iSize));

        }
        [Authorize]
        public ActionResult ThongTin(int? page, int id)
        {
            var thongTinTV = db.NguoiDungs.SingleOrDefault(tt => tt.MaND == id);
            var thongTinVaDSBV = new List<BaiVietView>();

            ViewBag.ThongTin = thongTinTV;
            ViewBag.IsAd = thongTinTV.LoaiND.TenLoai == "admin";
            thongTinVaDSBV = db.BaiViets.Select(bv => new BaiVietView
            {
                BaiViet = bv,
                SoBL = db.BinhLuans.Count(bl => bl.MaBV == bv.MaBV),
                CodeContent = null,
                IsAdmin = db.NguoiDungs.Any(n => n.MaND == bv.MaND && n.LoaiND.TenLoai == "admin"),
            }).Where(bv => bv.BaiViet.TrangThai.Contains("Đã duyệt")).OrderByDescending(n => n.BaiViet.NgayDang).ToList();

            ViewBag.Id = id;
            int iSize = 8;
            int iPageNumber = (page ?? 1);
            return View(thongTinVaDSBV.ToPagedList(iPageNumber, iSize));

        }
    }
}