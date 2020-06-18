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
        /// 后台获取产品信息
        /// </summary>
        /// <param name="pid">产品ID</param>
        /// <returns></returns>
        [HttpGet, Route("GetProductInfoForBackEnd")]
        public IHttpActionResult GetProductInfoForBackEnd(int pid)
        {
            var pinfo = CB.CABIProduct.Where(s => s.ID == pid).Select(i => new
            {
                i.ID,
                i.Color,
                i.Contents,
                i.ImgList,
                i.ListImg,
                i.NewTitle,
                i.Discribe,
                i.Price,
                i.Scene,
                i.SizeInfo,
                i.ThemeID,
                i.ClothInfo,
                i.DesignConcept,
                i.CollectionImg,
                i.SubTitle
            }).FirstOrDefault();

            if (pinfo != null)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "获取成功", Data = pinfo });
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
        /// 上架或下架一个产品
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        [HttpPost,Route("DownUpProduct")]
        public IHttpActionResult DownUpProduct([FromBody]JObject obj)
        {
            var pid = (int)obj["proID"];
            if (obj["isUp"].ToString() == "1")
            {
                CB.Database.ExecuteSqlCommand("update CABIProduct set IsLocked=0 where ID=@id", new SqlParameter("@id", pid));
            }
            else
            {
                CB.Database.ExecuteSqlCommand("update CABIProduct set IsLocked=1 where ID=@id", new SqlParameter("@id", pid));
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
            if (pageDTO == null)
            {
                return Content(HttpStatusCode.BadRequest, new resultInfo { Code = 400,Message="参数错误" });
            }
           
            if (pageDTO.targetID == 0)
            {
                var total = CB.CABIProduct.AsNoTracking().Count();
                var list = CB.CABIProduct.OrderByDescending(a => a.Desplay).Select(s => new
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
            else
            {
                var total = CB.CABIProduct.AsNoTracking().Where(a => a.ThemeID == pageDTO.targetID).Count();
                var list = CB.CABIProduct.AsNoTracking().Where(a => a.ThemeID == pageDTO.targetID).OrderByDescending(a => a.Desplay).Select(s => new
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


        /// <summary>
        /// 添加产品
        /// </summary>
        /// <param name="addProductDTO"></param>
        /// <returns></returns>
        [HttpPost,Route("AddProduct")]
        public IHttpActionResult AddProduct(AddProductDTO addProductDTO)
        {
            CABIProduct res = new CABIProduct();
            res.ThemeID = addProductDTO.ThemeID; //主题ID
            res.NewTitle = addProductDTO.NewTitle; //产品标题
            res.Discribe = addProductDTO.Discribe;//描述
            res.Price = addProductDTO.Price; //价格
            res.Color = addProductDTO.Color; //颜色
            res.TopRecommend = true; //是否在推荐栏目展示
            res.SizeInfo = addProductDTO.SizeInfo; //尺码
            res.Scene = addProductDTO.Scene; //应用场景 
            res.ProductClickNum = 0;//产品点击量
            res.CollectionNum = 0; //收藏量
            res.OrderNum = 0; //预约计数
            res.ShareNum = 0; //分享计数
            res.AddTime = DateTime.Now;//添加产品时间
            res.IsLocked = true;//下架
            res.SubTitle = addProductDTO.SubTitle;
            res.DesignConcept = addProductDTO.DesignConcept;
            CB.CABIProduct.Add(res);
            CB.SaveChanges();
            //return code.returnSuccess("添加产品成功", res);
            return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "OK", Data = res.ID });
        }


        /// <summary>
        /// 修改产品信息第一步
        /// </summary>
        /// <returns></returns>
        [HttpPut, Route("UpdateProductSetpOne")]
        public IHttpActionResult UpdateProdectStep1(UpdateProductOneDTO updateProductOneDTO)
        {
            int i = CB.Database.ExecuteSqlCommand("update CABIProduct set ThemeID=@themeid, NewTitle=@newtitle,Discribe=@desc,Price=@price,Color=@color,SizeInfo=@size,DesignConcept=@dconcept,Scene=@scene,SubTitle=@subtitle where ID=@pid",
                new SqlParameter("@themeid", updateProductOneDTO.ThemeID),
                new SqlParameter("@newtitle", updateProductOneDTO.NewTitle),
                new SqlParameter("@desc", updateProductOneDTO.DesignConcept),
                new SqlParameter("@price", updateProductOneDTO.Price),
                new SqlParameter("@size", updateProductOneDTO.SizeInfo),
                new SqlParameter("@color", updateProductOneDTO.Color),
                new SqlParameter("@dconcept", updateProductOneDTO.DesignConcept),
                new SqlParameter("@scene", updateProductOneDTO.Scene),
                new SqlParameter("@subtitle", updateProductOneDTO.SubTitle),
                new SqlParameter("@pid", updateProductOneDTO.ID)
                );
            if (i > 0)
            {
                return Ok(new resultInfo { Code = 200, Message="OK" , Data = "" });
            }
            return Ok(new resultInfo { Code = 404, Message = "信息没有被修改", Data = "" });
        }


        /// <summary>
        /// 获取最新5个预约
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("GetUserBooking")]
        public IHttpActionResult GetUserBooking()
        {
            var list = CB.UserOrder.AsNoTracking().Where(n => n.OrderContact == 1).OrderByDescending(n => n.OrderID).Select(s => new
            {
                s.OrderName,
                s.OrderPhone,
                s.CABIProduct.ID,
                s.OrderProduct,
                s.SubmitTime
            }).Take(5);

            return Ok(new resultInfo { Code = 200, Message = "OK", Data = list });
        }
        


    }
}
