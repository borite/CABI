using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CABIProgram.DTO
{
    public class AddUserDTO
    {
        /// <summary>
        /// 用户openID
        /// </summary>
        public string UserOpenID { get; set; }
        /// <summary>
        /// 用户微信头像
        /// </summary>
        public string WxHeadImg { get; set; }

        /// <summary>
        /// 用户所在城市
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// 用户所在省份
        /// </summary>
        public string Province { get; set; }

        /// <summary>
        /// 用户所在国家
        /// </summary>
        public string Counrty { get; set; }

        /// <summary>
        /// 用户性别
        /// </summary>
        public Nullable<byte> Gender { get; set; }

        /// <summary>
        /// 用户微信名字
        /// </summary>
        public string WxNickName { get; set; }

        /// <summary>
        /// 用户手机号码
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 用户真实姓名
        /// </summary>
        public string UserRealName { get; set; }
    }
}