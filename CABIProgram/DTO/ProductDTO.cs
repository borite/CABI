﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CABIProgram.DTO
{
    public class ProductDTO
    {
        /// <summary>
        /// 产品ID
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// 分类ID
        /// </summary>
        public int ThemeID { get; set; }
        /// <summary>
        /// 产品名字
        /// </summary>
        public string NewTitle { get; set; }
        /// <summary>
        /// 产品价格
        /// </summary>
        public Nullable<decimal> Price { get; set; }

        /// <summary>
        /// 编辑器内容
        /// </summary>
        public string Contents { get; set; }

        /// <summary>
        /// 产品颜色
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// 产品尺寸
        /// </summary>
        public string SizeInfo { get; set; }

        /// <summary>
        /// 产品适用场景
        /// </summary>
        public string Scene { get; set; }
        /// <summary>
        /// 滚动图片
        /// </summary>
        public string ListImg { get; set; }

        /// <summary>
        /// 面料图片
        /// </summary>
        public string ImgList { get; set; }


    }

    /// <summary>
    /// 
    /// </summary>
    public class AddWishDTO
    {

    }


}