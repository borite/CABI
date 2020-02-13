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
using Newtonsoft.Json.Linq;

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
        public IHttpActionResult GetProductInfo([FromUri]int ID,string openid)
        {
            var isInWishes = CB.Wishes.Any(s => s.ProductID == ID && s.UserOpenID == openid);

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
                ThemeID = (int)i.ThemeID,
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


        /// <summary>
        /// 获取产品分类
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("GetProductType")]
        public IHttpActionResult GetProductType()
        {
            var types = CB.TitleType.AsNoTracking().Where(t => t.IsLocked == false).OrderByDescending(a => a.Display).Select(s => new
            {
                s.ID,
                s.Title,
                //s.IsLocked,
                ////某个分类下的产品总数
                //count = s.CABIProduct.Where(a => a.ThemeID == s.ID).Count(),
                ////某个分类下的上架展示的产品数
                //isShowing = s.CABIProduct.Where(a => a.ThemeID == s.ID && a.IsLocked == false).Count()
            });

            if (types != null)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "OK", Data = types });
            }

            return Content(HttpStatusCode.OK, new resultInfo { Code = 404, Message = "没有产品分类信息", Data = "" });
        }



        /// <summary>
        /// 获取产品分类
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("GetProductTypeForBack")]
        public IHttpActionResult GetProductTypeForBack()
        {
            var types = CB.TitleType.AsNoTracking().OrderByDescending(a => a.Display).Select(s => new
            {
                s.ID,
                s.Title,
                s.IsLocked,
                //某个分类下的产品总数
                count = s.CABIProduct.Where(a => a.ThemeID == s.ID).Count(),
                //某个分类下的上架展示的产品数
                isShowing = s.CABIProduct.Where(a => a.ThemeID == s.ID && a.IsLocked == false).Count()
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
                products = CB.CABIProduct.Where(s => s.ThemeID == ID && s.IsLocked == false).OrderByDescending(p => p.Desplay).ThenByDescending(s => s.ID).Select(a => new
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


        /// <summary>
        /// 添加产品系列
        /// </summary>
        /// <param name="tName">产品系列的名字</param>
        /// <returns></returns>
        [HttpPost,Route("AddType")]
        public IHttpActionResult AddProductType(string TypeName)
        {
            bool isExits = CB.TitleType.Any(s => s.Title == TypeName);
            if (isExits)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 400, Message = "该分类名字已经存在", Data = "" });
            }
            else
            {
                TitleType tt = new TitleType();
                tt.Title = TypeName;
                tt.Display = 100;
                tt.IsLocked = false;
                CB.TitleType.Add(tt);
                CB.SaveChanges();
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "添加成功", Data = "" });
            }
        }

        /// <summary>
        /// 删除产品系列
        /// </summary>
        /// <param name="TypeID"></param>
        /// <returns></returns>
        [HttpPost,Route("DeleteType")]
        public IHttpActionResult DelProductType(int TypeID)
        {
            var cc = CB.TitleType.Where(t => t.ID == TypeID).FirstOrDefault();
            CB.TitleType.Remove(cc);
            CB.SaveChanges();
            return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "删除成功", Data = "" });
        }

        /// <summary>
        /// 上架或下架一个系列产品
        /// </summary>
        /// <param name="obj">{typeID,isUp}</param>
        /// <returns></returns>
        [HttpPost,Route("DownUpProductType")]
        public IHttpActionResult DownUpProductType([FromBody]JObject obj)
        {
            var tid = (int)obj["typeID"];

            if (obj["isUp"].ToString() == "1")
            {
                CB.Database.ExecuteSqlCommand("update TitleType set IsLocked=0 where ID=@id", new SqlParameter("@id", tid));
            }
            else
            {
                CB.Database.ExecuteSqlCommand("update TitleType set IsLocked=1 where ID=@id", new SqlParameter("@id", tid));
            }

            return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "操作成功", Data = "" });
        }


        /// <summary>
        /// 通过系列ID获取该系列下的产品
        /// </summary>
        /// <param name="pageDTO"></param>
        /// <returns></returns>
        [HttpPost,Route("ManageGetProductsByID")]
        public IHttpActionResult ManageGetProductByID(pageDTO pageDTO)
        {
            var total = CB.CABIProduct.Where(a => a.ThemeID == pageDTO.targetID).Count();
            var list = CB.CABIProduct.Where(a => a.ThemeID == pageDTO.targetID).OrderByDescending(a => a.Desplay).Select(s => new
            {
                s.ID,
                s.NewTitle,
                TypeName = s.TitleType.Title,
                s.Price,
                s.ProductClickNum,
                s.CollectionNum,
                s.OrderNum,
                s.AddTime,
                s.IsLocked
            }).Skip(pageDTO.pageSize * (pageDTO.pageIndex - 1)).Take(pageDTO.pageSize);
            return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = total.ToString(), Data = list });
        }

    }
}
