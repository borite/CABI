using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace CABI.Controllers
{   

    public class DefaultController : ApiController
    {
        [HttpGet,Route("test")]
        public IHttpActionResult test()
        {
            return Ok("哈哈");
        }
    }
}
