using CABIProgram.DTO;
using CABIProgram.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;
using ChinaAudio.Classes;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Data;
using System.Data.SqlClient;

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
            var isInWishes = CB.Wishes.Any(s => s.ProductID == ID);

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
                DesignConcept = i.DesignConcept,
                IsInWishes=isInWishes
            }).FirstOrDefault();

            if (pinfo!=null)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "获取成功", Data = pinfo});
            }

            return Content(HttpStatusCode.NotFound, new resultInfo { Code = 404, Message = "未找到相关记录" });
        }


        /// <summary>
        /// 用户添加产品至心愿栏
        /// </summary>
        /// <param name="addWishDTO"></param>
        /// <returns>返回message：1-成功  2-失败</returns>
        [HttpPost,Route("AddWishes")]
        public IHttpActionResult AddWishes(AddWishDTO addWishDTO)
        {
            Wishes wishes = new Wishes();
            wishes.ProductID = addWishDTO.ProductID;
            wishes.UserOpenID = addWishDTO.UserOpenID;
            wishes.AddTime = DateTime.Now;
            CB.Wishes.Add(wishes);
            CB.Database.ExecuteSqlCommand("update CABIProduct set CollectionNum=CollectionNum+1 where ID=@id", new SqlParameter("@id", addWishDTO.ProductID));
            int i = CB.SaveChanges();
            if (i == 1)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "1", Data = "" });
            }
            return Content(HttpStatusCode.OK, new resultInfo { Code = 400, Message = "0", Data = "" });

        }
        /// <summary>
        /// 获取该商品是否已经被收藏
        /// </summary>
        /// <param name="productID">产品ID</param>
        /// <returns></returns>
        [HttpGet,Route("GetIsWishes")]
        public IHttpActionResult GetIsInWishList(int productID)
        {
            bool isIn = CB.Wishes.Any(s => s.ProductID == productID);
            if (isIn)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "1", Data = "" });
            }
            else
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "0", Data = "" });
            }
        }

       

    }
}
