using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
//using Aliyun.Acs.Core;
//using Aliyun.Acs.Core.Exceptions;
//using Aliyun.Acs.Core.Profile;
//using Aliyun.Acs.Dysmsapi.Model.V20170525;
using Newtonsoft.Json;

namespace BigXia_yingxiao.Models
{
    public class Result
    {
        public string code;
        public string message;
        public string data;
        public dynamic data1;

        JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
        JsonSerializerSettings settings = new JsonSerializerSettings();
        #region 成功
        /// <summary>
        /// 注册成功
        /// </summary>
        /// <returns></returns>
        public string registerSuccess()
        {
            var b = new Result { code = "100", message = "注册成功", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 验证码发送成功
        /// </summary>
        /// <returns></returns>
        public string yzmSuccess()
        {
            var b = new Result { code = "101", message = "验证码发送成功", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        ///信息发送成功成功
        /// </summary>
        /// <returns></returns>
        /// 
        public string success()
        {
            var b = new Result { code = "200", message = "信息发送成功", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 登陆成功
        /// </summary>
        /// <param name="传token"></param>
        /// <returns></returns>
        public string LoginSuccess(string tokennum)
        {
            var b = new Result { code = "201", message = "登录成功", data = tokennum };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// token验证通过，直接登陆
        /// </summary>
        /// <returns></returns>
        public string tokenSuccess()
        {
            var b = new Result { code = "202", message = " token验证通过，直接登陆", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 修改密码成功
        /// </summary>
        /// <returns></returns>
        public string Changepwd()
        {
            var b = new Result { code = "199", message = " 修改密码成功", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 信息保存成功
        /// </summary>
        /// <returns></returns>
        public string Savesuccess()
        {
            var b = new Result { code = "203", message = "信息保存成功", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 删除成功
        /// </summary>
        /// <returns></returns>
        public string Deletesuccess()
        {
            var b = new Result { code = "204", message = "删除成功", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }

        /// <summary>
        /// 修改价钱成功（修改后的价钱在data里返回）
        /// </summary>
        /// <returns></returns>
        public string Deletesuccess(string aaa)
        {
            var b = new Result { code = "205", message = "修改价钱成功", data = aaa};

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 暂无待开始活动
        /// </summary>
        /// <returns></returns>
        public string activeNull()
        {
            var b = new Result { code = "206", message = "暂无待开始活动", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 暂无进行中的活动
        /// </summary>
        /// <returns></returns>
        public string activeing()
        {
            var b = new Result { code = "207", message = "暂无进行中的活动", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 服务码生成成功
        /// </summary>
        /// <param name="num">服务码</param>
        /// <returns></returns>
        public string ServerNumber(string num )
        {
            var b = new Result { code = "208", message = "服务码生成成功", data = num };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }

        /// <summary>
        /// 已经结束的的活动
        /// </summary>
        /// <returns></returns>
        public string GameOverTime()
        {
            var b = new Result { code = "209", message = "已经结束的的活动", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 开始计时
        /// </summary>
        /// <returns></returns>
        public string TimeStarts(string starttime ,string endTime ,string DayVal)
        {
            var b = new Result { code = "210", message = "计时已经开始，data有三个数据，分别是开始时间，结束时间，租期（天），用，分隔数据", data = starttime+","+endTime+","+DayVal };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 数据库没有找到这条ID的数据
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public string noDataID()
        {
            var b = new Result { code = "211", message = "数据库没有找到这条ID的数据，ID", data = ""  };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 成功新增加了一个产品
        /// </summary>
        /// <returns></returns>
        public string AddNewPrcduct()
        {
            var b = new Result { code = "212", message = "成功新增加了一个产品", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 没有业务员
        /// </summary>
        /// <returns></returns>
        public string ServerManNull()
        {
            var b = new Result { code = "213", message = "没有业务员记录", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }


        /// <summary>
        /// 成功更新了一个产品
        /// </summary>
        /// <returns></returns>
        public string UpDatePrcduct()
        {
            var b = new Result { code = "214", message = "成功更新了一个现有产品", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 成功更新了一个业务员的个人信息
        /// </summary>
        /// <param name="gonghao"> 工号</param>
        /// <returns></returns>
        public string UpDatePrcducta( string gonghao)
        {
            var b = new Result { code = "215", message = "成功更新了一个业务员的个人信息,工号在Data里", data = gonghao };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }

       /// <summary>
       /// 更新了yyy数据，值为xxx，值存在data里
       /// </summary>
       /// <param name="miaoshu"></param>
       /// <param name="val"></param>
       /// <returns></returns>
        public string AccountUpDateData(string miaoshu ,string val)
        {
            var b = new Result { code = "216", message = "成功更新了"+miaoshu+",更新的值是："+val+"值存在data里", data = val };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }


        
        /// <summary>
        /// 返回信息
        /// </summary>
        /// <param name="message">值</param>
        /// <returns></returns>
        public string returnSussess(dynamic message)
        {
            var b = new Result { code = "217", message = "操作成功", data1 = message };

         
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            string result = JsonConvert.SerializeObject(b, settings);
            return result;
        }

      


        #endregion
        #region 错误 失败
        /// <summary>
        /// 发生错误
        /// </summary>
        /// <returns></returns>
        public string fail()
        {
            var b = new Result { code = "500", message = "错误，发生异常", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 错误，找不到这个用户的数据
        /// </summary>
        /// <returns></returns>
        public string notFindData()
        {
            var b = new Result { code = "501", message = "错误，找不到这个用户的数据", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }



        /// <summary>
        /// 删除了该用户的留言
        /// </summary>
        /// <returns></returns>
        public string deleteUserMessage()
        {
            var b = new Result { code = "502", message = "删除了该用户的留言", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// Guid生成码
        /// </summary>
        /// <returns></returns>
        public string GuidFun()
        {
            var uuidN = Guid.NewGuid().ToString("N");
            return uuidN;

        }

        /// <summary>
        /// 该手机号已经注册了
        /// </summary>
        /// <returns></returns>
        public string UserRepeat()
        {
            var b = new Result { code = "503", message = "该手机号已经注册了", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 验证码错误
        /// </summary>
        /// <returns></returns>
        public string yzmfail()
        {
            var b = new Result { code = "504 ", message = "验证码错误", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 验证码时间验证超时,3分钟之内有效
        /// </summary>
        /// <returns></returns>
        public string yzmTimeError()
        {
            var b = new Result { code = "505 ", message = "验证码时间验证超时,3分钟之内有效", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 验证码session没有生成，造成找不到session进行对比
        /// </summary>
        /// <returns></returns>
        public string yzmnull()
        {
            var b = new Result { code = "506 ", message = "验证码session或者结束时间session没有生成，造成找不到session进行对比", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 找不到该手机号
        /// </summary>
        /// <returns></returns>
        public string PhoneNull()
        {
            var b = new Result { code = "507 ", message = "找不到这个手机号，账户找不到", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 手机登录的密码错误
        /// </summary>
        /// <returns></returns>
        public string PhonepwdError()
        {
            var b = new Result { code = "508 ", message = "手机登录的密码错误", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 错误，没有从前端接收到手机号密码或者验证码
        /// </summary>
        /// <returns></returns>
        public string notFindPhone()
        {
            var b = new Result { code = "509", message = "错误，没有从前端接收到手机号密码或者验证码", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        public string notSendyzm()
        {
            var b = new Result { code = "510", message = "错误，没有找到session，没发送验证码呢", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 没有token,跳回登陆页面,用账号密码登陆
        /// </summary>
        /// <returns></returns>
        public string tokennull()
        {
            var b = new Result { code = "510", message = "没有找到匹配token，token验证失败，说明token更新了，可能其他地方已经重新登陆，需要重新登陆", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        public string tokenTimeError()
        {
            var b = new Result { code = "511", message = "token过期了重新登陆", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 从前端没有接收到数据
        /// </summary>
        /// <returns></returns>
        public string messageError()
        {
            var b = new Result { code = "512", message = "从前端没有接收到数据", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 没有找到该用户的订单
        /// </summary>
        /// <returns></returns>
        public string orderNull()
        {
            var b = new Result { code = "513", message = "没有找到该用户的订单", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 错误！找不到订单ID
        /// </summary>
        /// <returns></returns>
        public string OrderIDNull()
        {
            var b = new Result { code = "514", message = "错误！找不到订单ID", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 没有找到该用户的已删除记录
        /// </summary>
        /// <returns></returns>
        public string SerachDeleteNull()
        {
            var b = new Result { code = "515", message = "没有找到该用户的已删除记录", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }

        /// <summary>
        /// 本条数据已经更新过，不能重复开始
        /// </summary>
        /// <returns></returns>
        public string gameStartError()
        {
            var b = new Result { code = "516", message = "本条数据已经点击过开始，不能重复开始", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 您的账号已经被锁定
        /// </summary>
        /// <returns></returns>

        public string AccountLocked ()
        {
            var b = new Result { code = "517", message = "您的账号已经被锁定，如需解锁账号需要联系管理员", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 错误，销售人员查询超过10万！
        /// </summary>
        /// <returns></returns>

        public string ServersError()
        {
            var b = new Result { code = "518", message = "错误，销售人员查询超过十万！", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 逻辑错误，生成的服务号发生重复！
        /// </summary>
        /// <returns></returns>
        public string ServersCreateError()
        {
            var b = new Result { code = "519", message = "逻辑错误，生成的服务号发生重复！", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 此工号下暂无用户授权的管理账户
        /// </summary>
        /// <returns></returns>
        public string ServerAccountNull()
        {
            var b = new Result { code = "520", message = "此工号下暂无用户授权的管理账户", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 暂无数据
        /// </summary>
        /// <returns></returns>
        public string dataNll()
        {
            var b = new Result { code = "521", message = "暂无数据", data = "[]" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 服务码错误
        /// </summary>
        /// <returns></returns>
        public string servesdataNll()
        {
            var b = new Result { code = "522", message = "服务码错误，请输入正确的服务码", data = "[]" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }
        /// <summary>
        /// 不要发个没完，一分钟发一次
        /// </summary>
        /// <returns></returns>
        public string yzmrepat()
        {
            var b = new Result { code = "523", message = "干啥呀？不要发个没完，一分钟发一次", data = "" };

            var returnval = jsonSerializer.Serialize(b);

            return returnval;
        }







        #endregion
        #region 小方法

        /// <summary>
        /// SHA1加密
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public string sha1(string password)
        {
            var buffer = Encoding.UTF8.GetBytes(password);
            var data = SHA1.Create().ComputeHash(buffer);

            var sb = new StringBuilder();
            foreach (var t in data)
            {
                sb.Append(t.ToString("X2"));
            }
            string HashPwd = sb.ToString();
            return HashPwd;

        }


        /// <summary>
        /// 读取Session的值
        /// </summary>
        /// <param name="key">Session的键名</param>        
        public string GetSession(string key)
        {
            if (HttpContext.Current.Session["yzm"] == null || HttpContext.Current.Session["yzm"].ToString() == "")
            {
                return null;
            }
            else
            {
                if (key.Length == 0)
                    return string.Empty;

                return HttpContext.Current.Session[key] as string;

            }

        }
        /// <summary>
        /// 读取类型Session的值
        /// </summary>
        /// <param name="key">Session的键名</param>        
        public T GetSessionType<T>(string key)
        {

            if (key.Length == 0)
                return default(T);
            return (T)HttpContext.Current.Session[key];



        }
        /// <summary>
        /// 发送阿里短信验证码
        /// </summary>
        /// <param name="phoneNumber">要发送的电话号码</param>
        /// <param name="signName">阿里云短信服务下的的签名名称（在签名管理下）</param>
        /// <param name="templateCode">短信模板（模板CODE码，在模板管理下）</param>
        /// <param name="verifyCode">传入生成的验证码</param>
        /// <returns></returns>
        //public string SendSingleMessage(string phoneNumber, string signName, string templateCode, string verifyCode)
        //{
        //    String product = "Dysmsapi";//短信API产品名称（短信产品名固定，无需修改）
        //    String domain = "dysmsapi.aliyuncs.com";//短信API产品域名（接口地址固定，无需修改）
        //    String accessKeyId = "LTAI3ectxxBI1opd";//你的accessKeyId，参考本文档步骤2
        //    String accessKeySecret = "ckN5IGfn05emWxF63DzUqqR93ldmJo";//你的accessKeySecret，参考本文档步骤2
        //    IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", accessKeyId, accessKeySecret);
        //    DefaultProfile.AddEndpoint("cn-hangzhou", "cn-hangzhou", product, domain);
        //    IAcsClient acsClient = new DefaultAcsClient(profile);
        //    SendSmsRequest request = new SendSmsRequest();
        //    string returnInfo = "-1";
        //    try
        //    {

        //        request.PhoneNumbers = phoneNumber;
        //        request.SignName = signName;
        //        request.TemplateCode = templateCode;
        //        request.TemplateParam = "{\"code\":" + verifyCode + "}";//这里根据模板需要需要进行灵活修改
        //        SendSmsResponse sendSmsResponse = acsClient.GetAcsResponse(request);
        //        returnInfo = sendSmsResponse.Message;
        //    }
        //    catch (ServerException e)
        //    {
        //        returnInfo = e.Message;
        //    }
        //    catch (ClientException e)
        //    {
        //        returnInfo = e.Message;
        //    }
        //    return returnInfo;
        //}


        /// <summary>
        /// 生成唯一数
            /// </summary>

        private static object obj = new object();
        private static int GuidInt { get { return Guid.NewGuid().GetHashCode(); } }
        private static string GuidIntStr { get { return Math.Abs(GuidInt).ToString(); } }

        /// <summary>
                /// 生成
                /// </summary>
                /// <param name="mark">前缀</param>
                /// <param name="timeType">时间精确类型  1 日,2 时,3 分，4 秒(默认) </param>
                /// <param name="id">id 小于或等于0则随机生成id</param>
                /// <returns></returns>
        public  string Gener(string mark, int timeType = 4, int id = 0)
        {
            lock (obj)
            {
                var number = mark;
                var ticks = (DateTime.Now.Ticks - GuidInt).ToString();
                int fillCount = 0;//填充位数

                number += GetTimeStr(timeType, out fillCount);
                if (id > 0)
                {
                    number += ticks.Substring(ticks.Length - (fillCount + 3)) + id.ToString().PadLeft(10, '0');
                }
                else
                {
                    number += ticks.Substring(ticks.Length - (fillCount + 3)) + GuidIntStr.PadLeft(10, '0');
                }
                return number;
            }
        }

        /// <summary>
                /// 生成
                /// </summary>
                /// <param name="mark">前缀</param>
                /// <param name="timeType">时间精确类型  1 日,2 时,3 分，4 秒(默认)</param>
                /// <param name="id">id 小于或等于0则随机生成id</param>
                /// <returns></returns>
        public  string GenerLong(string mark, int timeType = 4, long id = 0)
        {
            lock (obj)
            {
                var number = mark;
                var ticks = (DateTime.Now.Ticks - GuidInt).ToString();
                int fillCount = 0;//填充位数

                number += GetTimeStr(timeType, out fillCount);
                if (id > 0)
                {
                    number += ticks.Substring(ticks.Length - fillCount) + id.ToString().PadLeft(19, '0');
                }
                else
                {
                    number += GuidIntStr.PadLeft(10, '0') + ticks.Substring(ticks.Length - (9 + fillCount));
                }
                return number;
            }
        }

        /// <summary>
                /// 获取时间字符串
                /// </summary>
                /// <param name="timeType">时间精确类型  1 日,2 时,3 分，4 秒(默认)</param>
                /// <param name="fillCount">填充位数</param>
                /// <returns></returns>
        private static string GetTimeStr(int timeType, out int fillCount)
        {
            var time = DateTime.Now;
            if (timeType == 1)
            {
                fillCount = 6;
                return time.ToString("yyyyMMdd");
            }
            else if (timeType == 2)
            {
                fillCount = 4;
                return time.ToString("yyyyMMddHH");
            }
            else if (timeType == 3)
            {
                fillCount = 2;
                return time.ToString("yyyyMMddHHmm");
            }
            else
            {
                fillCount = 0;
                return time.ToString("yyyyMMddHHmmss");
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">传入数据的类型</typeparam>
        /// <param name="list">把整理的</param>
        /// <returns></returns>

        public  string ToJsonString<T>(List<T> list)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            string result = JsonConvert.SerializeObject(list, settings);
            return result;
        }



      
     




        #endregion





    }
}