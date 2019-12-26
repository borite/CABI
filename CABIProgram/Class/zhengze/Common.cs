using System;

using System.Web;

namespace ChinaAudio.Classes
{
    /// <summary>
    /// 向前端返回的数据模型
    /// </summary>

    public class resultInfo
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public dynamic Data { get; set; }
    }

    /// <summary>
    /// 所用到的正则表达式类
    /// </summary>
    public static class RegHelper
    {


        /// <summary>
        /// Email地址正则
        /// </summary>
        public const string email = @"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";

        /// <summary>
        /// 手机电话号码正则，含13、14、15、16、17、18开头
        /// </summary>
        public const string cellPhoneNum = @"^(13[0-9]|14[5|7]|15[0-9]|16[0-9]|17[0-9]|18[0-9])\d{8}$";

        /// <summary>
        /// 密码正则，6-20位字符(不能包含空格)
        /// </summary>
        public const string pwdPattern = @"^\S{6,20}$";

        /// <summary>
        /// 公司名字正则，字符只能包含中文、字母，数字，下划线，减号，逗号，点，空格
        /// </summary>
        public const string companyName = @"^[a-zA-Z0-9_-\s\.,/u4e00-/u9fa5]+$";

        /// <summary>
        /// 统一社会信用代码正则，18位
        /// </summary>
        public const string bussLicCode = @"^[^_IOZSVa-z\W]{2}\d{6}[^_IOZSVa-z\W]{10}$";


        /// <summary>
        /// 真实姓名正则，可以是中文、英文，允许输入英文符号点，允许输入空格，中文和英文不能同时出现，长度1-50个字符
        /// </summary>
        public const string reaCBame = @"^([\u4e00-\u9fa5]{1,20}|[a-zA-Z\.\s]{1,20})$";

    }

}