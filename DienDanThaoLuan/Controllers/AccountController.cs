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
using System.Configuration;
using Newtonsoft.Json.Linq;
using DienDanThaoLuan.Filters;

namespace DienDanThaoLuan.Controllers
{

    public class AccountController : Controller
    {
        DienDanEntities db = new DienDanEntities();
        private string clientId = ConfigurationManager.AppSettings["GoogleClientId"];
        private string clientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];
        private string redirectUri = ConfigurationManager.AppSettings["GoogleRedirectUri"];
        [SessionTimeout]
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
                return RedirectToAction("Index", "Home");
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
            if (memberAcc.TrangThai == false)
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
            var fromAddress = new MailAddress("2224802010514@student.tdmu.edu.vn", "Diễn Đàn IT Xperience Cảnh Báo");
            var toAddress = new MailAddress(toEmail);
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
                Credentials = new NetworkCredential(
                    ConfigurationManager.AppSettings["SmtpEmail"],
                    ConfigurationManager.AppSettings["SmtpAppPassword"])
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
                        tv.MaLoaiND = db.LoaiNDs.Where(l => l.TenLoai == "user").Select(l => l.MaLoaiND).FirstOrDefault(); // Mặc định là thành viên
                        tv.TrangThai = true; // Mặc định là hoạt động
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
            string token = Guid.NewGuid().ToString();
            user.ResetToken = token;
            user.TokenExpiry = DateTime.Now.AddMinutes(30); // Token hết hạn sau 1 giờ
            user.LastPasswordResetRequest = DateTime.Now; // Cập nhật thời gian gửi yêu cầu cuối cùng
            db.SaveChanges();

            var resetLink = Url.Action("ResetPassword", "Account", new { email = user.Email, token = token }, protocol: Request.Url.Scheme);

            // Gửi email mật khẩu mới cho người dùng
            SendResetEmail(user.Email, resetLink);

