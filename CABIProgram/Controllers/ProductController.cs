using CABIProgram.DTO;
using CABIProgram.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;
using ChinaAudio.Classes;
using Swashbuckle.Swagger.Annotations;

namespace CABIProgram.Controllers
{
    /// <summary>
    /// 服装产品相关API
    /// </summary>
    [RoutePrefix("api/product")]
    public class ProductController : ApiController
    {
        CABIProjectEntities CB = new CABIProjectEntities();
        /// <summary>
        /// 获取产品详情
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        [HttpGet, Route("GetProductInfo")]
        [SwaggerResponse(HttpStatusCode.OK,Type =typeof(ProductDTO))]
        public IHttpActionResult GetProductInfo([FromUri]int ID)
        {
            var pinfo = CB.CABIProduct.Where(s => s.ID == ID).Select(i => new ProductDTO
            {
                ID = i.ID,
                Color = i.Color,
                Contents = i.Contents,
                MaterialImg = i.ImgList,
                ListImg = i.ListImg,
                NewTitle = i.NewTitle, 
                Discribe=i.Discribe,
                Price = i.Price,
                Scene = i.Scene,
                SizeInfo = i.SizeInfo,
                ThemeID = i.ThemeID,
                ClothInfo = i.ClothInfo,
                DesignConcept = i.DesignConcept
            }).FirstOrDefault();

            if (pinfo!=null)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "获取成功", Data = pinfo });
            }

            return Content(HttpStatusCode.NotFound, new resultInfo { Code = 404, Message = "未找到相关记录" });

        }
    }
}
