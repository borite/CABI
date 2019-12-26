using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace BigXia_yingxiao.Models
{


    [Table("YX_ManagerUserAcc")]
    public class YX_ManagerUserAcc
    {

        [Key]
        [Column("Label")]
        public Int64 Label { get; set; }

        [Column("Type")]
        public Int32 Type { get; set; }


    }
}