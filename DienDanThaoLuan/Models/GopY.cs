//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DienDanThaoLuan.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class GopY
    {
        public int ID { get; set; }
        public string NoiDung { get; set; }
        public Nullable<System.DateTime> NgayGui { get; set; }
        public Nullable<bool> TrangThai { get; set; }
        public Nullable<int> MaND { get; set; }
    
        public virtual NguoiDung NguoiDung { get; set; }
    }
}
