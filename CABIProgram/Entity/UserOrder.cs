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
    
    public partial class UserOrder
    {
        public int OrderID { get; set; }
        public string OrderOpenID { get; set; }
        public int OrderProductID { get; set; }
        public string OrderPhone { get; set; }
        public string OrderName { get; set; }
        public string OrderHeadImg { get; set; }
        public string OrderSex { get; set; }
        public System.DateTime OrderTime { get; set; }
        public string OrderProduct { get; set; }
        public string Description { get; set; }
        public int OrderContact { get; set; }
        public System.DateTime SubmitTime { get; set; }
        public string AdminDescription { get; set; }
    
        public virtual CABIProduct CABIProduct { get; set; }
    }
}
