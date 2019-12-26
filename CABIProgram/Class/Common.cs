
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using Aliyun.Acs.Core.Exceptions;
using Aliyun.Acs.Core.Http;
using Aliyun.OSS;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using System.IO;

namespace ChinaAudio.Class
{
    public class Common
    {
        Code code = new Code();
        Random rd = new Random();
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
            if (HttpContext.Current.Session[key] == null || HttpContext.Current.Session[key].ToString() == "" || HttpContext.Current.Session[key].ToString().Trim() == "")
            {
                return null;
            }
            else
            {
                return HttpContext.Current.Session[key].ToString();

            }

        }
        ///// <summary>
        ///// 获取Session值
        ///// </summary>
        ///// <param name="Key">Session的键名</param>
        ///// <returns>返回对应键的值</returns>
        //public static string GetSession(string Key)
        //{
        //    if (HttpContext.Current.Session[Key] == null || HttpContext.Current.Session[Key].ToString() == "" || HttpContext.Current.Session[Key].ToString().Trim() == "")
        //    {
        //        return null;
        //    }
        //    else
        //    {
        //        return HttpContext.Current.Session[Key].ToString();

        //    }
        //}

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
        /// 写入不同类型的Session 
        /// </summary>
        /// <typeparam name="T">Session键值的类型</typeparam>
        /// <param name="key">Session的键名</param>
        /// <param name="value">Session的键值</param>
        public string WriteSessionType<T>(string key, T value)
        {
            if (key.Length == 0)
                return "0"; //没有成功就用0
            HttpContext.Current.Session[key] = value;
            return "1"; //成功状态是1
        }


        /// <summary>
        /// 生成六位随机验证码
        /// </summary>
        /// <returns></returns>
        public string yzmRandom()
        {           var randnum = rd.Next(100000, 1000000).ToString(); //六位随机数

            return randnum;
        }

        /// <summary>
        /// 生成guid
        /// </summary>
        /// <returns></returns>
        public string GuidFun()
        {

            var uuidN = Guid.NewGuid().ToString("N"); // e0a953c3ee6040eaa9fae2b667060e09
            return uuidN;
        }




        private static object obj = new object();
        private static int GuidInt { get { return Guid.NewGuid().GetHashCode(); } }
        private static string GuidIntStr { get { return Math.Abs(GuidInt).ToString(); } }

        /// <summary>
                /// 生成相对短一点的订单号
                /// </summary>
                /// <param name="mark">前缀</param>
                /// <param name="timeType">时间精确类型  1 日,2 时,3 分，4 秒(默认) </param>
                /// <param name="id">id 小于或等于0则随机生成id</param>
                /// <returns></returns>
        public string Gener(string mark, int timeType = 4, int id = 0)
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
                /// 生成长的订单号
                /// </summary>
                /// <param name="mark">前缀</param>
                /// <param name="timeType">时间精确类型  1 日,2 时,3 分，4 秒(默认)</param>
                /// <param name="id">id 小于或等于0则随机生成id</param>
                /// <returns></returns>
        public string GenerLong(string mark, int timeType = 4, long id = 0)
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

