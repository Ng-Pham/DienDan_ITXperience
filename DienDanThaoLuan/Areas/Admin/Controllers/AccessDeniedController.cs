﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DienDanThaoLuan.Areas.Admin.Controllers
{
    public class AccessDeniedController : Controller
    {
        // GET: Admin/AccessDenied
        public ActionResult Index()
        {
            return View();
        }
    }
}