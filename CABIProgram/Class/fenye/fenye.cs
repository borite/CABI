using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CABIProgram.Class.fenye
{
    public class fenye
    {

        /// <summary>
        /// 获取分页数据的标识字段，通过这个标识获取对应数据
        /// </summary>
        public string targetID { get; set; }

        /// <summary>
        /// 每页显示几条
        /// </summary>
        public int pageSize { get; set; }
        /// <summary>
        /// 页码索引（第几页）
        /// </summary>
        public int pageIndex { get; set; }

    }
}