            return Json(new { success = true, message = "Link đặt lại mật khẩu đã được gửi tới email của bạn!", remainingTime = 30 });
        }//---Hoàn thành chức năng quên mật khẩu

        public void SendResetEmail(string toEmail, string link)
        {
            var fromAddress = new MailAddress("2224802010514@student.tdmu.edu.vn", "Diễn đàn IT Xperience");
            var toAddress = new MailAddress(toEmail);
            const string subject = "Yêu cầu đặt lại mật khẩu";
            string body = $@"<h2>Yêu cầu đặt lại mật khẩu</h2>
                           <p>Vui lòng nhấp vào liên kết sau để đặt lại mật khẩu của bạn:</p>
                           <p><a href='{link}'>Đặt lại mật khẩu</a></p>
                           <p>Liên kết này sẽ hết hạn sau 30 phút.</p>";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                                    ConfigurationManager.AppSettings["SmtpEmail"],
                                    ConfigurationManager.AppSettings["SmtpAppPassword"])
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                smtp.Send(message);
            }
        }
        [HttpGet]
        public ActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                ViewBag.Message = "Liên kết không hợp lệ.";
                return View();
            }
            var user = db.NguoiDungs.FirstOrDefault(u =>
                u.Email == email &&
                u.ResetToken == token &&
                u.TokenExpiry > DateTime.Now);
            if (user == null)
            {
                ViewBag.Message = "Liên kết không hợp lệ hoặc đã hết hạn.";
                return View();
            }
            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                // Gom lỗi thành chuỗi cách nhau bằng dấu xuống dòng hoặc dấu phẩy
                var errorMessage = string.Join(" | ", errors);

                return Json(new { success = false, message = errorMessage });
            }

            var user = db.NguoiDungs.FirstOrDefault(u =>
                u.Email == model.Email &&
                u.ResetToken == model.Token &&
                u.TokenExpiry > DateTime.Now);

            if (user != null)
            {
                if (!IsPasswordStrongEnough(model.NewPassword))
                {
                    ViewBag.Message = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.";
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt!!" });
                }
                try
                {
                    user.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                    user.ResetToken = null;
                    user.TokenExpiry = null;
                    db.SaveChanges();

                    Log.Information("Người dùng {Username} đã đặt lại mật khẩu thành công.", user.TenDangNhap);
                    return Json(new { success = true, message = "Mật khẩu đã được cập nhật thành công." });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Lỗi khi cập nhật mật khẩu cho người dùng {Email}", model.Email);
                    return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau!!" });
                }
            }
            else
            {
                return Json(new { success = false, message = "Liên kết không hợp lệ hoặc đã hết hạn." });
            }
        }
        public ActionResult LoginGoogle(bool isRegistering = false)
        {
            string state = isRegistering ? "register" : "login";

            var redirectUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
                $"client_id={clientId}" +
                $"&redirect_uri={redirectUri}" +
                $"&response_type=code" +
                $"&scope=email%20profile" +
                $"&state={state}";

            return Redirect(redirectUrl);
        }
        public async Task<ActionResult> GoogleCallback(string code, string state)
        {
            bool isRegistering = state == "register";
            if (string.IsNullOrEmpty(code))
            {
                return RedirectToAction("Login"); // hoặc trang lỗi
            }

            // Đổi code lấy access token
            using (var client = new HttpClient())
            {
                var values = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
            });

                var response = await client.PostAsync("https://oauth2.googleapis.com/token", values);
                var responseString = await response.Content.ReadAsStringAsync();

                JObject tokenObj = JObject.Parse(responseString);
                string accessToken = tokenObj.Value<string>("access_token");

                if (accessToken == null)
                {
                    if (!isRegistering)
                    {
                        return RedirectToAction("Login"); // lỗi
                    }
                    return RedirectToAction("Register"); // lỗi
                }

                // Lấy thông tin user
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var userInfoResponse = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                var userInfoString = await userInfoResponse.Content.ReadAsStringAsync();

                JObject userObj = JObject.Parse(userInfoString);
                string email = userObj.Value<string>("email");

                if (string.IsNullOrEmpty(email))
                {
                    TempData["Error"] = "Không thể lấy thông tin email từ Google.";
                    if (!isRegistering)
                    {
                        return RedirectToAction("Login"); // lỗi
                    }
                    return RedirectToAction("Register"); // lỗi
                }

                // Lấy username trước dấu @ và cắt 15 ký tự
                string username = email.Split('@')[0];
                if (username.Length > 15)
                    username = username.Substring(0, 15);

                var user = db.NguoiDungs.FirstOrDefault(u => u.TenDangNhap == username);

                if (user == null)
                {
                    if (!isRegistering)
                    {
                        TempData["Error"] = "Tài khoản chưa tồn tại. Vui lòng đăng ký trước.";
                        return RedirectToAction("Login");
                    }

                    // Chỉ tạo user mới nếu isRegistering == true
                    user = new NguoiDung
                    {
                        TenDangNhap = username,
                        Email = email,
                        NgayThamGia = DateTime.Now,
                        AnhDaiDien = "avatar.jpg",
                        SoLanDNThatBai = 0,
                        MaLoaiND = db.LoaiNDs.Where(l => l.TenLoai == "user").Select(l => l.MaLoaiND).FirstOrDefault(),
                        TrangThai = true
                    };

                    db.NguoiDungs.Add(user);
                    db.SaveChanges();
                }
                else if(user.KhoaDenKhi != null && user.KhoaDenKhi > DateTime.Now)
                {
                    TempData["Error"] = $"Tài khoản bị khóa đến {user.KhoaDenKhi.Value.ToString("HH:mm:ss")}. Vui lòng thử lại sau.";
                    return RedirectToAction("Login");
                }
                else if (user.TrangThai == false)
                {
                    TempData["Error"] = "Tài khoản này đã bị khóa!!";
                    return RedirectToAction("Login");
                }
                else if (user.LoaiND.TenLoai == "admin")
                {
                    TempData["Error"] = "Tài khoản này chỉ có thể đăng nhập qua mật khẩu tài khoản";
                    return RedirectToAction("Login");
                }
                user.SoLanDNThatBai = 0;
                user.KhoaDenKhi = null;
                db.SaveChanges();
                Session["UserId"] = user.MaND.ToString();
                Session["Role"] = "Member"; // lưu quyền
                FormsAuthentication.SetAuthCookie(user.TenDangNhap, false);
                return RedirectToAction("Index", "Home");
            }
        }


        [Authorize]
        public ActionResult BaiVietCuaToi(int? page)
        {
            // Lấy UserID từ Session
            string userId = Session["UserId"]?.ToString();
            // Lấy AdminID từ Session
            string adminId = Session["AdminId"]?.ToString();
            int maND = Convert.ToInt32(userId);
            if (maND == 0)
            {
                maND = Convert.ToInt32(adminId);
            }    
            var baiVietCuaToi = db.BaiViets.Select(bv => new BaiVietView
            {
                BaiViet = bv,
                SoBL = db.BinhLuans.Count(bl => bl.MaBV == bv.MaBV),
                IsAdmin = db.NguoiDungs.Any(n => n.MaND == bv.MaND && n.LoaiND.TenLoai == "admin"),
            }).Where(bv => bv.BaiViet.TrangThai.Contains("Đã duyệt") && bv.BaiViet.MaND == maND).OrderByDescending(n => n.BaiViet.NgayDang).ToList();

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
            }).Where(bv => bv.BaiViet.TrangThai.Contains("Đã duyệt") && bv.BaiViet.MaND == id).OrderByDescending(n => n.BaiViet.NgayDang).ToList();

            ViewBag.Id = id;
            int iSize = 8;
            int iPageNumber = (page ?? 1);
            return View(thongTinVaDSBV.ToPagedList(iPageNumber, iSize));

        }
    }
}