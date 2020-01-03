using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CABIProgram.DTO
{
    public class BannerDTO
    {

        /// <summary>
        /// banner写入
        /// </summary>

        public class CreatBannerDTO
        {


            /// <summary>
            /// 标题
            /// </summary>
            public string Title { get; set; }
            /// <summary>
            /// 网址
            /// </summary>
            public string URL { get; set; }
            /// <summary>
            /// 锁定  false不锁定true锁定
            /// </summary>
            public bool IsLocked { get; set; }
            /// <summary>
            /// 排序，默认999 倒序排序
            /// </summary>
            public int Display { get; set; } = 999;
            /// <summary>
            /// 类型 1首页Banner 2产品banner
            /// </summary>
            public byte type { get; set; }
            // res.Img = filePath; //图片链接地址保存

        }

        /// <summary>
        /// banner文本信息更新
        /// </summary>

        public class UpdateBannerDTO
        {


            /// <summary>
            /// BannerID
            /// </summary>
            public int ID { get; set; }

            /// <summary>
            /// 标题
            /// </summary>
            public string Title { get; set; }
            /// <summary>
            /// 网址
            /// </summary>
            public string URL { get; set; }
            /// <summary>
            /// 锁定  false不锁定true锁定
            /// </summary>
            public bool IsLocked { get; set; }
            /// <summary>
            /// 排序，默认999 倒序排序
            /// </summary>
            public int Display { get; set; } = 999;
            /// <summary>
            /// 类型 1首页Banner 2产品banner
            /// </summary>
            public byte type { get; set; }
            // res.Img = filePath; //图片链接地址保存

        }
    }
}