        public string ToJsonString<T>(List<T> list)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            string result = JsonConvert.SerializeObject(list, settings);
            return result;
        }

        /// <summary>
        /// 随机生成验证码
        /// </summary>
     

        /// <summary>
        /// 一个时间到现在过了多久（一天，一周，一月）
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public string DateToNow(DateTime dt)
        {
            TimeSpan span = DateTime.Now - dt;
            if (span.TotalDays > 60)
            {
                return dt.ToShortDateString();
            }
            else
            {
                if (span.TotalDays > 30)
                {
                    return
                    "1个月前";
                }
                else
                {
                    if (span.TotalDays > 14)
                    {
                        return
                        "2周前";
                    }
                    else
                    {
                        if (span.TotalDays > 7)
                        {
                            return
                            "1周前";
                        }
                        else
                        {
                            if (span.TotalDays > 1)
                            {
                                return
                                string.Format("{0}天前", (int)Math.Floor(span.TotalDays));
                            }
                            else
                            {
                                if (span.TotalHours > 1)
                                {
                                    return
                                    string.Format("{0}小时前", (int)Math.Floor(span.TotalHours));
                                }
                                else
                                {
                                    if (span.TotalMinutes > 1)
                                    {
                                        return
                                        string.Format("{0}分钟前", (int)Math.Floor(span.TotalMinutes));
                                    }
                                    else
                                    {
                                        if (span.TotalSeconds >= 1)
                                        {
                                            return
                                            string.Format("{0}秒前", (int)Math.Floor(span.TotalSeconds));
                                        }
                                        else
                                        {
                                            return
                                            "1秒前";
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 日期比较
        /// </summary>
        /// <param name="today">当前日期</param>
        /// <param name="writeDate">输入日期</param>
        /// <param name="n">比较天数</param>
        /// <returns>当前日期大（第一个参数大是false）， 写入天数大是true</returns>
      public bool CompareDate(string today, string writeDate)
        {
            DateTime Today = Convert.ToDateTime(today);
            DateTime WriteDate = Convert.ToDateTime(writeDate);
         
            if (Today >= WriteDate)
                return false;
            else
                return true;
        }

        ///aliyun-net-sdk-core  引入这个
        ///https://help.aliyun.com/document_detail/112145.html?spm=a2c4g.11186623.2.14.3b8350a42MAKsZ

        /// <summary>
        /// 发送阿里短信验证码
        /// </summary>
        /// <param name="phoneNumber">要发送的电话号码</param>
        /// <param name="signName">阿里云短信服务下的的签名名称（在签名管理下）</param>
        /// <param name="templateCode">短信模板（模板CODE码，在模板管理下）</param>
        /// <param name="verifyCode">传入生成的验证码</param>
        /// <returns></returns>
        public string SendSingleMessage(string phoneNumber, string signName, string templateCode, string verifyCode)
        {
            IClientProfile profile = DefaultProfile.GetProfile("default", "LTAI3ectxxBI1opd", "ckN5IGfn05emWxF63DzUqqR93ldmJo");
            DefaultAcsClient client = new DefaultAcsClient(profile);
            CommonRequest request = new CommonRequest();
            request.Method = MethodType.POST;
            request.Domain = "dysmsapi.aliyuncs.com";
            request.Version = "2017-05-25";
            request.Action = "SendSms";
            request.AddQueryParameters("PhoneNumbers", phoneNumber);
            request.AddQueryParameters("SignName", signName);
            request.AddQueryParameters("TemplateCode", templateCode);
            request.AddQueryParameters("TemplateParam", "{ \"code\": \""+verifyCode+"\"}"); //在旧的文档有说明，传这个是一个json格式 ，\" 转译成字符串，没有意义。
            //这个是demo编辑器  https://api.aliyun.com/new?spm=a2c4g.11186623.2.13.14d219d9TDzclq#/?product=Dysmsapi&api=SendSms&params={%22RegionId%22:%22default%22,%22PhoneNumbers%22:%2218404710871%22,%22SignName%22:%22%E4%B8%AD%E5%9B%BD%E9%9F%B3%E5%93%8D%E7%BD%91%22,%22TemplateCode%22:%22SMS_170155884%22,%22TemplateParam%22:%22{code:1234}%22}&tab=DEMO&lang=CSHARP
            CommonResponse response = client.GetCommonResponse(request);
            return Encoding.Default.GetString(response.HttpResponse.Content);
        }




      //  //aliyun.oss.sdk  nuget先下载这个
      //  /// <summary>
      //  /// 上传图片的方法
      //  /// </summary>
      //  /// <param name="jsonObj"></param>
      //  /// <returns>返回图片路径</returns>
      ////  public string UploadImg([FromBody]JObject jsonObj)
      //  public string UploadImg(dynamic jsonObj ,string Savepath)
      //  {
     

      //      var jsonStr = JsonConvert.SerializeObject(jsonObj); //先转换成json字符串
      //      var jsonParams = JsonConvert.DeserializeObject<dynamic>(jsonStr); //再转换成动态类型 ，这里是post传值出现的一些问题，里面有两个参数，所以要查找post传多个值的方法，这里选用了jobject方法
      //      string file64 = jsonParams.fileBase64; //接收两个参数，一个是fileBase64  图片转码后的内容  （这个是需要和前端统一的）
      //      string fileName = jsonParams.fileName;//这个是  fileName 用来放文件名
      //      byte[] bytes = Convert.FromBase64String(file64); //转换二进制
           
      //      var strDateTime = DateTime.Now.ToString("yyMMddhhmmssfff"); //取得当前时间字符串，用来拼接名字
      //      var strRan = Convert.ToString(new Random().Next(100, 999)); //生成三位随机数
      //      var fileExtension = Path.GetExtension(fileName); //获取名字的后缀

      //      var saveName = strDateTime + strRan + fileExtension; //名字的拼接  时间+随机数+后缀名
      //      //var savePath = "img/headimg/" + saveName; //oss存储的路径加上名字
      //      var savePath = Savepath + saveName; //oss存储的路径加上名字
      //      string filePath = "http://chinaaudio-bigxia.oss-cn-beijing.aliyuncs.com/" + savePath;
      //      try
      //      {
      //          //将文件上传到阿里云oss
      //          using (MemoryStream m = new MemoryStream(bytes))
      //          {
      //              var client = new OssClient(AliyunOSSHelper.endpoint, AliyunOSSHelper.accessKeyId, AliyunOSSHelper.accessKeySecret); //填写三个参数
      //              var result = client.PutObject(AliyunOSSHelper.bucketName, savePath, m); //填写哪个bucket ，路径是哪里，内容是什么 savepath是路径+名字，如果不指定路径就会在根目录下
                   
      //              return filePath; //返回这个图片的路径
      //          }

      //      }
      //      catch (Exception ex)
      //      {
      //          //WriteSysLog(ex.ToString(), Entity.Base_SysManage.EnumType.LogType.接口调用异常);
      //          return ex.Message;
      //      }


      //  }

        #endregion

    }
}