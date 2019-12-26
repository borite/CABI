using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BigXia_yingxiao.Models
{
    public class canshu
    {
        /// <summary>
        /// 前端传回来购买的天数的参数
        /// </summary>
        public string TimeVal { get; set; }
        /// <summary>
        /// 选择的哪一个游戏的ID
        /// </summary>
        public string GameListID { get; set; }
    }
}