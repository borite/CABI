using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace CABIProgram.DTO
{

    /// <summary>
    /// 预约信息DTO
    /// </summary>
    public class BookingOrderDTO {

        /// <summary>
        /// 用户OpenID
        /// </summary>
        public string OrderOpenID { get; set; }
        /// <summary>
        /// 用户产品ID
        /// </summary>
        public int OrderProductID { get; set; }
        /// <summary>
        /// 用户手机号码
        /// </summary>
        public string OrderPhone { get; set; }
        /// <summary>
        /// 用户真实名字
        /// </summary>
        public string OrderName { get; set; }
        /// <summary>
        /// 用户微信头像
        /// </summary>
        public string OrderHeadImg { get; set; }
        /// <summary>
        /// 用户性别
        /// </summary>
        public string OrderSex { get; set; }
        /// <summary>
        /// 用户预约时间
        /// </summary>
        public DateTime OrderTime { get; set; }
        /// <summary>
        /// 预约产品名字
        /// </summary>
        public string OrderProduct { get; set; }
        /// <summary>
        /// 预约备注
        /// </summary>
        public string Description { get; set; }

    }
}
