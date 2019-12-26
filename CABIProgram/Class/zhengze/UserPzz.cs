using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using ChinaAudio.Classes;

namespace ChinaAudio.DTOModels
{

    /// <summary>
    /// 企业用户登录Model
    /// </summary>
    public class UserPLogin
    {
        /// <summary>
        /// 电话账号
        /// </summary>
        [Required(ErrorMessage = "手机号码不能为空")]
        [RegularExpression(RegHelper.cellPhoneNum, ErrorMessage = "手机电话号码正则，含13、14、15、16、17、18开头")]
        public string Phone { get; set; }

        /// <summary>
        /// 输入密码
        /// </summary>
        [Required(ErrorMessage ="请输入密码")]
        [MinLength(6, ErrorMessage = "请输入6-20位长度的密码")]
        [MaxLength(20,ErrorMessage = "请输入6-20位长度的密码")]
        public string Password { get; set; }

        //[Required(ErrorMessage = "请输入验证码")]
        //[MaxLength(6, ErrorMessage = "请输入6位长度的验证码")]
        //public string PhoneCheckNum { get; set; }



    }




    /// <summary>
    /// 企业用户注册Model
    /// </summary>
    public class BussUserRegDTO
    {
        /// <summary>
        /// 负责人名字
        /// </summary>
        [Required(ErrorMessage ="负责人姓名不能为空")]
        [RegularExpression(RegHelper.reaCBame, ErrorMessage = "负责人真实姓名验证错误，规则：中文、英文，允许输入英文符号点，允许输入空格，中文和英文不能同时出现，长度1-50个字符")]
        public string ManagerName { get; set; }
        
        /// <summary>
        /// 负责人电话
        /// </summary>
        [Required(ErrorMessage = "负责人手机号码不能为空")]
        [RegularExpression(RegHelper.cellPhoneNum, ErrorMessage = "手机电话号码正则，含13、14、15、16、17、18开头")]
        public string ManagerPhone { get; set; }

        /// <summary>
        /// 联系人名字
        /// </summary>
        [Required(ErrorMessage = "联系人姓名不能为空")]
        [RegularExpression(RegHelper.reaCBame, ErrorMessage = "联系人真实姓名参数验证错误，规则：中文、英文，允许输入英文符号点，允许输入空格，中文和英文不能同时出现，长度1-50个字符")]
        public string ContactsName { get; set; }

        /// <summary>
        /// 联系人电话
        /// </summary>
        [Required(ErrorMessage = "联系人手机号码不能为空")]
        [RegularExpression(RegHelper.cellPhoneNum, ErrorMessage = "手机电话号码正则，含13、14、15、16、17、18开头")]
        public string ContectsPhone { get; set; }

        /// <summary>
        /// 企业登录邮箱
        /// </summary>
        [Required(ErrorMessage = "企业登录邮箱不能为空")]
        [RegularExpression(RegHelper.email,ErrorMessage ="请输入正确的电子邮箱地址")]
        public string Email { get; set; }

        /// <summary>
        /// 企业登录密码
        /// </summary>
        [Required(ErrorMessage = "企业登录密码不能为空")]
        [RegularExpression(RegHelper.pwdPattern, ErrorMessage = "登录密码规则：6-20位数字、字母、特殊符号(不能包含空格)")]
        [MinLength(6, ErrorMessage = "密码最少6个字符")]
        [MaxLength(20,ErrorMessage = "密码最多20个字符")]
        public string Password { get; set; }


        [Required(ErrorMessage ="企业营业执照不能为空")]
        public string LisenceImg { get; set; }

        [Required(ErrorMessage = "企业营业执照统一信用代码不能为空")]
        [RegularExpression(RegHelper.bussLicCode, ErrorMessage = "请输入正确的统一社会信用代码")]
        public string LisenceID { get; set; }
    }
}