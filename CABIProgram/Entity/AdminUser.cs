//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace CABIProgram.Entity
{
    using System;
    using System.Collections.Generic;
    
    public partial class AdminUser
    {
        public int ID { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
        public string UserRole { get; set; }
        public bool IsLocked { get; set; }
        public string Remark { get; set; }
        public string HeadImg { get; set; }
    }
}
