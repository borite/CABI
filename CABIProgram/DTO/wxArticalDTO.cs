using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CABIProgram.DTO
{
    public class wxArticalDTO
    {
        /// <summary>
        /// 获取文章类型，图片（image）、视频（video）、语音 （voice）、图文（news）
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 从全部素材的该偏移位置开始返回，0表示从第一个素材 返回
        /// </summary>
        public string offset { get; set; }
        /// <summary>
        /// 返回素材的数量，取值在1到20之间
        /// </summary>
        public string count { get; set; }
    }



    public class WxArticalToShowDTO
    {
        /// <summary>
        /// 公众号素材ID
        /// </summary>
        public string media_id { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public string update_time { get; set; }


        /// <summary>
        /// 新闻具体信息
        /// </summary>
        public WxArticalInfoDTO[] news_info { get; set; }
    }

    public class WxArticalInfoDTO
    {
        /// <summary>
        /// 公众号文章标题
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// 公众号文章作者
        /// </summary>
        public string author { get; set; }

        /// <summary>
        /// 公众号文章摘要
        /// </summary>
        public string digest { get; set; }

        /// <summary>
        /// 公众号文章链接
        /// </summary>
        public string url { get; set; }

        /// <summary>
        /// 封面图片
        /// </summary>
        public string thumb_url { get; set; }

    }

}