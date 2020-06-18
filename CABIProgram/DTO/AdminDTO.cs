using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CABIProgram.DTO
{
    public class AdminDTO
    {


        /// <summary>
        /// 更新账号DTO
        /// </summary>
        public  class UpdateAccount
        {


            /// <summary>
            /// 账户
            /// </summary>
            public string Account { get; set; }

            /// <summary>
            /// ID
            /// </summary>
            public int ID { get; set; }


        }

    }

    /// <summary>
    /// 更新密码DTO
    /// </summary>
    public class UpdatePWDDTO
    {

        /// <summary>
        /// 旧密码
        /// </summary>
        public string OldPassword { get; set; }

        /// <summary>
        /// 更改的密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        public int ID { get; set; }


    }
}