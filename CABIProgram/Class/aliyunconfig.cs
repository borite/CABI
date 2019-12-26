using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CABIProgram.Class
{
    public class aliyunconfig
    {
        public class AccessToken
        {

            public static string accessKeyId = "LTAI4FipDN5KEUyFXnLPF59i";
            public static string accessKeySecret = "1GGLvq4jYKtWEikhspD73d0Pd3LwXH";
         


        }
        public class OssConfig:AccessToken
        {
            public static string bucketName = "cabiproject";
            public static string endpoint = "oss-cn-huhehaote.aliyuncs.com";


        }

    }
}