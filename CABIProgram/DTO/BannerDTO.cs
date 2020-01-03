using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CABIProgram.DTO
{
    public class BannerDTO
    {

        /// <summary>
        /// 新建一个
        /// </summary>
       public class CreatBannerData
        {

            /// <summary>
            /// 标题
            /// </summary>
            public string Title { get; set; }
            /// <summary>
            /// 网址不填没有外链
            /// </summary>
            public string URL { get; set; }
            /// <summary>
            /// 锁定false不锁定
            /// </summary>
            public bool IsLocked { get; set; }
            /// <summary>
            /// 999默认排序 倒序
            /// </summary>
            public int Display { get; set; }
            /// <summary>
            /// 类型1首页 2产品
            /// </summary>
            public byte type { get; set; }
     





        }
        /// <summary>
        /// 删除DTO
        /// </summary>

        public  class DeleteBannerDTO
        {
            public int ID { get; set; }

        }


        /// <summary>
        /// 更新一个Banner的ID
        /// </summary>
        public class UpdateBannerData
        {

            /// <summary>
            /// 
            /// </summary>
            public int ID { get; set; }

            /// <summary>
            /// 标题
            /// </summary>
            public string Title { get; set; }
            /// <summary>
            /// 网址不填没有外链
            /// </summary>
            public string URL { get; set; }
            /// <summary>
            /// 锁定false不锁定
            /// </summary>
            public bool IsLocked { get; set; }
            /// <summary>
            /// 999默认排序 倒序
            /// </summary>
            public int Display { get; set; }
            /// <summary>
            /// 类型1首页 2产品
            /// </summary>
            public byte type { get; set; }






        }


    }
}