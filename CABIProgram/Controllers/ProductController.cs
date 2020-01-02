using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace CABIProgram.Controllers
{
    public class ProductController : ApiController
    {

        /// <summary>
        /// 添加产品至心愿栏
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
<<<<<<< Updated upstream
        //public IHttpActionResult AddToWish(int id)
=======
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


        /// <summary>
        /// 获取产品分类
        /// </summary>
        /// <returns></returns>
        [HttpGet,Route("GetProductType")]
        public IHttpActionResult GetProductType()
        {
            var types = CB.TitleType.Where(t => t.IsLocked == false).OrderByDescending(a => a.Display).Select(s => new
            {
                s.ID,
                s.Title
            });

            if (types != null)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "OK", Data = types });
            }

            return Content(HttpStatusCode.OK, new resultInfo { Code = 404, Message = "没有产品分类信息", Data = "" });
        }


        /// <summary>
        /// 通过产品分类获取该分类下的产品
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        [HttpGet,Route("GetProductsByID")]
        public IHttpActionResult GetProductsByID(int ID)
        {
            IQueryable products = null;
            if (ID == 0)
            {
                products = CB.CABIProduct.OrderByDescending(p => p.Desplay).ThenByDescending(s=>s.ID).Select(a => new
                {
                    a.ID,
                    ProductName = a.NewTitle,
                    CoverImg = a.CollectionImg
                });
            }
            else
            {
                products = CB.CABIProduct.Where(s => s.ThemeID == ID && s.IsLocked == false).OrderByDescending(p => p.ID).Select(a => new
                {
                    a.ID,
                    ProductName = a.NewTitle,
                    CoverImg = a.CollectionImg
                });
            }

            if (products != null)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "OK", Data = products });
            }

            return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "NO" });


        }

       
>>>>>>> Stashed changes

    }
}
