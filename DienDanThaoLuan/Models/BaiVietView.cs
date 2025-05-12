using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DienDanThaoLuan.Models
{
    public class BaiVietView
    {
        public BaiViet BaiViet { get; set; }

        public int SoBL { get; set; }
        public BinhLuan BinhLuan { get; set; }

        public string CodeContent { get; set; }
        public bool IsAdmin { get; set; }
        public string ReplyToContent { get; set; }
    }
}