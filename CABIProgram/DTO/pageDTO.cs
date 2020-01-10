using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CABIProgram.DTO
{
    public class pageDTO
    {
       public int targetID{get;set;}
       public int pageSize { get; set; }
       public int pageIndex { get; set; }
    }
}