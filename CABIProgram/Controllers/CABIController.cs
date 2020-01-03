using Aliyun.OSS;
using CABIProgram.Class.fenye;
using CABIProgram.Entity;
using ChinaAudio.Class;
using Microsoft.AspNet.SignalR.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using WebApi.OutputCache.V2;
using static ChinaAudio.Class.Code;
using Swashbuckle.Swagger.Annotations;
using CABIProgram.DTO;
using System.Data;
using System.Data.SqlClient;
using static CABIProgram.DTO.BannerDTO;

namespace CABIProgram.Controllers
{
    [RoutePrefix("api")]
    public class CABIController : ApiController
    {
        CABIProjectEntities CB = new CABIProjectEntities();
        Code code = new Code();
        Common common = new Common();

        #region  Banner模块

        /// <summary>
        /// 用户用返回Banner
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("BannerList")]
        public IHttpActionResult BannerList([FromUri] byte type)
        {
            var cc = CB.Banner.AsNoTracking().Where(a => a.IsLocked == false&&a.type== type).OrderByDescending(a => a.Display).ThenByDescending(a=>a.ID);
            if (cc != null)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "获取成功", Data = cc });
            }
            return Content(HttpStatusCode.NotFound, new resultInfo { Code = 404, Message = "没有相应的滚动图信息", Data = cc });

        }
        /// <summary>
        /// 管理员用返回Banner（admin）
        /// </summary>

        [HttpGet, Route("AdminBannerList")]
        public string AdminBannerList([FromUri] byte type)
        {
            var cc = CB.Banner.AsNoTracking().Where(a=>a.type==type).OrderByDescending(a => a.Display).ToList();
            return code.returnSuccess(cc, "返回Banner列表，admin使用");
        }


        /// <summary>
        /// 增加一个Banner数据
        /// </summary>
        /// <param name="jsonObj"></param>
        /// <returns></returns>
        [HttpPost, Route("AddBanner")]
        public string AddBanner(CreatBannerDTO jsonObj)
        {


            Banner res = new Banner();
            res.Title = jsonObj.Title;
            // res.Img = filePath; //图片链接地址保存

            res.URL = jsonObj.URL;
            res.IsLocked = jsonObj.IsLocked;
            res.Display = jsonObj.Display;
            res.type = jsonObj.type;


            CB.Banner.Add(res);
            CB.SaveChanges();
            return code.returnSuccess("增加banner数据成功", res);





        }


        /// <summary>
        /// 增加一个banner图片(增加修改通用)
        /// </summary>
        /// <param name="Obj">前端传入 businessParam 的数组里面是 { imgs: imgurl, ID: IDval} imgurl 是二进制文件里面又存了baseURL（二进制文件） 和ext（后缀）  ID 用来传参 </param>
        /// <returns></returns>
        [HttpPost, Route("AddBannerIMG")]
        public string AddBannerIMG([FromBody]JObject Obj)
        {
            try
            {
                var ID = Convert.ToInt32(Obj["ID"]);
                var cc = CB.Banner.Where(a => a.ID == ID).FirstOrDefault();
                string jsonval = Obj["imgs"].ToString();
                var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval); //反序列化成json
                var urlListstr = cc.Img; //读取数据库中的字符串
                if (string.IsNullOrEmpty(urlListstr))
                {
                    var filePath = IMGListFun(reslist, BannerOSSHelper.ImgFirstName, BannerOSSHelper.objectPath, BannerOSSHelper.endpoint, BannerOSSHelper.accessKeyId, BannerOSSHelper.accessKeySecret, BannerOSSHelper.bucketName);
                    cc.Img = filePath;
                    CB.SaveChanges();
                    return code.returnSuccess("banner图增加成功", filePath);
                }
                else
                {

                    string[] arraystring = urlListstr.Split(','); //转化一下，先删除数据库中的图片
                    foreach (var item in arraystring) //删除oss的对应图片
                    {
                        RemoveIMGFun(BannerOSSHelper.bucketName, BannerOSSHelper.endpoint, BannerOSSHelper.accessKeyId, BannerOSSHelper.accessKeySecret, item, 49);
                        var filePath = IMGListFun(reslist, BannerOSSHelper.ImgFirstName, BannerOSSHelper.objectPath, BannerOSSHelper.endpoint, BannerOSSHelper.accessKeyId, BannerOSSHelper.accessKeySecret, BannerOSSHelper.bucketName);

                        cc.Img = filePath;
                        CB.SaveChanges();
                        return code.returnSuccess("banner图增加成功", filePath);
                    }
                }


            }
            catch (Exception ex)
            {

                return code.returnFail(ex);
            }

            return code.returnFail("内部错误");

        }


        /// <summary>
        /// 删除Banner
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("RemoveBanner")]
        public string RemoveBanner(Banner obj)
        { //查找要删除的Banner链接
            var cc = CB.Banner.Where(a => a.ID == obj.ID).FirstOrDefault();



            try
            {

                RemoveIMGFun(ProductsInfoOSSHelper.bucketName, ProductsListOSSHelper.endpoint, ProductsListOSSHelper.accessKeyId, ProductsListOSSHelper.accessKeySecret, cc.Img, 49);

                CB.Banner.Remove(cc);
                CB.SaveChanges();
                return code.returnSuccess("删除banner成功", "");

            }

            catch (Exception ex)
            {

                return code.returnFail(ex);
            }


        }

        /// <summary>
        /// 修改一个Banner数据
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPut, Route("UpdateBanner")]
        public string UpdateBanner(UpdateBannerDTO obj)
        {
            var cc = CB.Banner.Where(a => a.ID == obj.ID).FirstOrDefault();
            cc.Title = obj.Title;
            cc.URL = obj.URL;
            cc.IsLocked = obj.IsLocked;
            cc.Display = obj.Display;
            cc.Display = obj.Display;
            CB.SaveChanges();
            return code.returnSuccess("更新banner成功", cc);
        }


        #endregion

        #region 首页推荐模块（女王的新衣）

        /// <summary>
        ///前台查看推荐栏目
        /// </summary>
        /// <param name="obj">{"pageSize":"1","pageIndex":"1"} </param>
        /// <returns>message返回数据 total返回数据总数</returns>
        [HttpPost, Route("UserTopList")]
        public IHttpActionResult TopList(fenye obj)
        {

            //每页的多少
            int pageSize = obj.pageSize;
            //当前页码
            int pageIndex = obj.pageIndex;

            var total = CB.CABIProduct.Where(a => a.IsLocked == false && a.TopRecommend == true).Count();

            var list = CB.CABIProduct.Where(a => a.IsLocked == false && a.TopRecommend == true).OrderByDescending(a => a.TopDesplay).Skip(pageSize * (pageIndex - 1)).Take(pageSize).Select(s => new
            {
                s.ID,
                s.CollectionImg,
                s.SubTitle,
                s.NewTitle
            });

            if (list != null)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "记录获取成功", Data = list });
            }

            return Content(HttpStatusCode.OK, new resultInfo { Code = 404, Message = "没有找到相关记录" });

        }

        /// <summary>
        /// 后台获取所有推荐栏目信息列表
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("AdminTopList")]
        public string AdminTopList(fenye obj)
        {

            //每页的多少
            //每页的多少
            int pageSize = obj.pageSize;
            //当前页码
            int pageIndex = obj.pageIndex;

            var total = CB.CABIProduct.Where(a => a.TopRecommend == true).Count();

            var cc = CB.CABIProduct.Where(a => a.TopRecommend == true).OrderByDescending(a => a.TopDesplay).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess2(cc, total, "返回后台勾选过推荐栏目的信息");
        }

        /// <summary>
        /// 推荐栏目修改一些状态
        /// </summary>
        [HttpPost, Route("ChangeTopPrucdction")]
        public string ChangeTopPrucdction(CABIProduct obj)
        {
            var res = CB.CABIProduct.Where(a => a.ID == obj.ID).FirstOrDefault();
            res.TopRecommend = obj.TopRecommend; //是否推荐
            res.IsLocked = obj.IsLocked; //是否下架
            res.TopDesplay = obj.TopDesplay; //排序
            CB.SaveChanges();
            return code.returnSuccess("推荐状态修改成功", res);
        }
        #endregion


        #region 产品管理

        /// <summary>
        /// 增加一个产品信息（增加完信息加图片）
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("AddProduct")]
        public string AddProduct(CABIProduct obj)
        {
            var ThemeID = obj.ThemeID;
            var NewTitle = obj.NewTitle;
            var Discribe = obj.Discribe;
            var Price = obj.Price;
            var Contents = obj.Contents;
            var Color = obj.Color;
            var TopRecommend = obj.TopRecommend;
            var SizeInfo = obj.SizeInfo;
            var Scene = obj.Scene;
            //   var ListImg = obj["ListImg"].ToString();
            // var ImgListTopIndex = Convert.ToInt32(obj["ImgListTopIndex"]);
            var Desplay = obj.Desplay;
            var TopDesplay = obj.TopDesplay;
            var AllDesplay = obj.AllDesplay;
            var IsLocked = obj.IsLocked;
            var Remark = obj.Remark;


            CABIProduct res = new CABIProduct();
            res.ThemeID = ThemeID; //主题ID
            res.NewTitle = NewTitle; //产品标题
            res.Discribe = Discribe;//描述
            res.Price = Price; //价格
            res.Contents = Contents; //内容，编辑器使用

            res.Color = Color; //颜色
            res.TopRecommend = TopRecommend; //是否在推荐栏目展示
            res.SizeInfo = SizeInfo; //尺码
            res.Scene = Scene; //应用场景
                               // res.ListImg = ListImg;//ListImg; //列表页图片（列表页显示）
                               //  res.ImgList = filePathList; //内容页显示的列表页图片
                               // res.CollectionImg = "暂无";// obj.CollectionImg; //收藏图片上传
                               // res.ImgListTopIndex = ImgListTopIndex; //内容页列表图设为首页的索引
            res.ProductClickNum = 0;//产品点击量
            res.CollectionNum = 0; //收藏量
            res.OrderNum = 0; //预约计数
            res.ShareNum = 0; //分享计数
            res.AddTime = DateTime.Now;//添加产品时间
            res.Desplay = Desplay;//按分类的排序
            res.TopDesplay = TopDesplay;//推荐页面排序
            res.AllDesplay = AllDesplay;//所有页展示排序
            res.IsLocked = IsLocked;//下架
            res.Remark = Remark;//备注
            CB.CABIProduct.Add(res);
            CB.SaveChanges();
            return code.returnSuccess("添加产品成功", res);
        }


        /// <summary>
        /// 产品滚动图多图片上传(更新加上传)
        /// </summary>
        /// <param name="obj">var businessParam = { imgs: imgurl, ID: IDval };</param>
        /// <returns></returns>
        [HttpPost, Route("productList")]
        public string productslist([FromBody] JObject obj)
        {
            //var ID = Convert.ToInt32(obj["ID"]);

            //string jsonval = obj["imgs"].ToString();
            //var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval);
            ////上传多张图片的方法，返回一个,分隔的图片地址字符串
            //string urllist = IMGListFun(reslist, ProductsListOSSHelper.ImgFirstName, ProductsListOSSHelper.objectPath, ProductsListOSSHelper.endpoint, ProductsListOSSHelper.accessKeyId, ProductsListOSSHelper.accessKeySecret, ProductsListOSSHelper.bucketName);

            //var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
            //cc.ListImg = urllist; //列表页
            //CB.SaveChanges();
            //return code.returnSuccess("列表页上传成功", cc.ImgList);

            //-------------
            try
            {
                var ID = Convert.ToInt32(obj["ID"]);
                var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                string jsonval = obj["imgs"].ToString();
                var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval); //反序列化成json
                var urlListstr = cc.ListImg; //读取数据库中的字符串
                if (string.IsNullOrEmpty(urlListstr))
                {
                    var filePath = IMGListFun(reslist, ProductsListOSSHelper.ImgFirstName, ProductsListOSSHelper.objectPath, ProductsListOSSHelper.endpoint, ProductsListOSSHelper.accessKeyId, ProductsListOSSHelper.accessKeySecret, ProductsListOSSHelper.bucketName);
                    cc.ListImg = filePath;
                    CB.SaveChanges();
                    return code.returnSuccess("banner图增加成功", filePath);
                }
                else
                {

                    string[] arraystring = urlListstr.Split(','); //转化一下，先删除数据库中的图片
                    foreach (var item in arraystring) //删除oss的对应图片
                    {
                        RemoveIMGFun(ProductsListOSSHelper.bucketName, ProductsListOSSHelper.endpoint, ProductsListOSSHelper.accessKeyId, ProductsListOSSHelper.accessKeySecret, item, 49);
                        var filePath = IMGListFun(reslist, ProductsListOSSHelper.ImgFirstName, ProductsListOSSHelper.objectPath, ProductsListOSSHelper.endpoint, ProductsListOSSHelper.accessKeyId, ProductsListOSSHelper.accessKeySecret, ProductsListOSSHelper.bucketName);

                        cc.ListImg = filePath;
                        CB.SaveChanges();
                        return code.returnSuccess("列表图增加成功", filePath);
                    }
                }


            }
            catch (Exception ex)
            {

                return code.returnFail(ex);
            }

            return code.returnFail("内部错误");



        }


        ///// <summary>
        ///// 详情页多图片上传
        ///// </summary>
        ///// <param name="obj">var businessParam = { imgs: imgurl, ID: IDval };</param>
        ///// <returns></returns>
        //[HttpPost, Route("productsInfoList")]
        //public string productsInfolist([FromBody] JObject obj)
        //{
        //    var ID = Convert.ToInt32(obj["ID"]);

        //    string jsonval = obj["imgs"].ToString();
        //    var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval);
        //    //上传多张图片的方法，返回一个,分隔的图片地址字符串
        //    string urllist = IMGListFun(reslist, ProductsInfoOSSHelper.ImgFirstName, ProductsInfoOSSHelper.objectPath, ProductsInfoOSSHelper.endpoint, ProductsInfoOSSHelper.accessKeyId, ProductsInfoOSSHelper.accessKeySecret, ProductsInfoOSSHelper.bucketName);
        //    var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
        //    cc.ImgList = urllist;
        //    CB.SaveChanges();
        //    return code.returnSuccess("详情页上传成功", cc.ImgList);
        //}

        ///// <summary>
        ///// 编辑器增加oss图片数组
        ///// </summary>
        ///// <returns></returns>
        //[HttpPost, Route("ContentIMGList")]
        //public IHttpActionResult ContentIMGList([FromBody] JObject obj)
        //{

        //    var ID = Convert.ToInt32(obj["ID"]);

        //    string jsonval = obj["imgs"].ToString();
        //    var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval);
        //    //上传多张图片的方法，返回一个,分隔的图片地址字符串
        //    string urllist = IMGListFun(reslist, ProductEditIMG.ImgFirstName, ProductEditIMG.objectPath, ProductEditIMG.endpoint, ProductEditIMG.accessKeyId, ProductEditIMG.accessKeySecret, ProductEditIMG.bucketName);
        //    var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
        //    cc.ImgList = urllist;
        //    CB.SaveChanges();
        //    return Content(HttpStatusCode.OK, code.returnSuccess("成功", urllist));
        //}

        ///// <summary>
        ///// 更新编辑器图，可更新可添加，用一个即可
        ///// </summary>
        ///// <param name="obj">需要产品ID和imgs数组</param>
        ///// <returns></returns>
        //[HttpPost, Route("UpdateEdit")]
        //public IHttpActionResult UpdateEdit([FromBody] JObject obj)
        //{
        //    try
        //    {

        //        var ID = Convert.ToInt32(obj["ID"]);
        //        var search = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
        //        string jsonval = obj["imgs"].ToString();
        //        var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval); //反序列化成json

        //        var urlListstr = search.ContentIMGList; //读取数据库中的字符串
        //        if (string.IsNullOrEmpty(urlListstr)) //如果是空的，直接添加新的字符串，不删除 这是增加操作
        //        {

        //            //上传多张图片的方法，返回一个,分隔的图片地址字符串
        //            string urllist = IMGListFun(reslist, ProductEditIMG.ImgFirstName, ProductEditIMG.objectPath, ProductEditIMG.endpoint, ProductEditIMG.accessKeyId, ProductEditIMG.accessKeySecret, ProductEditIMG.bucketName);
        //            // var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
        //            //数据库赋值
        //            search.ContentIMGList = urllist; //注意这里每次记得修改，数据库对应字段赋值
        //            CB.SaveChanges();
        //            return Content(HttpStatusCode.OK, code.returnSuccess("如果是空的，相当于插入直接更新", search.ContentIMGList));


        //        }
        //        else //如果有内容删掉之前的oss文件，再写入新的oss文件
        //        {
        //            string[] arraystring = urlListstr.Split(','); //转化一下，先删除数据库中的图片
        //            foreach (var item in arraystring) //删除oss的对应图片
        //            {
        //                // var substringURL = item.Substring(49);
        //                RemoveIMGFun(ProductsInfoOSSHelper.bucketName, ProductEditIMG.endpoint, ProductEditIMG.accessKeyId, ProductEditIMG.accessKeySecret, item, 49);

        //            }
        //            //上传多张图片的方法，返回一个,分隔的图片地址字符串
        //            string urllist = IMGListFun(reslist, ProductEditIMG.ImgFirstName, ProductEditIMG.objectPath, ProductEditIMG.endpoint, ProductEditIMG.accessKeyId, ProductEditIMG.accessKeySecret, ProductEditIMG.bucketName);
        //            // var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
        //            //数据库赋值
        //            search.ContentIMGList = urllist;
        //            CB.SaveChanges();
        //            return Content(HttpStatusCode.OK, code.returnSuccess("数据更新", search.ContentIMGList));
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        //  return code.returnFail(ex);
        //        return Content(HttpStatusCode.BadRequest, code.returnFail(ex));
        //    }






        //}





        ///// <summary>
        ///// 收藏图上传
        ///// </summary>
        ///// <param name="obj">传入ID还有一个 businessParam的数组 里面的格式是{ imgs: （二进制文件）imgurl,ID: (产品ID)IDval}  </param>
        ///// <returns>data返回具体地址</returns>
        //[HttpPost, Route("AddCollection")]
        //public string AddCollection([FromBody]JObject obj)
        //{
        //    try
        //    {

        //        var ID = Convert.ToInt32(obj["ID"]);
        //        var search = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();



        //        string jsonval = obj["imgs"].ToString();
        //        var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval);
        //        //上传多张图片的方法，返回一个,分隔的图片地址字符串
        //        string urllist = IMGListFun(reslist, ProductCollectionOSSHelper.ImgFirstName, ProductCollectionOSSHelper.objectPath, ProductCollectionOSSHelper.endpoint, ProductCollectionOSSHelper.accessKeyId, ProductCollectionOSSHelper.accessKeySecret, ProductCollectionOSSHelper.bucketName);
        //        search.CollectionImg = urllist;
        //        CB.SaveChanges();
        //        return code.returnSuccess("收藏图上传成功", search.CollectionImg);

        //    }
        //    catch (Exception ex)
        //    {

        //        return code.returnFail(ex);
        //    }




        //}

        /// <summary>
        /// 更新一个详情图(更新和上传)
        /// </summary>
        /// <param name="obj">var businessParam = { imgs: imgurl, ID: IDval };</param>
        /// <returns></returns>
        [HttpPost, Route("UpdateInfoList")]
        public string UpdateInfoList([FromBody]JObject obj)
        {
            try
            {

                var ID = Convert.ToInt32(obj["ID"]);
                var search = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                string jsonval = obj["imgs"].ToString();
                var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval); //反序列化成json

                var urlListstr = search.Contents; //读取数据库中的字符串
                if (string.IsNullOrEmpty(urlListstr)) //如果是空的，直接添加新的字符串，不删除
                {

                    //上传多张图片的方法，返回一个,分隔的图片地址字符串
                    string urllist = IMGListFun(reslist, ProductsInfoOSSHelper.ImgFirstName, ProductsInfoOSSHelper.objectPath, ProductsInfoOSSHelper.endpoint, ProductsInfoOSSHelper.accessKeyId, ProductsInfoOSSHelper.accessKeySecret, ProductsInfoOSSHelper.bucketName);
                    // var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                    //数据库赋值
                    search.Contents = urllist;
                    CB.SaveChanges();
                    return code.returnSuccess("详情页更新成功", search.Contents);

                }
                else //如果有内容删掉之前的oss文件，再写入新的oss文件
                {
                    string[] arraystring = urlListstr.Split(','); //转化一下，先删除数据库中的图片
                                                                  //需要截取路径，把前面的域名截取掉

                    foreach (var item in arraystring) //删除oss的对应图片
                    {
                        //var substringURL = item.Substring(49);
                        RemoveIMGFun(ProductsInfoOSSHelper.bucketName, ProductsListOSSHelper.endpoint, ProductsListOSSHelper.accessKeyId, ProductsListOSSHelper.accessKeySecret, item, 49);

                    }
                    //上传多张图片的方法，返回一个,分隔的图片地址字符串
                    string urllist = IMGListFun(reslist, ProductsInfoOSSHelper.ImgFirstName, ProductsInfoOSSHelper.objectPath, ProductsInfoOSSHelper.endpoint, ProductsInfoOSSHelper.accessKeyId, ProductsInfoOSSHelper.accessKeySecret, ProductsInfoOSSHelper.bucketName);
                    // var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                    //数据库赋值
                    search.Contents = urllist;
                    CB.SaveChanges();
                    return code.returnSuccess("详情页更新成功", search.Contents);
                }
            }
            catch (Exception ex)
            {

                return code.returnFail(ex);
            }


        }

        /// <summary>
        /// 更新一个面料图(更新和上传通用)
        /// </summary>
        /// <param name="obj">var businessParam = { imgs: imgurl, ID: IDval };</param>
        /// <returns></returns>
        [HttpPost, Route("UpdateCloseInfoIMG")]
        public string UpdateCloseInfoIMG([FromBody]JObject obj)
        {
            try
            {

                var ID = Convert.ToInt32(obj["ID"]);
                var search = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                string jsonval = obj["imgs"].ToString();
                var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval); //反序列化成json

                var urlListstr = search.ImgList; //读取数据库中的字符串
                if (string.IsNullOrEmpty(urlListstr)) //如果是空的，直接添加新的字符串，不删除
                {
                    //上传多张图片的方法，返回一个,分隔的图片地址字符串
                    string urllist = IMGListFun(reslist, ClothInfoOSSHelper.ImgFirstName, ClothInfoOSSHelper.objectPath, ClothInfoOSSHelper.endpoint, ClothInfoOSSHelper.accessKeyId, ClothInfoOSSHelper.accessKeySecret, ClothInfoOSSHelper.bucketName);
                    // var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                    //数据库赋值
                    search.ImgList = urllist;
                    CB.SaveChanges();
                    return code.returnSuccess("详情页更新成功", search.ImgList);

                }
                else //如果有内容删掉之前的oss文件，再写入新的oss文件
                {
                    string[] arraystring = urlListstr.Split(','); //转化一下，先删除数据库中的图片
                                                                  //需要截取路径，把前面的域名截取掉

                    foreach (var item in arraystring) //删除oss的对应图片
                    {
                        //var substringURL = item.Substring(49);
                        RemoveIMGFun(ClothInfoOSSHelper.bucketName, ProductsListOSSHelper.endpoint, ProductsListOSSHelper.accessKeyId, ProductsListOSSHelper.accessKeySecret, item, 49);

                    }
                    //上传多张图片的方法，返回一个,分隔的图片地址字符串
                    string urllist = IMGListFun(reslist, ClothInfoOSSHelper.ImgFirstName, ClothInfoOSSHelper.objectPath, ClothInfoOSSHelper.endpoint, ClothInfoOSSHelper.accessKeyId, ClothInfoOSSHelper.accessKeySecret, ClothInfoOSSHelper.bucketName);
                    // var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                    //数据库赋值
                    search.ImgList = urllist;
                    CB.SaveChanges();
                    return code.returnSuccess("详情页更新成功", search.ImgList);
                }
            }
            catch (Exception ex)
            {

                return code.returnFail(ex);
            }


        }



        /// <summary>
        /// 更新一个收藏图和列表图（两个图用的一张图）
        /// </summary>
        /// <param name="obj">var businessParam = { imgs: imgurl, ID: IDval };</param>
        /// <returns></returns>
        [HttpPost, Route("UpdateCollection")]
        public string UpdateCollection([FromBody]JObject obj)
        {
            try
            {

                var ID = Convert.ToInt32(obj["ID"]);
                var search = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                string jsonval = obj["imgs"].ToString();
                var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval); //反序列化成json
                var urlListstr = search.CollectionImg; //读取数据库中的字符串
                if (string.IsNullOrEmpty(urlListstr)) //如果是空的，直接添加新的字符串，不删除
                {

                    //上传多张图片的方法，返回一个,分隔的图片地址字符串
                    string urllist = IMGListFun(reslist, ProductCollectionOSSHelper.ImgFirstName, ProductCollectionOSSHelper.objectPath, ProductCollectionOSSHelper.endpoint, ProductCollectionOSSHelper.accessKeyId, ProductCollectionOSSHelper.accessKeySecret, ProductCollectionOSSHelper.bucketName);
                    // var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                    //数据库赋值
                    search.CollectionImg = urllist; //注意这里每次记得修改，数据库对应字段赋值
                    CB.SaveChanges();
                    return code.returnSuccess("收藏图首次上传成功", search.CollectionImg);

                }
                else //如果有内容删掉之前的oss文件，再写入新的oss文件
                {
                    string[] arraystring = urlListstr.Split(','); //转化一下，先删除数据库中的图片
                    foreach (var item in arraystring) //删除oss的对应图片
                    {
                        // var substringURL = item.Substring(49);
                        RemoveIMGFun(ProductsInfoOSSHelper.bucketName, ProductCollectionOSSHelper.endpoint, ProductCollectionOSSHelper.accessKeyId, ProductCollectionOSSHelper.accessKeySecret, item, 49);

                    }
                    //上传多张图片的方法，返回一个,分隔的图片地址字符串
                    string urllist = IMGListFun(reslist, ProductCollectionOSSHelper.ImgFirstName, ProductCollectionOSSHelper.objectPath, ProductCollectionOSSHelper.endpoint, ProductCollectionOSSHelper.accessKeyId, ProductCollectionOSSHelper.accessKeySecret, ProductCollectionOSSHelper.bucketName);
                    // var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                    //数据库赋值
                    search.CollectionImg = urllist;
                    CB.SaveChanges();
                    return code.returnSuccess("收藏页更新成功", search.CollectionImg);
                }
            }
            catch (Exception ex)
            {

                return code.returnFail(ex);
            }




        }


        /// <summary>
        /// 删除产品(衣服尺码图、收藏和列表图图、详情图)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("DeleteProduct")]
        public string DeleteProduct(CABIProduct obj)
        {

            var cc = CB.CABIProduct.Where(a => a.ID == obj.ID).FirstOrDefault();
            string[] IMGlist = Array.Empty<string>(); //返回一个空数组
            List<string> AddIMGlist = IMGlist.ToList();//后面可以添加
            AddIMGlist.Add(cc.ImgList); //面料图
            AddIMGlist.Add(cc.ListImg); //详情滚动图
            AddIMGlist.Add(cc.Contents); //删除详情图
            AddIMGlist.Add(cc.CollectionImg); //收藏图+封面
         

          //  AddIMGlist.Add(cc.ListImg);
            string vals = string.Join(",", AddIMGlist); //转换成，分隔的字符串

            string[] arraystring = vals.Split(','); //转化一下，删除数据库中的图片
            foreach (var item in arraystring) //删除oss的对应图片
            {
                // var substringURL = item.Substring(49);
                RemoveIMGFun(ProductsInfoOSSHelper.bucketName, ProductCollectionOSSHelper.endpoint, ProductCollectionOSSHelper.accessKeyId, ProductCollectionOSSHelper.accessKeySecret, item, 49);

            }


            CB.CABIProduct.Remove(cc);
            CB.SaveChanges();
            return code.returnSuccess(cc, "删除产品");
        }


        /// <summary>
        /// 更新产品文字信息
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("UpdateProduct")]
        public string UpdateProduct(CABIProduct obj)
        {
            var cc = CB.CABIProduct.Where(a => a.ID == obj.ID).FirstOrDefault();
            cc.ThemeID = obj.ThemeID; //主题ID
            cc.NewTitle = obj.NewTitle; //产品标题
            cc.Discribe = obj.Discribe;//描述
            cc.Price = obj.Price; //价格
            cc.Contents = obj.Contents; //内容，编辑器使用
            cc.Color = obj.Color; //颜色
            cc.TopRecommend = obj.TopRecommend; //是否在推荐栏目展示
            cc.SizeInfo = obj.SizeInfo; //尺码
            cc.Scene = obj.Scene; //应用场景
                                  //cc.ListImg = obj.ListImg; //列表页图片（列表页显示）
                                  //cc.ImgList = obj.ImgList; //内容页显示的列表页图片
                                  //cc.CollectionImg = obj.CollectionImg; //收藏图片上传
                                  //  cc.ImgListTopIndex = obj.ImgListTopIndex; //内容页列表图设为首页的索引
            cc.Desplay = obj.Desplay;//按分类的排序
            cc.TopDesplay = obj.TopDesplay;//推荐页面排序
            cc.AllDesplay = obj.AllDesplay;//所有页展示排序
            cc.IsLocked = obj.IsLocked;//下架
            cc.Remark = obj.Remark;//备注
            CB.SaveChanges();
            return code.returnSuccess("更新产品信息成功", cc);
        }

        /// <summary>
        /// 前台按分类读取产品
        /// </summary>
        /// <param name="obj">传值：pageSize pageIndex ThemeID(主题ID) </param>
        /// <returns></returns>
        [HttpPost, Route("ReadProduct")]
        public string ReadProduct([FromBody] JObject obj)
        {
            //m每页先死几条内容
            int pageSize = Convert.ToInt16(obj["pageSize"]);
            //当前页码
            int pageIndex = Convert.ToInt16(obj["pageIndex"]);
            //主题ID
            int themeid = Convert.ToInt16(obj["ThemeID"]);


            var total = CB.CABIProduct.Where(a => a.IsLocked == false && a.ThemeID == themeid).Count();

            var list = CB.CABIProduct.Where(a => a.IsLocked == false && a.ThemeID == themeid).OrderByDescending(a => a.Desplay).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess(list, "前台返回分页数据，当前是第" + pageIndex + "页，每页显示" + pageSize + "条内容");


        }



        /// <summary>
        /// 后台按分类读取产品
        /// </summary>
        /// <param name="obj">传值：pageSize pageIndex ThemeID(主题ID)</param>
        /// <returns></returns>
        [HttpPost, Route("AdminReadProduct")]
        public string AdminReadProduct([FromBody] JObject obj)
        {
            //m每页先死几条内容
            int pageSize = Convert.ToInt16(obj["pageSize"]);
            //当前页码
            int pageIndex = Convert.ToInt16(obj["pageIndex"]);
            //主题ID
            int themeid = Convert.ToInt16(obj["ThemeID"]);


            var total = CB.CABIProduct.Where(a => a.ThemeID == themeid).Count();

            var list = CB.CABIProduct.Where(a => a.ThemeID == themeid).OrderByDescending(a => a.Desplay).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess(list, "前台返回分页数据，当前是第" + pageIndex + "页，每页显示" + pageSize + "条内容");


        }


        /// <summary>
        /// 前台不分类栏目读取产品
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("AllReadProduct")]
        public string AllReadProduct(fenye obj)
        {
            //每页显示几条内容
            int pageSize = obj.pageSize;
            //当前页码
            int pageIndex = obj.pageIndex;
            var total = CB.CABIProduct.Where(a => a.IsLocked == false).Count();

            var list = CB.CABIProduct.Where(a => a.IsLocked == false).OrderByDescending(a => a.AllDesplay).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess2(list, total, "前台不分类栏目读取产品");
        }



        /// <summary>
        /// 后台不分类栏目读取产品
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("AdminAllReadProduct")]
        public string AdminAllReadProduct(fenye obj)
        {
            //每页显示几条内容
            int pageSize = obj.pageSize;
            //当前页码
            int pageIndex = obj.pageIndex;
            var total = CB.CABIProduct.Count();

            var list = CB.CABIProduct.OrderByDescending(a => a.AllDesplay).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess2(list, total, "后台不分类栏目读取产品");
        }

        #endregion

        #region 预约管理

        /// <summary>
        /// 加一个预约
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("AddOrder")]
        public IHttpActionResult AddOrder(BookingOrderDTO bookingOrderDTO)
        {
            //  var order= CB.UserOrder
            UserOrder res = new UserOrder();
            res.OrderOpenID = bookingOrderDTO.OrderOpenID;
            res.OrderPhone = bookingOrderDTO.OrderPhone;
            res.OrderName = bookingOrderDTO.OrderName;
            res.OrderHeadImg = bookingOrderDTO.OrderHeadImg;
            res.OrderSex = bookingOrderDTO.OrderSex;
            // res.TimeQuantum = obj.TimeQuantum;
            res.OrderTime = bookingOrderDTO.OrderTime;
            res.OrderProduct = bookingOrderDTO.OrderProduct;
            res.OrderProductID = bookingOrderDTO.OrderProductID;
            res.Description = bookingOrderDTO.Description;
            res.OrderContact = 1; //预约状态（1.待试衣2.订单完成3.订单失效）
            res.SubmitTime = DateTime.Now; //提交时间
                                           //  res.AdminDescription = obj.AdminDescription;//商家备注
            CB.UserOrder.Add(res);
            //产品表增加一次预约计数
            var cc = CB.CABIProduct.Where(a => a.ID == res.OrderProductID).FirstOrDefault();

            cc.OrderNum += 1; //预约量+1
            CB.Entry(cc).Property("OrderNum").IsModified = true;

            try { 
                CB.SaveChanges(); 
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.BadRequest, new resultInfo { Code = 400, Message = ex.Message, Data = "" });
            }

            return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message ="OK", Data = "" });

        }

        /// <summary>
        /// 删除一个预约
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("DeleteOrder")]
        public string DeleteOrder(UserOrder obj)
        {
            var cc = CB.UserOrder.Where(a => a.OrderID == obj.OrderID).FirstOrDefault();
            CB.UserOrder.Remove(cc);
            CB.SaveChanges();
            return code.returnSuccess("删除成功", "");

        }

        /// <summary>
        /// 修改预约
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("UpdateOrder")]
        public string UdateOrder(UserOrder obj)
        {
            var res = CB.UserOrder.Where(a => a.OrderID == obj.OrderID).FirstOrDefault();
            res.OrderPhone = obj.OrderPhone;
            res.OrderName = obj.OrderName;
            res.OrderSex = obj.OrderSex;
            res.OrderTime = obj.OrderTime;
            res.OrderContact = obj.OrderContact; //是否联系过
            res.AdminDescription = obj.AdminDescription;//商家备注
            CB.SaveChanges();
            return code.returnSuccess(res, "更新成功，返回当前修改后的数据");

        }

        /// <summary>
        /// admin查看预约列表（按预定时间排序）
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("AdminOrderList1")]
        public string AdminOrderList1(fenye obj)
        {


            //每页展示几条内容
            int pageSize = obj.pageSize;
            //当前页码
            int pageIndex = obj.pageIndex;

            var total = CB.UserOrder.Count();

            var list = CB.UserOrder.OrderByDescending(a => a.OrderTime).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess2(list, total, " admin查看预约列表（按预定时间排序）");

        }
        /// <summary>
        /// admin查看预约列表（按提交时间排序）
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("AdminOrderList2")]
        public IHttpActionResult AdminOrderList2(fenye obj)
        {
            //每页展示几条内容
            int pageSize = obj.pageSize;
            //当前页码
            int pageIndex = obj.pageIndex;

            var total = CB.UserOrder.Count();

            var list = CB.UserOrder.AsNoTracking().Where(s => s.OrderOpenID == obj.targetID).OrderByDescending(a => a.SubmitTime).Skip(pageSize * (pageIndex - 1)).Take(pageSize);

            if (list == null)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 404, Message = "您还没有预约记录" });
            }

            return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "OK", Data = list });

            //return code.returnSuccess2(list, total, " admin查看预约列表（按提交时间排序）");

        }

        /// <summary>
        /// 前台查看信息(按预约时间最近排序)
        /// </summary>
        /// <param name="obj">pageSize  pageIndex   OrderOpenID(预约ID) </param>
        /// <returns></returns>
        [HttpPost, Route("GetOrderList")]
        public IHttpActionResult GetOrderList(fenye obj)
        {

            //每页展示几条内容
            int pageSize = obj.pageSize;
            //当前页码
            int pageIndex = obj.pageIndex;

            var total = CB.UserOrder.Count();

            var list = CB.UserOrder.AsNoTracking().Where(s => s.OrderOpenID == obj.targetID).OrderByDescending(a => a.SubmitTime).Skip(pageSize * (pageIndex - 1)).Take(pageSize).Select(s=>new { 
                pid=s.OrderProductID,
                pname=s.OrderProduct,
                otime= s.OrderTime,
                opic=s.CABIProduct.CollectionImg,
                state=s.OrderContact,
                ID=s.OrderID
            });

            if (list == null)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 404, Message = "您还没有预约记录" });
            }

            return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "OK", Data = list });
        }

        #endregion

        #region 数据统计记录
        /// <summary>
        /// 访问小程序访问量+1
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("addVisited")]
        public string addVisited()
        {
            var cc = CB.ProgramData.Where(a => a.ID == 1).FirstOrDefault();
            cc.Visited += 1;
            CB.SaveChanges();
            return code.returnSuccess(cc.Visited, "访问量累计+1");

        }

        /// <summary>
        /// 某产品点击量+1
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("UpdateProductNum")]
        public string UpdateProductNum(CABIProduct obj)
        {
            var cc = CB.CABIProduct.Where(a => a.ID == obj.ID).FirstOrDefault();

            cc.ProductClickNum += 1;
            CB.SaveChanges();
            return code.returnSuccess(cc.OrderNum, "该产品访问量累计+1");

        }
        /// <summary>
        /// 某产品收藏量更新+1
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("UpdateCollectionNum")]
        public string UpdateCollectionNum(CABIProduct obj)
        {
            var cc = CB.CABIProduct.Where(a => a.ID == obj.ID).FirstOrDefault();

            cc.CollectionNum += 1;
            CB.SaveChanges();
            return code.returnSuccess(cc.CollectionNum, "该产品访问量累计+1");

        }

        /// <summary>
        /// 某产品预约量更新+1
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("UpdateOrderNum")]
        public string UpdateOrderNum(CABIProduct obj)
        {
            var cc = CB.CABIProduct.Where(a => a.ID == obj.ID).FirstOrDefault();

            cc.OrderNum += 1;
            CB.SaveChanges();
            return code.returnSuccess(cc.OrderNum, "该产品访问量累计+1");

        }
        /// <summary>
        /// 某产品分享量更新+1
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("UpdateShareNum")]
        public string UpdateShareNum(CABIProduct obj)
        {
            var cc = CB.CABIProduct.Where(a => a.ID == obj.ID).FirstOrDefault();

            cc.ShareNum += 1;
            CB.SaveChanges();
            return code.returnSuccess(cc.ShareNum, "该产品访问量累计+1");

        }

        /// <summary>
        /// 产品点击量排行
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("ReadProductNum")]
        public string ReadProductNum(fenye obj)
        {

            //每页展示几条内容
            int pageSize = obj.pageSize;
            //第几页
            int pageIndex = obj.pageIndex;

            var count = CB.CABIProduct.Count();
            var cc = CB.CABIProduct.OrderByDescending(a => a.ProductClickNum).ThenByDescending(a => a.AddTime).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess2(cc, count, "产品点击量排序");






            //var list = CB.CABIProduct.Where(a => a.ThemeID == themeid).OrderByDescending(a => a.Desplay).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            //return code.returnSuccess(list, "前台返回分页数据，当前是第" + pageIndex + "页，每页显示" + pageSize + "条内容");




        }



        /// <summary>
        /// 产品收藏量排行
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("ReadCollectionNum")]
        public string ReadCollectionNum(fenye obj)
        {
            //每页展示几条内容
            int pageSize = obj.pageSize;
            //第几页
            int pageIndex = obj.pageIndex;

            var count = CB.CABIProduct.Count();
            var cc = CB.CABIProduct.OrderByDescending(a => a.CollectionNum).ThenByDescending(a => a.AddTime).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess2(cc, count, "产品收藏量排序");
        }



        /// <summary>
        /// 产品预约数量排行
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("ReadOrderNum")]
        public string ReadOrderNum(fenye obj)
        {
            //每页展示几条内容
            int pageSize = obj.pageSize;
            //第几页
            int pageIndex = obj.pageIndex;

            var count = CB.CABIProduct.Count();
            var cc = CB.CABIProduct.OrderByDescending(a => a.OrderNum).ThenByDescending(a => a.AddTime).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess2(cc, count, "产品预约量排序");
        }


        /// <summary>
        /// 产品预约数量排行
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("ReadShareNum")]
        public string ReadShareNum(fenye obj)
        {
            //每页展示几条内容
            int pageSize = obj.pageSize;
            //第几页
            int pageIndex = obj.pageIndex;

            var count = CB.CABIProduct.Count();
            var cc = CB.CABIProduct.OrderByDescending(a => a.ShareNum).ThenByDescending(a => a.AddTime).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess2(cc, count, "产品预约量排序");
        }





        /// <summary>
        /// 小程序访问量
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("ReadVisitedNum")]
        public string ReadVisitedNum()
        {

            var cc = CB.ProgramData.Where(a => a.ID == 1).FirstOrDefault();
            return code.returnSuccess(cc, "产品预约量排序");
        }

        #endregion

        #region 公司信息管理
        /// <summary>
        /// 修改公司信息
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("UpdateCompanyInfo")]
        public string UpdateCompanyInfo(CompanyInfo obj)
        {

            var cc = CB.CompanyInfo.Where(a => a.ID == 1).FirstOrDefault();
            cc.Address = obj.Address;
            cc.Phone = obj.Phone;
            cc.WeChatNum = obj.WeChatNum;
            cc.WeChatImg = obj.WeChatImg;
            return code.returnSuccess(cc, "修改后的公司信息");

        }

        /// <summary>
        /// 查看公司信息
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>

        [HttpGet, Route("ReadCompanyInfo")]
        public string ReadCompanyInfo()
        {

            var cc = CB.CompanyInfo.Where(a => a.ID == 1).FirstOrDefault();
            return code.returnSuccess(cc, "修改后的公司信息");

        }





        #endregion

        #region 后台登陆

        /// <summary>
        /// 管理员登陆
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>

        [HttpPost, Route("AdminLogin")]
        public string AdminLogin(AdminUser obj)
        {

            var account = CB.AdminUser.Where(a => a.Account == obj.Account).FirstOrDefault();

            if (account == null)//账户名找不到
            {

                return code.AccountNull("没有找到该账户");



            }
            else //找到了账户
            {

                var pwd = common.sha1(obj.Password);
                if (account.Password == pwd)
                {
                    return code.loginSuccess("密码验证正确", "", "");
                }
                else
                {
                    return code.passwordError("登陆密码错误");

                }




            }


        }


        #endregion


        #region 并发测试
        /// <summary>
        /// 并发测试
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("testbing")]
        public string testbing()
        {
            var fdb = CB.CABIProduct.Where(a => a.ID == 12).FirstOrDefault();
            fdb.NewTitle = "第一次更新的title";

            //第二次修改并保存
            using (var sdb = new CABIProjectEntities())
            {
                var fdbobj = sdb.CABIProduct.Where(a => a.ID == 12).FirstOrDefault();
                fdbobj.NewTitle = "第二次更新的title";
                sdb.SaveChanges();

            }
            try
            {
                //保存第一个值会抛异常，因为TimeStamp 值不匹配
                CB.SaveChanges();
                return code.returnSuccess("保存成功", "");


            }
            catch (System.Data.DataException ex) //一般会跑到这里
            {
                return code.returnFail("并发起作用了");

            }



        }

        #endregion

        #region 微信公众平台账号获取

        /// <summary>
        /// 获取公众号access_token  服务端ache存7000秒
        /// </summary>
        /// <returns></returns>
        [CacheOutput(ServerTimeSpan = 7000)]
        public string GetToken()
        {
            var url = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid=wx0e699cb1fef36b68&secret=7f3bea2f74a7e213e744e7eb02c7fbe2";
            string getval = CreateGetHttpResponse(url);
            wxstate jsonobj = JsonConvert.DeserializeObject<wxstate>(getval);
            return jsonobj.access_token;//返回token
        }

        /// <summary>
        /// 获取登录用户的OpenID
        /// </summary>
        /// <param name="code">获取到的code</param>
        /// <returns></returns>
        [HttpGet,Route("GetUserOpenID")]
        public IHttpActionResult GetUserOpenID(string code)
        {
            string url = "https://api.weixin.qq.com/sns/jscode2session?appid=wx90bf745f5cc88bd7&secret=167bdd4296423e1e5e06ffca5d49ad5e&js_code=" + code + "&grant_type=authorization_code";
            string getVal = CreateGetHttpResponse(url);
            JObject jo = JObject.Parse(getVal);
            return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "获取成功", Data = jo });
        }


        /// <summary>
        /// 获取关联公众号文章
        /// </summary>
        /// <param name="articalDTO"></param>
        /// <returns></returns>
        [HttpPost, Route("GetWxArtical")]

        public IHttpActionResult GetArtical(wxArticalDTO articalDTO)
        {
            if (articalDTO == null)
            {
                return BadRequest("请输入相关信息");
            }
            string token = GetToken();

            string url = "https://api.weixin.qq.com/cgi-bin/material/batchget_material?access_token=" + token;
            string postData = JsonConvert.SerializeObject(articalDTO);
            var webCli = new WebClient();
            webCli.Headers[HttpRequestHeader.ContentType] = "application/json";
            webCli.Encoding = Encoding.UTF8;
            //string response = "{\"item\":[{\"media_id\":\"iHpfwZPeQpsiqQSXwHXC5uJvjqeKU49rp-7EF2k21FY\",\"content\":{\"news_item\":[{\"title\":\"CABI LADY乌日罕丨认真生活的定义是忠于自己\",\"author\":\"Miss 察\",\"digest\":\"她说：人心始终是自由的，要自己去选择快乐\",\"content\":\"<section class=\\\"xmteditor\\\" style=\\\"display:none;\\\" data-tools=\\\"新媒体管家\\\" data-label=\\\"powered by xmt.cn\\\" data-mpa-powered-by=\\\"yiban.io\\\"><\\/section><section style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.544px;white-space: normal;background-color: rgb(255, 255, 255);text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"0.13828125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyrJ03j3HAoT7w2ymTJWkmJZdLQfUOTNcFqHldrUN9DGObVyqYUicPiaibfcdicXHHEibPTyh6yHOvUDzA\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"width: 677px !important;visibility: visible !important;\\\"  \\/><\\/section><section style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.544px;white-space: normal;background-color: rgb(255, 255, 255);text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"708\\\" data-ratio=\\\"1.37265625\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq4yRQLPLFGn7GcVWOfOTlUFr7icsq7via0p6VajJicuWKXDYJLsppjNSibSg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"width: 558px;visibility: visible !important;height: 766px;\\\"  \\/><\\/section><p style=\\\"margin-right: 8px;margin-left: 8px;font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.544px;white-space: normal;background-color: rgb(255, 255, 255);text-align: center;\\\"><br  \\/><\\/p><section class=\\\"\\\" data-tools=\\\"135编辑器\\\" data-id=\\\"95198\\\" style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.544px;white-space: normal;background-color: rgb(255, 255, 255);\\\"><section style=\\\"width: 677px;line-height: 1.75em;margin-left: 8px;margin-right: 8px;\\\"><section style=\\\"display: flex;justify-content: center;align-items: center;\\\"><section data-width=\\\"100%\\\" style=\\\"width: 287.5px;border-top: 1px solid rgb(51, 51, 51);\\\"><\\/section><section style=\\\"margin-right: auto;margin-left: auto;width: 6em;background: rgb(254, 254, 254);vertical-align: middle;height: 20px;transform: rotate(0deg);\\\"><section style=\\\"width: 6em;\\\"><img class=\\\"__bg_gif\\\" data-ratio=\\\"0.2781954887218045\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_gif\\/GvcDexibACicwou99dHR4okqeN0hmre9f3s7ibI4KRHjz9r4AFYudCsflgzLibmCFDcbLvCVrN61rqAOBjoNStAMOQ\\/640?wx_fmt=gif\\\" data-type=\\\"gif\\\" data-w=\\\"133\\\" style=\\\"display: block;width: 6em !important;visibility: visible !important;\\\"  \\/><\\/section><\\/section><section data-width=\\\"100%\\\" style=\\\"width: 287.5px;border-top: 1px solid rgb(51, 51, 51);\\\"><\\/section><\\/section><\\/section><section data-autoskip=\\\"1\\\" class=\\\"\\\" style=\\\"padding: 1em;font-size: 14px;text-align: center;letter-spacing: 1.5px;line-height: 1.75em;color: rgb(63, 62, 63);\\\"><section style=\\\"line-height: 1.75em;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"text-align: justify;caret-color: rgb(255, 0, 0);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(0, 0, 0);font-size: 12px;\\\">「 察必女性 」是一档对话呼市本地优秀新女性的栏目，<\\/span><\\/section><section style=\\\"line-height: 1.75em;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"text-align: justify;caret-color: rgb(255, 0, 0);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(0, 0, 0);font-size: 12px;\\\">CABI察必不仅能为她们在穿着上找到独特个人魅力，<\\/span><\\/section><section style=\\\"line-height: 1.75em;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(0, 0, 0);font-size: 12px;\\\"><span style=\\\"text-align: justify;caret-color: rgb(255, 0, 0);\\\">也希望<\\/span><span style=\\\"caret-color: red;text-align: justify;\\\">了解她们背后丰厚精彩的人生故事<\\/span><\\/span><\\/section><\\/section><section data-width=\\\"100%\\\" style=\\\"width: 677px;border-top: 1px solid rgb(51, 51, 51);\\\"><\\/section><\\/section><section style=\\\"margin: 0pt 8px;font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.544px;white-space: normal;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: 1.75em;\\\"><span style=\\\"font-size: 14px;letter-spacing: 1px;font-family: &quot;Helvetica Neue&quot;, Helvetica, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;\\\"><strong><span style=\\\"caret-color: red;color: rgb(136, 136, 136);\\\"><br  \\/><\\/span><\\/strong><\\/span><\\/section><p style=\\\"margin: 0pt 8px;font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;white-space: normal;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;text-align: justify;\\\"><strong><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">大家好，我是Miss察<\\/span><\\/strong><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/p><p style=\\\"margin: 0pt 8px;font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;white-space: normal;background-color: rgb(255, 255, 255);line-height: 1.7;font-size: 11pt;color: rgb(73, 73, 73);text-align: justify;\\\"><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">&nbsp;<\\/span><\\/p><section style=\\\"margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\"><span data-shimo-docs=\\\"[[20,&quot;乌日罕有一种独特的魅力。\\\\n在聊天过程中，笑容仿佛会不自觉的被她带动，不是那种职业性的微笑，是莫名的就跟着真挚的笑了起来。她是一个耿直率真的白羊座女子，曾是厦门城市现代舞团的舞蹈演员，如今成为国有企业工会的文体科负责人。她的生活从来就不那么单一化，性格如是。\\\\n\\\\n&quot;],[20,&quot;轨迹无法复制，因为前路未知&quot;,&quot;27:\\\\&quot;16\\\\&quot;|8:1&quot;],[20,&quot;\\\\n\\\\n&quot;,&quot;7:1&quot;],[20,&quot;「做好自己最重要了，你要知道人的心始终是自由的，要自己去选择快乐。」\\\\n\\\\n12岁时独自一人前往异乡学习舞蹈，这份经历让乌日罕比同龄人更果断洒脱。聊起她做过最疯狂的决定，就是在20岁时候接到一通父母的电话，第二天便背起行囊放弃学了8年的舞蹈，离开自己心爱的舞团回到家乡。\\\\n\\\\n「那个时候年纪太小了，很单纯，根本不会想人生啊、未来啊，只是我今天这么想了，明天就要这样做」亦师亦友的舞团团长看到她发来的航班信息时傻了眼，想不通昨天还在拼命练舞的小女孩怎么说走就走，「团长是我最想要感谢的人，我从小就离开家，17岁的时候去了厦门，她对我的意义不只是舞团团长，更像是人生的启蒙人，她的品质，待人接物的方式，以及对生活的态度，到现在都影响着我。」\\\\n\\\\n我问乌日罕后来有没有去联系团长，乌日罕有些苦涩的笑了一下「很想她，但我不敢联系她。团长像对自己小孩一样培养我，挽留了我很久，临走时她对我说，只要我想回去，随时可以联系她，我却执意离开了，对她我心中始终是愧疚的。」\\\\n\\\\n离开厦门后她来到呼和浩特继续求学，找了一份行政工作，与舞蹈完全不相关的工作性质让她短暂的苦恼了一下，转到文体科后，乌日罕彻底放下拘束感，有多年舞台经验的她更明白组织群体活动应该什么时候动、什么时候静，对这份工作她始终充满创意与热情。\\\\n\\\\n休息日她喜欢去敬老院陪老人们聊天，还养了一条名叫「蹦哒」的小狗，日子就这样平凡又充实的过着，后来有一天，她遇到了自己的爱人...\\\\n\\\\n&quot;],[20,&quot;当白羊座遇到天秤座&quot;,&quot;27:\\\\&quot;16\\\\&quot;|8:1&quot;],[20,&quot;\\\\n&quot;,&quot;7:1&quot;],[20,&quot;\\\\n守护星是火星的白羊座独立而率真，白羊座爱一个人时是毫无保留的，全情投入地用整个灵魂和生命去爱你，正因如此她有时会毫无顾虑地说出一些冲动的话。而风元素的天秤座充满客观、冷静、理智的特质，很重视他人的想法和感受，他喜欢用大事化小、小事化了的方式去解决问题和冲突。\\\\n\\\\n「缘分是很奇妙的，我和我先生的父母互相认识，家人一直想让我们见面，但他工作比较忙，我一个女孩子嘛，也不可能主动去约人家，就这样拖了一年，后来年初见了面，年底订婚，第二年就结婚了」谈及婚姻的相处模式，乌日罕有自己的想法「两个人在一起，难免会发生各种各样的摩擦。发生冲突的时候要给彼此空间。我先生这一点做的特别好，知道我的负面情绪只要发泄出来就好了。我们已经结婚8年了，他了解我，包容我，碰到这样的男人你是吵不起来架的。」\\\\n\\\\n其实爱情本该如此，矛盾的重点不是要吵赢对方，而是知道对方为何会有这些情绪，你我都是彼此最爱的人，又怎么会在意琐事中的输赢呢？找到一见钟情的人或许不难，但想找有足够耐心解决分歧的人并不容易。\\\\n\\\\n从他们两人身上，我好像看到了爱情本身的模样。\\\\n\\\\n&quot;],[20,&quot;极致简约的独特美学&quot;,&quot;27:\\\\&quot;16\\\\&quot;|8:1&quot;],[20,&quot;\\\\n&quot;,&quot;7:1&quot;],[20,&quot;\\\\n「我每年都会给自己和家人定制几套蒙古袍，过节的时候全家人穿着蒙古袍一起唱歌，吃肉，喝酒，特定的服装真的会让你觉得这天不同于以往。」\\\\n\\\\n少数民族的人对于自己民族的自豪感是很强的。乌日罕的孩子在蒙族幼儿园上学，她说开家长会时老师会要求家长穿带有民族元素的服装，这是一种文化的传承，也是对于自己民族的尊重。\\\\n「我购买的第一件察必服装是一套绿色的裙子，虽然薄但面料很有质感，比传统蒙古袍更轻便，生活感更浓，这样简单的民族元素服装也能直观代表我们的民族。」\\\\n\\\\n乌日罕还给我们分享了她自己的搭配经验，日常穿衣不要太复杂，以简单干练的风格为主，女生一定要尝试穿高跟鞋，会让身材更加修长。\\\\n「我很支持女生化妆打扮，好看的服装能够带给你愉悦感，精致的妆容是对生活的尊重。别人怎么看无所谓，主要就是为了自己开心。」\\\\n\\\\n女生很容易因为在意他人的眼光而改变自己，一昧去迎合他人却会让自己过得越来越不快乐。乌日罕是个忠于自己的随性女人，她喜欢休息日和孩子懒洋洋的瑜伽时光，或是享受夏天傍晚全家人骑着单车感受微风，她习惯把日程排到满，过充实忙碌的一天。\\\\n\\\\n人们说「了解自己」是人生的必经选题，这样的过程并不容易，长大总要学会事故退让，可自己的生活，本就应该听从内心的声音。&quot;]]\\\"><p line=\\\"CFru\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">乌日罕有一种独特的魅力。<\\/span><\\/p><p line=\\\"PwhR\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">在聊天过程中，笑容仿佛会不自觉的被她带动，不是那种职业性的微笑，是莫名的就跟着真挚的笑了起来。<\\/span><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">她是一个耿直率真的白羊座女子，曾是厦门城市现代舞团的舞蹈演员，如今成为国有企业工会的文体科负责人。<\\/span><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">她的生活从来就不那么单一化，性格如是。<\\/span><span class=\\\"ql-author-24411236\\\"><\\/span><\\/p><p line=\\\"ozH3\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\">&nbsp;<\\/p><p style=\\\"text-align: center;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.2\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq4vkPwra2Jibhwic8KcUPBFV5HL44wMJ226MjmRKCcP4zusr3uT4pP8Pqw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"\\\"  \\/><\\/p><p line=\\\"HZyE\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><br  \\/><\\/p><p line=\\\"D39c\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><strong><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(24, 24, 120);\\\">「做好自己最重要了，你要知道人的心始终是自由的，要自己去选择快乐。<\\/span><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(24, 24, 120);\\\">」<\\/span><\\/strong><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/p><p line=\\\"HCrk\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/p><p line=\\\"Hp3L\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">12岁时独自一人前往异乡学习舞蹈，这份经历让乌日罕比同龄人更果断洒脱。<\\/span><\\/p><p line=\\\"Hp3L\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">聊起她做过最疯狂的决定，就是在20岁时候接到一通父母的电话，第二天便背起行囊放弃学了8年的舞蹈，离开自己心爱的舞团回到家乡。<\\/span><\\/p><p line=\\\"4ivR\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/p><p style=\\\"text-align: right;margin-left: 0px;margin-right: 0px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"1.00859375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq4iaFJicNpaGNXZvB9AgNDeLicyibQiafmbpSjESKgxc2sdO687tf1zE4HNvw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"width: 99%;height: auto !important;\\\"  \\/><\\/p><p line=\\\"4ivR\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: rgb(73, 73, 73);text-align: right;\\\"><span style=\\\"color: rgb(214, 214, 214);font-size: 12px;background-color: rgb(255, 255, 255);font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.4352px;text-align: right;\\\">▲<\\/span><\\/p><p style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.4352px;white-space: normal;background-color: rgb(255, 255, 255);text-align: right;\\\"><span style=\\\"color: rgb(214, 214, 214);font-size: 12px;font-family: mp-quote, -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;\\\">CABI察必<\\/span><span style=\\\"color: rgb(214, 214, 214);font-size: 12px;font-family: mp-quote, -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;\\\">墨色重工钉珠半裙套装<\\/span><span style=\\\"color: rgb(214, 214, 214);font-size: 12px;font-family: mp-quote, -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;\\\"><\\/span><\\/p><p line=\\\"4ivR\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><br  \\/><\\/p><p line=\\\"zZbW\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">「那个时候年纪太小了，很单纯，根本不会想人生啊、未来啊，只是我今天这么想了，明天就要这样做」<\\/span><\\/p><p line=\\\"zZbW\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><br  \\/><\\/p><p line=\\\"zZbW\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);\\\">亦师亦友的舞团团长看到她发来的航班信息时傻了眼，想不通昨天还在拼命练舞的小女孩怎么说走就走。<\\/span><\\/p><p line=\\\"zZbW\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);\\\"><br  \\/><\\/span><\\/p><p line=\\\"zZbW\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(0, 0, 0);\\\">「团长是我最想要感谢的人，我从小就离开家，17岁的时候去了厦门，她对我的意义不只是舞团团长，更像是人生的启蒙人，<\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(24, 24, 120);\\\"><strong>她的品质，待人接物的方式，以及对生活的态度，到现在都影响着我。<\\/strong><\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">」<\\/span><\\/p><p line=\\\"vblj\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/p><p line=\\\"vblj\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.6734375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq4bzKVzSrm2w8jEzC49w1bfBBE9pjpFjC3Qt4DKWQrJicRvKzfVdqpznQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;font-size: 14px;text-align: center;white-space: normal;\\\"  \\/><\\/span><\\/p><p line=\\\"4ivR\\\" style=\\\"margin-top: 0pt;margin-bottom: 0pt;font-family: Optima-Regular, PingFangTC-light;white-space: normal;line-height: 1.7;font-size: 11pt;color: rgb(73, 73, 73);text-align: left;\\\"><span style=\\\"color: rgb(214, 214, 214);font-size: 12px;background-color: rgb(255, 255, 255);font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.4352px;\\\">▲<\\/span><\\/p><p style=\\\"color: rgb(136, 136, 136);font-size: 14px;white-space: normal;font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.4352px;background-color: rgb(255, 255, 255);text-align: left;\\\"><span style=\\\"color: rgb(214, 214, 214);font-size: 12px;font-family: mp-quote, -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;\\\">CABI察必<\\/span><span style=\\\"color: rgb(214, 214, 214);font-size: 12px;font-family: mp-quote, -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;\\\">简约真丝拼织锦缎钉珠套装<\\/span><\\/p><p line=\\\"vblj\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><br  \\/><\\/p><p line=\\\"569x\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">我问乌日罕后来有没有去联系团长，<\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);\\\">乌日罕有些苦涩的笑了一下。<\\/span><\\/p><p line=\\\"569x\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">「<\\/span><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(24, 24, 120);\\\"><strong>很想她，但我不敢联系她。<\\/strong><\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\"><\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">团长像对自己小孩一样培养我，挽留了我很久，临走时她对我说，只要我想回去，随时可以联系她，我却执意离开了，对她我心中始终是愧疚的。」<\\/span><\\/p><p line=\\\"iq52\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/p><p line=\\\"53mx\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">离开厦门后她来到呼和浩特继续求学，找了一份行政工作，与舞蹈完全不相关的工作性质让她短暂的苦恼了一下，转到公司工会后，乌日罕彻底放下拘束感，有多年舞台经验的她更明白组织群体活动应该什么时候动、什么时候静。<\\/span><\\/p><p line=\\\"xLLW\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/p><p line=\\\"hWcd\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><span style=\\\"color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;font-size: 14px;letter-spacing: 1px;background-color: rgb(255, 255, 255);\\\">对这份工作她始终充满创意与热情，<\\/span>日子就这样平凡又充实的过着，后来有一天，她遇到了自己的爱人...<\\/span><span class=\\\"ql-author-24411236\\\"><\\/span><\\/p><p line=\\\"kIae\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\">&nbsp;<\\/p><p style=\\\"text-align: center;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.2\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq4BegicHA04O2Aicicb6IXnyHWtal7gvs08gbjcMqFOupuBniaaMwXyzJF7w\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"\\\"  \\/><\\/p><p line=\\\"Lhgh\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\">&nbsp;<\\/p><p line=\\\"yUVJ\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">守护星是火星的白羊座独立而率真，白羊座爱一个人时是毫无保留的，全情投入地用整个灵魂和生命去爱你，正因如此她有时会毫无顾虑地说出一些冲动的话。<\\/span><\\/p><p line=\\\"yUVJ\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">而风元素的天秤座充满客观、冷静、理智的特质，很重视他人的想法和感受，他喜欢用大事化小、小事化了的方式去解决问题和冲突。<\\/span><\\/p><p line=\\\"qoTL\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/p><p style=\\\"text-align: center;margin-left: 0px;margin-right: 0px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.5390625\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq4NfDyL6Oau9E8ZgsVcMnbwBxAZfftL9yUKGGlJz8Lfmd9hTsfTka64Q\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"\\\"  \\/><\\/p><p line=\\\"qoTL\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><br  \\/><\\/p><p line=\\\"Cow7\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">「<\\/span><strong><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(24, 24, 120);\\\">缘分是很奇妙的，<\\/span><\\/strong><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">我和我先生的父母互相认识，家人一直想让我们见面，但他工作比较忙，我一个女孩子嘛，也不可能主动去约人家，就这样拖了一年，后来年初见了面，年底订婚，第二年就结婚了」<\\/span><\\/p><p line=\\\"Cow7\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><br  \\/><\\/span><\\/p><p line=\\\"Cow7\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">谈及婚姻的相处模式，乌日罕有自己的想法。<\\/span><\\/p><p line=\\\"Cow7\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(0, 0, 0);\\\">「<\\/span><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(24, 24, 120);\\\"><strong>两个人在一起，难免会发生各种各样的摩擦，<\\/strong><\\/span><span style=\\\"color: rgb(24, 24, 120);\\\"><strong><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">发生冲突的时候要给彼此空间。<\\/span><\\/strong><\\/span><span style=\\\"color: rgb(73, 65, 65);\\\"><strong><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/strong><\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">我先生这一点做的特别好，知道我的负面情绪只要发泄出来就好了。<\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">我们已经结婚8年了，他了解我，包容我，碰到这样的男人你是吵不起来架的。<\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">」<\\/span><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/p><p line=\\\"JdA1\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/p><p style=\\\"text-align: left;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"507\\\" data-ratio=\\\"1.00859375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq4ZGgRdxpMKEndztFUEyoG6A6Vqo8JBgACRxqc7k9S04s6TAVDzcb8mQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"width: 558px;height: 563px;\\\"  \\/><\\/p><p line=\\\"JdA1\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/p><p line=\\\"jXW4\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">其实爱情本该如此，矛盾的重点不是要吵赢对方，而是知道对方为何会有这些情绪，你我都是彼此最爱的人，又怎么会在意琐事中的输赢呢？<\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\"><span data-shimo-docs=\\\"[[20,&quot;乌日罕有一种独特的魅力。\\\\n在聊天过程中，笑容仿佛会不自觉的被她带动，不是那种职业性的微笑，是莫名的就跟着真挚的笑了起来。她是一个耿直率真的白羊座女子，曾是厦门城市现代舞团的舞蹈演员，如今成为国有企业工会的文体科负责人。她的生活从来就不那么单一化，性格如是。\\\\n\\\\n&quot;],[20,&quot;轨迹无法复制，因为前路未知&quot;,&quot;27:\\\\&quot;16\\\\&quot;|8:1&quot;],[20,&quot;\\\\n\\\\n&quot;,&quot;7:1&quot;],[20,&quot;「做好自己最重要了，你要知道人的心始终是自由的，要自己去选择快乐。」\\\\n\\\\n12岁时独自一人前往异乡学习舞蹈，这份经历让乌日罕比同龄人更果断洒脱。聊起她做过最疯狂的决定，就是在20岁时候接到一通父母的电话，第二天便背起行囊放弃学了8年的舞蹈，离开自己心爱的舞团回到家乡。\\\\n\\\\n「那个时候年纪太小了，很单纯，根本不会想人生啊、未来啊，只是我今天这么想了，明天就要这样做」亦师亦友的舞团团长看到她发来的航班信息时傻了眼，想不通昨天还在拼命练舞的小女孩怎么说走就走，「团长是我最想要感谢的人，我从小就离开家，17岁的时候去了厦门，她对我的意义不只是舞团团长，更像是人生的启蒙人，她的品质，待人接物的方式，以及对生活的态度，到现在都影响着我。」\\\\n\\\\n我问乌日罕后来有没有去联系团长，乌日罕有些苦涩的笑了一下「很想她，但我不敢联系她。团长像对自己小孩一样培养我，挽留了我很久，临走时她对我说，只要我想回去，随时可以联系她，我却执意离开了，对她我心中始终是愧疚的。」\\\\n\\\\n离开厦门后她来到呼和浩特继续求学，找了一份行政工作，与舞蹈完全不相关的工作性质让她短暂的苦恼了一下，转到文体科后，乌日罕彻底放下拘束感，有多年舞台经验的她更明白组织群体活动应该什么时候动、什么时候静，对这份工作她始终充满创意与热情。\\\\n\\\\n休息日她喜欢去敬老院陪老人们聊天，还养了一条名叫「蹦哒」的小狗，日子就这样平凡又充实的过着，后来有一天，她遇到了自己的爱人...\\\\n\\\\n&quot;],[20,&quot;当白羊座遇到天秤座&quot;,&quot;27:\\\\&quot;16\\\\&quot;|8:1&quot;],[20,&quot;\\\\n&quot;,&quot;7:1&quot;],[20,&quot;\\\\n守护星是火星的白羊座独立而率真，白羊座爱一个人时是毫无保留的，全情投入地用整个灵魂和生命去爱你，正因如此她有时会毫无顾虑地说出一些冲动的话。而风元素的天秤座充满客观、冷静、理智的特质，很重视他人的想法和感受，他喜欢用大事化小、小事化了的方式去解决问题和冲突。\\\\n\\\\n「缘分是很奇妙的，我和我先生的父母互相认识，家人一直想让我们见面，但他工作比较忙，我一个女孩子嘛，也不可能主动去约人家，就这样拖了一年，后来年初见了面，年底订婚，第二年就结婚了」谈及婚姻的相处模式，乌日罕有自己的想法「两个人在一起，难免会发生各种各样的摩擦。发生冲突的时候要给彼此空间。我先生这一点做的特别好，知道我的负面情绪只要发泄出来就好了。我们已经结婚8年了，他了解我，包容我，碰到这样的男人你是吵不起来架的。」\\\\n\\\\n其实爱情本该如此，矛盾的重点不是要吵赢对方，而是知道对方为何会有这些情绪，你我都是彼此最爱的人，又怎么会在意琐事中的输赢呢？找到一见钟情的人或许不难，但想找有足够耐心解决分歧的人并不容易。\\\\n\\\\n从他们两人身上，我好像看到了爱情本身的模样。\\\\n\\\\n&quot;],[20,&quot;极致简约的独特美学&quot;,&quot;27:\\\\&quot;16\\\\&quot;|8:1&quot;],[20,&quot;\\\\n&quot;,&quot;7:1&quot;],[20,&quot;\\\\n「我每年都会给自己和家人定制几套蒙古袍，过节的时候全家人穿着蒙古袍一起唱歌，吃肉，喝酒，特定的服装真的会让你觉得这天不同于以往。」\\\\n\\\\n少数民族的人对于自己民族的自豪感是很强的。乌日罕的孩子在蒙族幼儿园上学，她说开家长会时老师会要求家长穿带有民族元素的服装，这是一种文化的传承，也是对于自己民族的尊重。\\\\n「我购买的第一件察必服装是一套绿色的裙子，虽然薄但面料很有质感，比传统蒙古袍更轻便，生活感更浓，这样简单的民族元素服装也能直观代表我们的民族。」\\\\n\\\\n乌日罕还给我们分享了她自己的搭配经验，日常穿衣不要太复杂，以简单干练的风格为主，女生一定要尝试穿高跟鞋，会让身材更加修长。\\\\n「我很支持女生化妆打扮，好看的服装能够带给你愉悦感，精致的妆容是对生活的尊重。别人怎么看无所谓，主要就是为了自己开心。」\\\\n\\\\n女生很容易因为在意他人的眼光而改变自己，一昧去迎合他人却会让自己过得越来越不快乐。乌日罕是个忠于自己的随性女人，她喜欢休息日和孩子懒洋洋的瑜伽时光，或是享受夏天傍晚全家人骑着单车感受微风，她习惯把日程排到满，过充实忙碌的一天。\\\\n\\\\n人们说「了解自己」是人生的必经选题，这样的过程并不容易，长大总要学会事故退让，可自己的生活，本就应该听从内心的声音。&quot;]]\\\" style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">找到一见钟情的人或许不难，但想找有足够耐心解决分歧的人并不容易。<\\/span><\\/span><span style=\\\"color: rgb(24, 24, 120);\\\"><strong><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/strong><\\/span><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/p><p line=\\\"4Twb\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/p><p line=\\\"honb\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">从他们两人身上，我好像看到了爱情本身的模样。<\\/span><\\/p><p line=\\\"honb\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><br  \\/><\\/span><\\/p><p style=\\\"text-align: justify;margin-left: 0px;margin-right: 0px;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"0.2\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq4Go2kkohxEbGzwx020oEKMgKWrPicpW1rZZmibcZ9DPMUzZqBRMvYyBug\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"\\\"  \\/><\\/p><p line=\\\"honb\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/p><\\/span><\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(0, 0, 0);\\\">「我每年都会给自己和家人定制几套蒙古袍，过节的时候全家人穿着蒙古袍一起唱歌，吃肉，喝酒，<\\/span><strong><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(24, 24, 120);\\\">特定的服装真的会让你觉得这天不同于以往。<\\/span><\\/strong><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">」<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">少数民族的人对于自己民族的自豪感是很强的。<\\/span><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">乌日罕的孩子在蒙古族幼儿园上学，她说老师会要求家长穿蒙古民族服饰参加家长会等活动，这是一种文化的传承，也是对自己民族的热爱。<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><br  \\/><\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">「我购买的第一件察必服装是一套绿色的裙子，虽然薄但面料很有质感，比传统蒙古袍更轻便，生活感更浓，这样简单的民族元素服装也能直观代表我们的民族。<\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">」<\\/span><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/section><p style=\\\"text-align: center;padding: 0px;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"508\\\" data-ratio=\\\"0.984375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxticmf9T7Xn3tSOzUlDCGaNCec5nkWKX2AzY7hZkW5gVXibOWlI80gWsxgzC1XnbdDI4aF1sjJzmGQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"box-shadow: none;width: 558px;height: 549px;\\\"  \\/><\\/p><p style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"background-color: rgb(255, 255, 255);color: rgb(214, 214, 214);font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;font-size: 12px;letter-spacing: 0.4352px;text-align: left;\\\">▲<\\/span><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/p><section style=\\\"color: rgb(136, 136, 136);font-size: 14px;white-space: normal;font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.4352px;background-color: rgb(255, 255, 255);text-align: left;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"color: rgb(214, 214, 214);font-size: 12px;font-family: mp-quote, -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;\\\">CABI察必<\\/span><span style=\\\"color: rgb(214, 214, 214);font-size: 12px;font-family: mp-quote, -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;\\\">19新品匈奴领钉珠连衣裙<\\/span><\\/section><p style=\\\"color: rgb(136, 136, 136);font-size: 14px;white-space: normal;font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.4352px;background-color: rgb(255, 255, 255);text-align: left;\\\"><span style=\\\"color: rgb(214, 214, 214);font-size: 12px;font-family: mp-quote, -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;\\\"><br  \\/><\\/span><\\/p><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">乌日罕还给我分享了她的搭配经验，日常穿衣不要太复杂，以简单干练的风格为主<\\/span><span style=\\\"line-height: 1.7;font-size: 11pt;color: rgb(73, 73, 73);font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">，女生一定要尝试穿高跟鞋，会让身材更加修长。<\\/span><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><br  \\/><\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(0, 0, 0);\\\">「我很支持女生化妆打扮，<\\/span><strong><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(24, 24, 120);\\\">好看的服装能够带给你愉悦感，精致的妆容是对生活的尊重。<\\/span><\\/strong><span style=\\\"color: rgb(73, 65, 65);\\\"><strong><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/strong><\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">别人怎么看无所谓，主要就是为了自己开心。<\\/span><span style=\\\"font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.7;font-size: 11pt;color: #494949;background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(73, 65, 65);\\\">」<\\/span><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">她是忠于自己的随性女人，喜欢休息日和孩子懒洋洋的瑜伽时光，或是享受夏天傍晚全家人骑着单车感受微风，她习惯把日程排到满，过充实忙碌的一天。<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">&nbsp;<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><span style=\\\"color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;font-size: 14px;letter-spacing: 1px;background-color: rgb(255, 255, 255);\\\">女生很容易因为在意他人的眼光而改变自己，一昧去迎合他人却会让自己过得越来越不快乐。<\\/span>人们说<\\/span><strong><span style=\\\"background-color: rgb(255, 255, 255);font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;color: rgb(24, 24, 120);\\\">「了解自己」<\\/span><\\/strong><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\">是人生的必经选题，这样的过程并不容易，长大总要学会事故退让，可自己的生活，本就应该听从内心的声音。<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><br  \\/><\\/span><\\/section><section style=\\\"text-align: right;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"553\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"644\\\" data-ratio=\\\"1.171875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq4cyD7eroUSlsyZhKZuXN2KHxgezmVe1aJibMK6r7ia7xOTzemQcks4TRg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"width: 553px;height: 648px;\\\"  \\/><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 1px;background-color: rgb(255, 255, 255);font-size: 11pt;color: rgb(73, 73, 73);line-height: normal;font-size: 14px;line-height: 1.75;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;letter-spacing: 1px;\\\"><\\/span><br  \\/><\\/section><section style=\\\"font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;white-space: normal;font-size: 14px;line-height: 1.75;letter-spacing: 1px;\\\"><section style=\\\"font-family: Optima-Regular, PingFangTC-light;text-align: left;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"line-height: 1.75;color: rgb(136, 136, 136);\\\">我是Miss察，每周四晚与您聊聊美学与时尚<\\/span><br  \\/><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: Optima-Regular, PingFangTC-light;text-align: left;\\\"><span style=\\\"line-height: 1.75;color: rgb(136, 136, 136);\\\">将生活，过的更漂亮<\\/span><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: Optima-Regular, PingFangTC-light;text-align: center;\\\"><br  \\/><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: Optima-Regular, PingFangTC-light;text-align: center;\\\"><span style=\\\"color: rgb(136, 136, 136);\\\">▼<\\/span><\\/section><section style=\\\"font-family: Optima-Regular, PingFangTC-light;text-align: center;\\\"><br  \\/><\\/section><p style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;text-align: center;\\\"><img class=\\\"rich_pages  __bg_gif\\\" data-ratio=\\\"0.24528301886792453\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_gif\\/GvcDexibACicwl4GmicIF0iaCm32lLibtneXl5510ER7212XqyRT344rvKxJ7n3WPCaKRtckpkE77sAn5rkWbjnKDwA\\/640?wx_fmt=gif\\\" data-type=\\\"gif\\\" data-w=\\\"636\\\" style=\\\"width: 636px !important;visibility: visible !important;\\\"  \\/><\\/p><section style=\\\"font-family: Optima-Regular, PingFangTC-light;text-align: center;\\\"><br  \\/><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\">- 预约服务 -<\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\"><br  \\/><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\">▼<\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;font-size: 16px;line-height: 1.75em;text-align: center;\\\"><br  \\/><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;font-size: 16px;line-height: 1.75em;text-align: center;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;line-height: 1.75em;\\\">CABI察必预约试衣服务已经开通<\\/span><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;font-size: 16px;line-height: 1.75em;text-align: center;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;line-height: 1.75em;\\\">只需添加客服微信，提供您的预约信息<\\/span><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;font-size: 16px;line-height: 1.75em;text-align: center;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;line-height: 1.75em;\\\">即可享受更加私密沉浸的购衣体验<\\/span><\\/section><p style=\\\"font-family: PingFangSC-light;\\\"><br  \\/><\\/p><p style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;text-align: center;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"1.03359375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACiczHyJJxpW5ROicctZXBRO4lDGlpMib3JmZI6f1YAO7ZWt1joDIOoLFzC1IBhHgSicKHYal7WTUl4lictw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"width: 677px !important;visibility: visible !important;\\\"  \\/><\\/p><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\"><br  \\/><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\">- 往期精选&nbsp;-<\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\"><br  \\/><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\">▼<\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;color: rgb(136, 136, 136);line-height: 1.75em;text-align: center;\\\"><br  \\/><\\/section><section style=\\\"text-align: center;margin-left: 8px;margin-right: 8px;\\\"><a target=\\\"_blank\\\" href=\\\"http:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&amp;mid=2247484804&amp;idx=1&amp;sn=6381f75f6679d57747c43a537bf119c5&amp;chksm=fa1ac3accd6d4aba1b6ef72b3db369caedd7f088b5c3b9384c665863fe30be2ae6911a845e82&amp;scene=21#wechat_redirect\\\" textvalue=\\\"你已选中了添加链接的内容\\\" data-itemshowtype=\\\"0\\\" tab=\\\"innerlink\\\" data-linktype=\\\"1\\\"><span class=\\\"js_jump_icon h5_image_link\\\" data-positionback=\\\"static\\\" style=\\\"top: auto;left: auto;margin: 0px;right: auto;bottom: auto;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.4171875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq4KCsvs7NT5iabuIONOH5wXWC2ibG4jarewxHfOJichjRCemibHbNgHFbzBQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"margin: 0px;\\\"  \\/><\\/span><\\/a><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;text-align: center;\\\"><a href=\\\"https:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&amp;mid=2247484716&amp;idx=1&amp;sn=7764d1720304e56e2b16d4aea6884b44&amp;scene=21#wechat_redirect\\\" target=\\\"_blank\\\" data-linktype=\\\"1\\\" style=\\\"-webkit-tap-highlight-color: rgba(0, 0, 0, 0);cursor: pointer;\\\"><span class=\\\"js_jump_icon h5_image_link\\\" data-positionback=\\\"static\\\" style=\\\"line-height: 0;top: auto;left: auto;right: auto;bottom: auto;\\\"><img class=\\\"rich_pages\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"233\\\" data-ratio=\\\"0.4171875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq4rmGRia22MtZ7bicqWG6eAkcVF4ssmibtcBNJiaZ623iaJ1BWmC0lqmd2ibFA\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"border-width: 0px;border-style: initial;border-color: initial;width: 559px;visibility: visible !important;height: 233px;\\\"  \\/><\\/span><\\/a><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;\\\"><a href=\\\"http:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&amp;mid=2247484673&amp;idx=1&amp;sn=75703bec6974bca10dc52ecf9450a28c&amp;chksm=fa1ac329cd6d4a3fde7d0cc07478a147db216589a063cd56ad8e6cae058034b1829e89f53d03&amp;scene=21#wechat_redirect\\\" target=\\\"_blank\\\" data-itemshowtype=\\\"0\\\" data-linktype=\\\"1\\\" hasload=\\\"1\\\" style=\\\"-webkit-tap-highlight-color: rgba(0, 0, 0, 0);cursor: pointer;\\\"><span class=\\\"js_jump_icon h5_image_link\\\" data-positionback=\\\"static\\\" style=\\\"line-height: 0;top: auto;left: auto;right: auto;bottom: auto;\\\"><img class=\\\"rich_pages\\\" data-backh=\\\"239\\\" data-backw=\\\"574\\\" data-before-oversubscription-url=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicwARbic3qMOXowFhT6l5zMDia9zVQG5icC0iaDHlVb4bREryLhJOoQV8PXsOFxfgZonPgHeXVYEApsRkg\\/?wx_fmt=jpeg\\\" data-ratio=\\\"0.4171875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicwARbic3qMOXowFhT6l5zMDia9zVQG5icC0iaDHlVb4bREryLhJOoQV8PXsOFxfgZonPgHeXVYEApsRkg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"border-width: 0px;border-style: initial;border-color: initial;width: 661px;visibility: visible !important;\\\"  \\/><\\/span><\\/a><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;\\\"><a href=\\\"http:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&amp;mid=2247484608&amp;idx=1&amp;sn=ba6ac08f12f73b52b2fb05bdc765a568&amp;chksm=fa1ac2e8cd6d4bfe9cdbade762db6cd49185b4241d4e4804b4eee9e95614efced11ced3e098b&amp;scene=21#wechat_redirect\\\" target=\\\"_blank\\\" data-itemshowtype=\\\"0\\\" data-linktype=\\\"1\\\" hasload=\\\"1\\\" style=\\\"-webkit-tap-highlight-color: rgba(0, 0, 0, 0);cursor: pointer;\\\"><span class=\\\"js_jump_icon h5_image_link\\\" data-positionback=\\\"static\\\" style=\\\"line-height: 0;top: auto;left: auto;right: auto;bottom: auto;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"0.4171875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicwARbic3qMOXowFhT6l5zMDiaGAAkMHVp7nKiarNC80sbdXLaicPALv8THn49BPXu6GLLFOoBjLmEW1Iw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"border-width: 0px;border-style: initial;border-color: initial;top: auto;left: auto;right: auto;bottom: auto;width: 677px !important;visibility: visible !important;\\\"  \\/><\\/span><\\/a><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;text-align: center;\\\"><a href=\\\"http:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&amp;mid=2247484577&amp;idx=1&amp;sn=e3243cf0338361e603a7609860cc2cee&amp;chksm=fa1ac289cd6d4b9f183659236c1b2ccb172a47a12442f6f4a1630d9a1cdc59900ed3ff1cc62b&amp;scene=21#wechat_redirect\\\" target=\\\"_blank\\\" data-itemshowtype=\\\"0\\\" data-linktype=\\\"1\\\" hasload=\\\"1\\\" style=\\\"-webkit-tap-highlight-color: rgba(0, 0, 0, 0);cursor: pointer;\\\"><span class=\\\"js_jump_icon h5_image_link\\\" data-positionback=\\\"static\\\" style=\\\"line-height: 0;top: auto;left: auto;right: auto;bottom: auto;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"0.4171875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicwARbic3qMOXowFhT6l5zMDiaI4iaQzIqMcJaxm9eAQ0E7Qcmg5p64oKQq9u4CGXFj3squkUT25aBib0Q\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"border-width: 0px;border-style: initial;border-color: initial;width: 677px !important;visibility: visible !important;\\\"  \\/><\\/span><\\/a><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: PingFangSC-light;text-align: center;\\\"><br  \\/><\\/section><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.544px;white-space: normal;text-align: center;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;\\\">▼<\\/span><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.544px;white-space: normal;text-align: center;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;\\\"><br  \\/><\\/span><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.544px;white-space: normal;text-align: center;\\\"><\\/section><section style=\\\"margin-right: 8px;margin-left: 8px;font-family: -apple-system-font, BlinkMacSystemFont, &quot;Helvetica Neue&quot;, &quot;PingFang SC&quot;, &quot;Hiragino Sans GB&quot;, &quot;Microsoft YaHei UI&quot;, &quot;Microsoft YaHei&quot;, Arial, sans-serif;letter-spacing: 0.544px;white-space: normal;text-align: center;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"2.1246753246753247\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_png\\/GvcDexibACicyrJ03j3HAoT7w2ymTJWkmJoNlCyic8MZLfic4ADShKCn6AuWGfnROQY9GAs3rEnfhghnrs2wGKX3jQ\\/640?wx_fmt=png\\\" data-type=\\\"png\\\" data-w=\\\"770\\\" style=\\\"width: 677px !important;visibility: visible !important;\\\"  \\/><\\/section>\",\"content_source_url\":\"\",\"thumb_media_id\":\"iHpfwZPeQpsiqQSXwHXC5jN4nhmL3sbVZoQ_EATgBYY\",\"show_cover_pic\":0,\"url\":\"http:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&mid=100001167&idx=1&sn=1a502ddb20d60a3989269b384cbfecbd&chksm=7a1ac3a74d6d4ab16d58a3b40281de3481a54f7e8421d8e59a07677f15425392dbc630ad0143#rd\",\"thumb_url\":\"http:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicxFsPvaSRbVPd71KtGFBWq401SAan91KTcpCO1AVC1QKtk4ibWhiaichyo9lmxtFhscURcmPa5N4yZHQ\\/0?wx_fmt=jpeg\",\"need_open_comment\":1,\"only_fans_can_comment\":0}],\"create_time\":1577258320,\"update_time\":1577349603},\"update_time\":1577349603},{\"media_id\":\"iHpfwZPeQpsiqQSXwHXC5g5xrpvhnp1CzQHQOJU-41c\",\"content\":{\"news_item\":[{\"title\":\"CABI察必送礼丨圣诞不止红配绿，这样搭配更高级\",\"author\":\"Miss 察\",\"digest\":\"CABI察必送圣诞礼物啦，点击即可领取精美胸针\",\"content\":\"<section class=\\\"xmteditor\\\" style=\\\"display:none;\\\" data-tools=\\\"新媒体管家\\\" data-label=\\\"powered by xmt.cn\\\" data-mpa-powered-by=\\\"yiban.io\\\"><\\/section><section class=\\\"mpa-template\\\" data-mpa-category=\\\"背景\\\" style=\\\"background-color: rgb(255, 255, 255);\\\"><section class=\\\"mpa-template\\\" data-mpa-category=\\\"背景\\\" style=\\\"background-color: rgb(255, 255, 255);\\\"><section class=\\\"xmteditor\\\" style=\\\"display: none;text-align: left;\\\" data-tools=\\\"新媒体管家\\\" data-label=\\\"powered by xmt.cn\\\"><\\/section><section style=\\\"font-family: Optima-Regular, PingFangTC-light;\\\"><section class=\\\"xmteditor\\\" style=\\\"display:none;\\\" data-tools=\\\"新媒体管家\\\" data-label=\\\"powered by xmt.cn\\\"><\\/section><\\/section><section style=\\\"font-size: 14px;line-height: 1.75;letter-spacing: 1px;box-sizing: border-box;\\\"><p style=\\\"text-align: center;font-family: Optima-Regular, PingFangTC-light;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"0.13828125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyrJ03j3HAoT7w2ymTJWkmJZdLQfUOTNcFqHldrUN9DGObVyqYUicPiaibfcdicXHHEibPTyh6yHOvUDzA\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"\\\"  \\/><\\/p><p line=\\\"nXtt\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><strong><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\">snow reindeer&nbsp;cinnamon<\\/span><\\/strong><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\"><\\/span><\\/p><p line=\\\"dPXP\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\">雪 驯鹿与肉桂<\\/span><\\/p><p line=\\\"HWHP\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\">2019.12.19&lt;CABI&gt;VOL.09<\\/span><\\/p><p line=\\\"WIoF\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\">去年亦是如此街景依旧<\\/span><\\/p><p line=\\\"Ug5f\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\">装饰了恋人们的笑颜，和我的行囊<\\/span><\\/p><p line=\\\"rboP\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\">\\t那繁星点缀下的平安夜<\\/span><\\/p><p line=\\\"oam7\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\">\\t即使有再多的路程，也要走到<\\/span><\\/p><p line=\\\"nbDv\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\">\\t初雪飘零之际，烛光中圣洁的蜡烛<\\/span><\\/p><p line=\\\"P4gF\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\">\\t那些宁静的旋律，如期而至<\\/span><\\/p><p line=\\\"Mhks\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\"> &nbsp; &nbsp; &nbsp; &nbsp;——《多少季的圣诞节》<\\/span><\\/p><p line=\\\"Mhks\\\" class=\\\"ql-align-center ql-long-24411236\\\" style=\\\"text-align:center;line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);text-align: center;font-family: Optima-Regular, PingFangTC-light;font-size: 12px;\\\"><br  \\/><\\/span><\\/p><p style=\\\"text-align: center;font-family: Optima-Regular, PingFangTC-light;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"0.35703125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_png\\/GvcDexibACicw2DgbBiczdicGyNbwDswBlskY0ekXZTC3Ez43RGM1dt7CsIJ53sHVzIkxSFkZWp395buuG0YOW3e6w\\/640?wx_fmt=png\\\" data-type=\\\"png\\\" data-w=\\\"1280\\\" style=\\\"\\\"  \\/><\\/p><p style=\\\"white-space: normal;box-sizing: border-box;font-family: Optima-Regular, PingFangTC-light;margin-left: 8px;margin-right: 8px;\\\"><br  \\/><span data-shimo-docs=\\\"[[20,&quot;大家好，我是Miss察\\\\n\\\\n生活总是需要一点仪式感的，圣诞节是调味平淡日子的最好机会，这几天就连大街上都纷纷挂上了缤纷的彩灯，各大店铺的音响也都放起《jingle bells》\\\\n如果你想要快速融入圣诞氛围，只要在家居装饰和服装搭配上做一些改变就可以啦，今天Miss察为大家准备了不同色系的圣诞风格，快看看你最喜欢哪一种吧&quot;]]\\\"><\\/span><\\/p><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">大家好，我是Miss察<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">&nbsp;<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">生活总是需要一点仪式感的<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><strong><span style=\\\"font-size: 14px;line-height: 1.75;letter-spacing: 1px;font-family: Optima-Regular, PingFangTC-light;color: rgb(217, 33, 66);\\\">圣诞节<\\/span><\\/strong><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">是调味平淡日子的最好机会<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">这几天就连大街上都纷纷挂上了缤纷的彩灯<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">各大店铺的音响也都放起《jingle bells》<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\"><br  \\/><\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">如果你想要快速融入<\\/span><strong><span style=\\\"font-size: 14px;line-height: 1.75;letter-spacing: 1px;font-family: Optima-Regular, PingFangTC-light;color: rgb(64, 118, 0);\\\">圣诞氛围<\\/span><\\/strong><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">只要在家居装饰和服装搭配上做一些改变就可以啦<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">今天Miss察为大家准备了不同色系的圣诞风格<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">快看看你最喜欢哪一种吧<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><span style=\\\"color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;font-size: 14px;\\\">（<\\/span><span style=\\\"color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;font-size: 14px;\\\">文末<\\/span><span style=\\\"color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;font-size: 14px;\\\">有圣诞礼<\\/span><span style=\\\"color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;font-size: 14px;\\\">物相送<\\/span><span style=\\\"color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;font-size: 14px;\\\">哦！<\\/span><span style=\\\"color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;font-size: 14px;\\\">）<\\/span><\\/section><section style=\\\"line-height: 1.7;margin: 0pt 8px;font-size: 11pt;color: rgb(73, 73, 73);\\\"><strong><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\"><\\/span><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\"><\\/span><\\/strong><\\/section><p line=\\\"yuE8\\\" class=\\\"ql-long-24411236\\\" style=\\\"line-height: 1.7;margin-bottom: 0pt;margin-top: 0pt;font-size: 11pt;color: #494949;\\\"><br  \\/><\\/p><p style=\\\"font-family: PingFangSC-light;color: rgb(136, 136, 136);box-sizing: border-box;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"132\\\" data-ratio=\\\"0.2359375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmYtJEdmbS5RwASvKRM9VoTU3UtY9luTRic1hkdSFvSaZsFWxYaiblHfjw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 559px;height: 132px;\\\"  \\/><\\/p><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"371\\\" data-ratio=\\\"0.66484375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmpiajb2QySy40XTWIq5f6M0UcRibHutnaj7cMxaxeE2FULlCl9HpcrwBQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 371px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"277\\\" data-ratio=\\\"0.49609375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmO4WOvgCAic7DlcE7yniamjP4DxA3g8bKhuN8060udIaSicefe8fZV5dOg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 277px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"644\\\" data-ratio=\\\"1.15234375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmWzPLCfYRwBicEmL0EpicibZIs7224iaWCV03pXxxPUsRaLegKppYFVwMjQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 559px;height: 644px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"500\\\" data-ratio=\\\"0.89453125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmE5NloSxabhHGjxzRfiazSKM8bgDzHAGHZXgS78Oyo2oiaxia7ofVVa40Q\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 559px;height: 500px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"570\\\" data-ratio=\\\"1.02265625\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmdlJp6HsvhMot0iaLLdQxojDl8uKicVJmbVdnX9hSnNJes8KYibxG4ouDg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 571px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"418\\\" data-ratio=\\\"0.7484375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDm5SgJVyDbNOhLJKGhXn5I5Rjfnycwuyarscs3J4jReyB1XcWF2WY2mg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 418px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"436\\\" data-ratio=\\\"0.78125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmcTSPhiahNaL2RpoQqUARlUyK6ia0KmRADPzgwK4rW7EOMVl01H9lN1iaw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 436px;\\\"  \\/><\\/section><section style=\\\"font-family: Optima-Regular, PingFangTC-light;white-space: normal;box-sizing: border-box;margin-left: 8px;margin-right: 8px;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.24609375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmau3Howvez1PLT561GeWrJp7Zq8LU4WLUCNGw62EGrzmfHPqX4ibiatog\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"322\\\" data-ratio=\\\"0.578125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDm6RrpUvCySXJXtGC85pQXML0fjZUYbgpPXVBCxDdcBIPj6pZrficPR4g\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 323px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"337\\\" data-ratio=\\\"0.60234375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmlO4dMYzVCNO7Gy0h9opQdkVWhoqLm4vLMhOFC0TgkWib8JwShHAwQiaA\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 559px;height: 337px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"490\\\" data-ratio=\\\"0.87578125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmjT4osP7pHAhZFQtnBo44p6FsxBk6qfzqB3fTTwhr1R05UJlXB8PaiaA\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 560px;height: 490px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"578\\\" data-ratio=\\\"1.03515625\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmAVatrb88M5cd5xA1gicic495B7u1Bpaqqn0qMxD02JqIS1zNjayNVUjA\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 578px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"364\\\" data-ratio=\\\"0.65234375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmwWvVRqbiasNcMGRRd1Ribm5L2TMyx0ZesNibMK1om6bCoicIEZ3D8vUJVw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 364px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"563\\\" data-ratio=\\\"1.009375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmldGvNLgiajGrriaI8dibtOibD22piakiaJp47PbtF09ciccu00QlTp5xBX0Jg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 563px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"259\\\" data-ratio=\\\"0.4640625\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmEPrHq1B6V5uJQ2fTMzWiawHibic5enXKTnnNow69Znp61Byjp6ic5L6hqQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 259px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.2546875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmgEHXmsybGhOvg3vgH9gIlDS7SBfNaWnAm7wBDibfdSDm8GZUfVr9ic9Q\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"374\\\" data-ratio=\\\"0.671875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDm4m1FWgpuj61XjvvMGv63RkESPOX5qcZtXs25LjogOUduzzy46raRTQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 375px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"409\\\" data-ratio=\\\"0.7328125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmqobwJoxX3XpB9b9pqYEeOfHa6FibpHH2P3QwJ22iabhbCN4GPk4maoeg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 409px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.79609375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmWQcpfbmNAicaxDicCcmPrpFDVonvqG5VSFb2vjQ5WUvgNibmMK3tca3Sw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"352\\\" data-ratio=\\\"0.63125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDm9oP5XwS3nD3Fy4sl0bKBYvoYokictpG0WYmwSxl0p3ZgDHvOfEJmHBQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 352px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"532\\\" data-ratio=\\\"0.953125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmXFIq7B6Mpv6vlvMicM8aLJGnKBRPuRTVgfYp5VPUglDiadytlzRr1ibLA\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 532px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"477\\\" data-ratio=\\\"0.8546875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmOh5QUfJwxttMAqCDJavHyv82hUtEnAC5gYnAU5ib75Oib1j1IrFj5W0A\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 477px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"318\\\" data-ratio=\\\"0.56875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmicaYTTVVd4KP22KdQHmLXAS1Ria5ib2Czybg2qRMsANQP9gBC9QyYgrXg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 559px;height: 318px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.2359375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmras1PpyvOsOtQnURt0yibKNB0n25NOuibINYKYA7AXC6xQJhkzVkmlog\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"421\\\" data-ratio=\\\"0.75390625\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmLHzibQLjqof2DIxdez80e60ZEYicbBjWxzRjD48EhUwH3ZiaHmuoiaw7NQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 421px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"599\\\" data-ratio=\\\"1.07421875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmuFNunURa8lcWtojCKG31Rtv6HXibiaCEIole6w5I2UBLB6us4XtreCRw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 599px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"746\\\" data-ratio=\\\"1.33671875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmJNIss9xhjt6RKkJgIeVDbFG3Cwsun1WJLaiam62CdpdzNsLT07ZfFNw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 746px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"343\\\" data-ratio=\\\"0.61484375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmwNekPMYMVI8GRIvibTibONt3czibqF1qvRWV8zT6iaj5weXFRcyQaBn5mg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 343px;\\\"  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"446\\\" data-ratio=\\\"0.8\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDm4uQOCja7mRP2FWbrTdbmynaelTo4Iia12zuWJa1Nq63S9HEstZpWkSw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 446px;\\\"  \\/><\\/section><p style=\\\"margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-cropselx1=\\\"0\\\" data-cropselx2=\\\"558\\\" data-cropsely1=\\\"0\\\" data-cropsely2=\\\"306\\\" data-ratio=\\\"0.5484375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmVbbkuwR1EMbJVfITfe03QibOQT9sOLJdwlMU5GPVtOWJYkpnfTWDicRA\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;width: 558px;height: 306px;\\\"  \\/><\\/p><section style=\\\"text-align: left;margin-left: 8px;margin-right: 8px;\\\"><section class=\\\"_135editor\\\" data-role=\\\"paragraph\\\" style=\\\"background-image: url(&quot;https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmZNn6TBmVjBjgykcgrCmeEEXibhDTP8iaG14ComGzeGl9YiaicTrYg5Zo9A\\/640?wx_fmt=jpeg&quot;);background-repeat: repeat;\\\"><p style=\\\"text-align:center;\\\"><br  \\/><\\/p><p style=\\\"text-align:center;\\\"><br  \\/><\\/p><p style=\\\"text-align:center;\\\"><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">圣诞节不仅是为这一年的努力画下句点<\\/span><\\/strong><br  \\/><\\/p><p style=\\\"text-align:center;\\\"><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">更是期许美好的下一年<\\/span><\\/strong><\\/p><p style=\\\"text-align:center;\\\"><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\"><br  \\/><\\/span><\\/strong><\\/p><p style=\\\"text-align:center;\\\"><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">CABI察必为了让大家过一个有温度的完美圣诞<\\/span><\\/strong><br  \\/><\\/p><p style=\\\"text-align:center;\\\"><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">特意准备了圣诞活动哦<\\/span><\\/strong><\\/p><p style=\\\"text-align:center;\\\"><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">礼品多多<\\/span><\\/strong><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">，快往下滑<\\/span><\\/strong><\\/p><p style=\\\"text-align:center;\\\"><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\"><br  \\/><\\/span><\\/strong><\\/p><p style=\\\"text-align:center;\\\"><br  \\/><\\/p><section class=\\\"_135editor\\\" data-tools=\\\"135编辑器\\\" data-id=\\\"89715\\\"><section style=\\\"width: 35px;margin-right: auto;margin-left: auto;\\\"><img class=\\\"\\\" data-ratio=\\\"1.345\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_gif\\/4BY4nn87ITkYibXSrg4akQicFianNJCG2W3iaKXPXwZkxWQF5Dth5XkjRDxFr7coiajCXeKoKL1jqLT501iazy11pxXw\\/640?wx_fmt=gif\\\" data-type=\\\"gif\\\" data-w=\\\"200\\\" data-width=\\\"100%\\\" style=\\\"width: 80%;display: block;height: auto !important;\\\"  \\/><\\/section><section style=\\\"width: 35px;margin-right: auto;margin-left: auto;\\\"><br  \\/><\\/section><section style=\\\"width: 35px;margin-right: auto;margin-left: auto;\\\"><br  \\/><\\/section><\\/section><\\/section><section style=\\\"margin-left: 0px;margin-right: 0px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.1328125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmcO3Aqf1U5kXlABwH8p0G4wYCebxqh7lINeQuH4bBpcBQBVBFhpw97g\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"width: 100%;height: auto !important;float: left;\\\"  \\/><\\/section><section class=\\\"_135editor\\\" data-role=\\\"paragraph\\\" style=\\\"background-image: url(&quot;https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmZNn6TBmVjBjgykcgrCmeEEXibhDTP8iaG14ComGzeGl9YiaicTrYg5Zo9A\\/640?wx_fmt=jpeg&quot;);background-repeat: repeat;\\\"><p style=\\\"text-align:center;\\\"><br  \\/><\\/p><p style=\\\"text-align:center;\\\"><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">点击下方图片<\\/span><\\/strong><\\/p><p style=\\\"text-align:center;\\\"><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">关注公众号&nbsp;<\\/span><\\/strong><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">即可参与抽奖<\\/span><\\/strong><\\/p><p style=\\\"text-align:center;\\\"><strong><span style=\\\"line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\"><br  \\/><\\/span><\\/strong><span style=\\\"color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;line-height: 1.75;\\\"><\\/span><\\/p><p><a class=\\\"weapp_image_link js_weapp_entry\\\" data-miniprogram-appid=\\\"wx178bc2f689f159eb\\\" data-miniprogram-path=\\\"pages\\/lawState\\/lawState?scene=19187023-1\\\" data-miniprogram-nickname=\\\"凡科抽奖\\\" href=\\\"\\\" data-miniprogram-type=\\\"image\\\" data-miniprogram-servicetype=\\\"0\\\" href=\\\"\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"0.5\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDm1W00iasjt1KUCyibh0iaCVibxzzfa7cFv0MsiaXeG1pDRHL71xrWNawtTJw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"900\\\" style=\\\"\\\"  \\/><\\/a><\\/p><\\/section><section class=\\\"_135editor\\\" data-role=\\\"paragraph\\\" style=\\\"background-image: url(&quot;https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmZNn6TBmVjBjgykcgrCmeEEXibhDTP8iaG14ComGzeGl9YiaicTrYg5Zo9A\\/640?wx_fmt=jpeg&quot;);background-repeat: repeat;\\\"><p style=\\\"text-align:center;\\\"><br  \\/><\\/p><p style=\\\"text-align:center;\\\"><span style=\\\"letter-spacing: 1px;font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">一等奖：面值1000元察必代金券1张<br  \\/><\\/span><\\/p><p style=\\\"text-align:center;\\\"><span style=\\\"letter-spacing: 1px;font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">二等奖：8折购物券1张+极致闪耀女士保温杯1个<br  \\/><\\/span><\\/p><p style=\\\"text-align:center;\\\"><span style=\\\"letter-spacing: 1px;font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\">三等奖：8折购物券1张+女士羊绒手套一双<\\/span><strong><span style=\\\"letter-spacing: 1px;font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\"><br  \\/><\\/span><\\/strong><\\/p><p style=\\\"text-align:center;\\\"><span style=\\\"letter-spacing: 1px;font-size: 14px;line-height: 1.75;font-family: Optima-Regular, PingFangTC-light;color: rgb(96, 93, 93);\\\"><br  \\/><\\/span><\\/p><section class=\\\"_135editor\\\" data-role=\\\"paragraph\\\" style=\\\"background-image: url(&quot;https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmZNn6TBmVjBjgykcgrCmeEEXibhDTP8iaG14ComGzeGl9YiaicTrYg5Zo9A\\/640?wx_fmt=jpeg&quot;);background-repeat: repeat;\\\"><p style=\\\"text-align:center;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;caret-color: red;font-family: Optima-Regular, PingFangTC-light;\\\"><strong style=\\\"color: rgb(96, 93, 93);\\\">凭中奖截图前往门店兑换礼品哦<\\/strong><\\/span><br  \\/><\\/p><p style=\\\"text-align:center;\\\"><br  \\/><\\/p><\\/section><section class=\\\"_135editor\\\" data-role=\\\"paragraph\\\" style=\\\"background-image: url(&quot;https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmZNn6TBmVjBjgykcgrCmeEEXibhDTP8iaG14ComGzeGl9YiaicTrYg5Zo9A\\/640?wx_fmt=jpeg&quot;);background-repeat: repeat;\\\"><p style=\\\"text-align:center;\\\"><br  \\/><\\/p><section class=\\\"_135editor\\\" data-tools=\\\"135编辑器\\\" data-id=\\\"89715\\\"><section style=\\\"width: 35px;margin-right: auto;margin-left: auto;\\\"><img class=\\\"\\\" data-ratio=\\\"1.345\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_gif\\/4BY4nn87ITkYibXSrg4akQicFianNJCG2W3iaKXPXwZkxWQF5Dth5XkjRDxFr7coiajCXeKoKL1jqLT501iazy11pxXw\\/640?wx_fmt=gif\\\" data-type=\\\"gif\\\" data-w=\\\"200\\\" data-width=\\\"100%\\\" style=\\\"width: 80%;display: block;height: auto !important;\\\"  \\/><\\/section><section style=\\\"width: 35px;margin-right: auto;margin-left: auto;\\\"><br  \\/><\\/section><\\/section><p style=\\\"text-align:center;\\\"><br  \\/><\\/p><\\/section><\\/section><p><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.1328125\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmIVRluUcmg93l11MZ83x3hXcCTIoRoZWZuicibbfujvVI97eicXXluSwxg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"float: left;\\\"  \\/><\\/p><section class=\\\"_135editor\\\" data-role=\\\"paragraph\\\" style=\\\"font-size: 14px;letter-spacing: 1px;text-align: left;white-space: normal;background-color: rgb(255, 255, 255);background-image: url(&quot;https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmZNn6TBmVjBjgykcgrCmeEEXibhDTP8iaG14ComGzeGl9YiaicTrYg5Zo9A\\/640?wx_fmt=jpeg&quot;);background-repeat: repeat;\\\"><p style=\\\"text-align: center;\\\"><br  \\/><\\/p><p style=\\\"text-align: center;\\\"><span style=\\\"color: rgb(96, 93, 93);\\\"><strong><span style=\\\"font-family: Optima-Regular, PingFangTC-light;\\\">关注公众号<\\/span><\\/strong><strong><span style=\\\"font-family: Optima-Regular, PingFangTC-light;\\\"><\\/span><\\/strong><\\/span><br  \\/><\\/p><p style=\\\"text-align: center;\\\"><span style=\\\"color: rgb(96, 93, 93);\\\"><strong><span style=\\\"font-family: Optima-Regular, PingFangTC-light;\\\">到店出示卡券即可领取售价298元的精美胸针<\\/span><\\/strong><\\/span><span style=\\\"font-family: Optima-Regular, PingFangTC-light;color: rgb(136, 136, 136);\\\"><br  \\/><\\/span><\\/p><p style=\\\"text-align: center;\\\"><span style=\\\"color: rgb(96, 93, 93);\\\"><strong><span style=\\\"font-family: Optima-Regular, PingFangTC-light;\\\"><br  \\/><\\/span><\\/strong><\\/span><\\/p><p style=\\\"text-align: center;\\\"><span style=\\\"font-family: Optima-Regular, PingFangTC-light;color: rgb(136, 136, 136);\\\"><iframe class=\\\"res_iframe card_iframe js_editor_card\\\" data-cardid=\\\"pR3Ek0jwktW6rla7CiihutAy6vvM\\\" data-num=\\\"0\\\" data-display-src=\\\"https:\\/\\/mp.weixin.qq.com\\/cgi-bin\\/readtemplate?t=cardticket\\/card_preview_tmpl&amp;logo_url=https%3A%2F%2Fmmbiz.qlogo.cn%2Fmmbiz_png%2FGvcDexibACicxZOqtiaayeO5Nb4nOZibSZeom65SMOmibH1rSUgnoFV3GdibtibBKDtibTJkibKunia6hdibdNTdibicTjwR88w%2F0%3Fwx_fmt%3Dpng&amp;brand_name=CABI%E5%AF%9F%E5%BF%85%E8%BD%BB%E5%A5%A2%E5%A5%B3%E8%A3%85&amp;title=%E7%B2%BE%E7%BE%8E%E8%83%B8%E9%92%88%E5%85%91%E6%8D%A2%E5%88%B8&amp;color=%23DE9C33&amp;lang=zh_CN&amp;cardid=pR3Ek0jwktW6rla7CiihutAy6vvM&amp;token=1780465299&amp;lang=zh_CN\\\" data-src=\\\"http:\\/\\/mp.weixin.qq.com\\/bizmall\\/appmsgcard?action=show&amp;biz=MzUyNTYwMzY1NQ==&amp;cardid=pR3Ek0jwktW6rla7CiihutAy6vvM&amp;wechat_card_js=1#wechat_redirect\\\"><\\/iframe><\\/span><\\/p><p style=\\\"text-align: center;\\\"><span style=\\\"font-family: Optima-Regular, PingFangTC-light;color: rgb(136, 136, 136);\\\"><br  \\/><\\/span><\\/p><section class=\\\"_135editor\\\" data-role=\\\"paragraph\\\" style=\\\"background-image: url(&quot;https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmZNn6TBmVjBjgykcgrCmeEEXibhDTP8iaG14ComGzeGl9YiaicTrYg5Zo9A\\/640?wx_fmt=jpeg&quot;);background-repeat: repeat;\\\"><p style=\\\"text-align:center;\\\"><br  \\/><\\/p><section class=\\\"_135editor\\\" data-tools=\\\"135编辑器\\\" data-id=\\\"89715\\\"><section style=\\\"width: 35px;margin-right: auto;margin-left: auto;\\\"><img class=\\\"\\\" data-ratio=\\\"1.345\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_gif\\/4BY4nn87ITkYibXSrg4akQicFianNJCG2W3iaKXPXwZkxWQF5Dth5XkjRDxFr7coiajCXeKoKL1jqLT501iazy11pxXw\\/640?wx_fmt=gif\\\" data-type=\\\"gif\\\" data-w=\\\"200\\\" data-width=\\\"100%\\\" style=\\\"width: 80%;display: block;height: auto !important;\\\"  \\/><\\/section><\\/section><p style=\\\"text-align:center;\\\"><br  \\/><\\/p><p style=\\\"text-align:center;\\\"><br  \\/><\\/p><\\/section><\\/section><p style=\\\"text-align: center;margin-left: 0px;margin-right: 0px;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"1.03984375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmR7I23EjcHh8BhmUvyXM5N2iajSs7N1NvVzO5ib6QMBXBHYOOcicAhDOzQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"\\\"  \\/><\\/p><p line=\\\"lSCe\\\" linespacing=\\\"150\\\" class=\\\"ql-long-24411236\\\" style=\\\"font-family: PingFangSC-light;\\\"><br  \\/><\\/p><p style=\\\"font-family: PingFangSC-light;text-align: left;margin-left: 0px;margin-right: 0px;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">看到CABI察必的圣诞惊喜后，有没有更期待今年的圣诞节了？<\\/span><\\/p><p style=\\\"font-family: PingFangSC-light;text-align: left;margin-left: 0px;margin-right: 0px;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">快来一起参与吧<\\/span><\\/p><p style=\\\"font-family: PingFangSC-light;text-align: left;margin-left: 0px;margin-right: 0px;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">Miss察提前祝大家Merry Christmas~<\\/span><\\/p><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><br  \\/><\\/section><section style=\\\"font-family: Optima-Regular, PingFangTC-light;white-space: normal;box-sizing: border-box;text-align: left;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">我是Miss察，每周四晚与您聊聊美学与时尚<\\/span><br style=\\\"box-sizing: border-box;\\\"  \\/><\\/section><section style=\\\"font-family: Optima-Regular, PingFangTC-light;white-space: normal;box-sizing: border-box;text-align: left;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"font-size: 14px;font-family: PingFangSC-light;line-height: 1.75;letter-spacing: 1px;color: rgb(136, 136, 136);font-family: Optima-Regular, PingFangTC-light;\\\">将生活，过的更漂亮<\\/span><\\/section><section style=\\\"font-family: Optima-Regular, PingFangTC-light;white-space: normal;box-sizing: border-box;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><br  \\/><\\/section><section style=\\\"font-family: Optima-Regular, PingFangTC-light;white-space: normal;box-sizing: border-box;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;text-align: center;\\\">▼<\\/span><\\/section><section style=\\\"font-family: Optima-Regular, PingFangTC-light;white-space: normal;box-sizing: border-box;text-align: center;\\\"><br  \\/><\\/section><p style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"0.24528301886792453\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_gif\\/GvcDexibACicwl4GmicIF0iaCm32lLibtneXl5510ER7212XqyRT344rvKxJ7n3WPCaKRtckpkE77sAn5rkWbjnKDwA\\/640?wx_fmt=gif\\\" data-type=\\\"gif\\\" data-w=\\\"636\\\" style=\\\"\\\"  \\/><\\/p><section style=\\\"font-family: Optima-Regular, PingFangTC-light;white-space: normal;box-sizing: border-box;text-align: center;\\\"><br  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"box-sizing: border-box;\\\">- 预约服务 -<\\/span><\\/section><section style=\\\"font-family: PingFangSC-light;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"box-sizing: border-box;\\\"><br  \\/><\\/span><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"box-sizing: border-box;\\\">▼<\\/span><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;font-size: 16px;white-space: normal;box-sizing: border-box;line-height: 1.75em;text-align: center;\\\"><br  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;font-size: 16px;white-space: normal;box-sizing: border-box;line-height: 1.75em;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;line-height: 1.75em;\\\">CABI察必预约试衣服务已经开通<\\/span><\\/section><section style=\\\"font-family: PingFangSC-light;font-size: 16px;white-space: normal;box-sizing: border-box;line-height: 1.75em;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;line-height: 1.75em;\\\">只需添加客服微信，提供您的预约信息<\\/span><\\/section><section style=\\\"font-family: PingFangSC-light;font-size: 16px;white-space: normal;box-sizing: border-box;line-height: 1.75em;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;line-height: 1.75em;\\\">即可享受更加私密沉浸的购衣体验<\\/span><\\/section><p style=\\\"font-family: PingFangSC-light;\\\"><br style=\\\"white-space: normal;\\\"  \\/><\\/p><p style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"1.03359375\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACiczHyJJxpW5ROicctZXBRO4lDGlpMib3JmZI6f1YAO7ZWt1joDIOoLFzC1IBhHgSicKHYal7WTUl4lictw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"\\\"  \\/><\\/p><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;\\\"><br  \\/><\\/section><section style=\\\"font-family: PingFangSC-light;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"box-sizing: border-box;\\\">- 往期精选&nbsp;-<\\/span><\\/section><section style=\\\"font-family: PingFangSC-light;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"box-sizing: border-box;\\\"><br  \\/><\\/span><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;margin-right: 8px;margin-left: 8px;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;\\\"><\\/section><section style=\\\"font-family: PingFangSC-light;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"box-sizing: border-box;\\\">▼<\\/span><\\/section><section style=\\\"font-family: PingFangSC-light;white-space: normal;color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;box-sizing: border-box;line-height: 1.75em;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"box-sizing: border-box;\\\"><br  \\/><\\/span><\\/section><section style=\\\"text-align: center;margin-left: 8px;margin-right: 8px;\\\"><a href=\\\"https:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&amp;mid=2247484716&amp;idx=1&amp;sn=7764d1720304e56e2b16d4aea6884b44&amp;scene=21#wechat_redirect\\\" target=\\\"_blank\\\" data-linktype=\\\"1\\\"><span class=\\\"js_jump_icon h5_image_link\\\" data-positionback=\\\"static\\\" style=\\\"top: auto;left: auto;margin: 0px;right: auto;bottom: auto;\\\"><img class=\\\"rich_pages js_insertlocalimg\\\" data-ratio=\\\"0.4171875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmLcoRq7zf5NJiasl1UEDLzzpE3gkbfSAWC3ZSIPMvicRGzvLKzCh0pSeQ\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"margin: 0px;\\\"  \\/><\\/span><\\/a><\\/section><section style=\\\"font-family: PingFangSC-light;margin-left: 8px;margin-right: 8px;\\\"><a href=\\\"http:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&amp;mid=2247484673&amp;idx=1&amp;sn=75703bec6974bca10dc52ecf9450a28c&amp;chksm=fa1ac329cd6d4a3fde7d0cc07478a147db216589a063cd56ad8e6cae058034b1829e89f53d03&amp;scene=21#wechat_redirect\\\" target=\\\"_blank\\\" data-itemshowtype=\\\"0\\\" data-linktype=\\\"1\\\"><span class=\\\"js_jump_icon h5_image_link\\\" data-positionback=\\\"static\\\" style=\\\"top: auto;left: auto;margin: 0px;right: auto;bottom: auto;\\\"><img class=\\\"rich_pages\\\" data-backh=\\\"239\\\" data-backw=\\\"574\\\" data-before-oversubscription-url=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicwARbic3qMOXowFhT6l5zMDia9zVQG5icC0iaDHlVb4bREryLhJOoQV8PXsOFxfgZonPgHeXVYEApsRkg\\/?wx_fmt=jpeg\\\" data-ratio=\\\"0.4171875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicwARbic3qMOXowFhT6l5zMDia9zVQG5icC0iaDHlVb4bREryLhJOoQV8PXsOFxfgZonPgHeXVYEApsRkg\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"width: 100%;height: auto;margin: 0px;\\\"  \\/><\\/span><\\/a><\\/section><section style=\\\"font-family: PingFangSC-light;margin-left: 8px;margin-right: 8px;\\\"><a href=\\\"http:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&amp;mid=2247484608&amp;idx=1&amp;sn=ba6ac08f12f73b52b2fb05bdc765a568&amp;chksm=fa1ac2e8cd6d4bfe9cdbade762db6cd49185b4241d4e4804b4eee9e95614efced11ced3e098b&amp;scene=21#wechat_redirect\\\" target=\\\"_blank\\\" data-itemshowtype=\\\"0\\\" data-linktype=\\\"1\\\"><span class=\\\"js_jump_icon h5_image_link\\\" data-positionback=\\\"static\\\" style=\\\"top: auto;left: auto;margin: 0px;right: auto;bottom: auto;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"0.4171875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicwARbic3qMOXowFhT6l5zMDiaGAAkMHVp7nKiarNC80sbdXLaicPALv8THn49BPXu6GLLFOoBjLmEW1Iw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"margin: 0px;top: auto;left: auto;right: auto;bottom: auto;\\\"  \\/><\\/span><\\/a><\\/section><section style=\\\"font-family: PingFangSC-light;text-align: center;margin-left: 8px;margin-right: 8px;\\\"><a href=\\\"http:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&amp;mid=2247484577&amp;idx=1&amp;sn=e3243cf0338361e603a7609860cc2cee&amp;chksm=fa1ac289cd6d4b9f183659236c1b2ccb172a47a12442f6f4a1630d9a1cdc59900ed3ff1cc62b&amp;scene=21#wechat_redirect\\\" target=\\\"_blank\\\" data-itemshowtype=\\\"0\\\" data-linktype=\\\"1\\\"><span class=\\\"js_jump_icon h5_image_link\\\" data-positionback=\\\"static\\\" style=\\\"top: auto;left: auto;margin: 0px;right: auto;bottom: auto;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"0.4171875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicwARbic3qMOXowFhT6l5zMDiaI4iaQzIqMcJaxm9eAQ0E7Qcmg5p64oKQq9u4CGXFj3squkUT25aBib0Q\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\" style=\\\"margin: 0px;\\\"  \\/><\\/span><\\/a><a href=\\\"http:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&amp;mid=2247484481&amp;idx=1&amp;sn=3f90cf9b0602da7b34c16df34e20c88f&amp;chksm=fa1ac269cd6d4b7f62967bd7f351e4006a3def4c83a73738b4aa60ef9627a3045d453141be91&amp;scene=21#wechat_redirect\\\" target=\\\"_blank\\\" data-itemshowtype=\\\"0\\\" data-linktype=\\\"1\\\" style=\\\"text-align: justify;\\\"><span class=\\\"js_jump_icon h5_image_link\\\" data-positionback=\\\"static\\\" style=\\\"top: auto;left: auto;right: auto;bottom: auto;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"0.4171875\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicwARbic3qMOXowFhT6l5zMDia7lCUaPHWgpLbDFDOVOEOrV4LfEicmMFXMXW7us1yfNko8IZrcQr3ejw\\/640?wx_fmt=jpeg\\\" data-type=\\\"jpeg\\\" data-w=\\\"1280\\\"  \\/><\\/span><\\/a><\\/section><section style=\\\"font-family: Optima-Regular, PingFangTC-light;white-space: normal;box-sizing: border-box;text-align: center;\\\"><br  \\/><\\/section><\\/section><section style=\\\"text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;text-align: center;\\\">▼<\\/span><\\/section><section style=\\\"text-align: center;margin-left: 8px;margin-right: 8px;\\\"><span style=\\\"color: rgb(136, 136, 136);font-size: 14px;letter-spacing: 1px;text-align: center;\\\"><br  \\/><\\/span><\\/section><section style=\\\"text-align: center;margin-left: 8px;margin-right: 8px;\\\"><img class=\\\"rich_pages\\\" data-ratio=\\\"2.1246753246753247\\\" data-s=\\\"300,640\\\" data-src=\\\"https:\\/\\/mmbiz.qpic.cn\\/mmbiz_png\\/GvcDexibACicyrJ03j3HAoT7w2ymTJWkmJoNlCyic8MZLfic4ADShKCn6AuWGfnROQY9GAs3rEnfhghnrs2wGKX3jQ\\/640?wx_fmt=png\\\" data-type=\\\"png\\\" data-w=\\\"770\\\" style=\\\"\\\"  \\/><\\/section><\\/section><\\/section>\",\"content_source_url\":\"\",\"thumb_media_id\":\"iHpfwZPeQpsiqQSXwHXC5uf6IBAdcHnBjCOhPx4HpiE\",\"show_cover_pic\":0,\"url\":\"http:\\/\\/mp.weixin.qq.com\\/s?__biz=MzUyNTYwMzY1NQ==&mid=100001114&idx=1&sn=0e795f0e73065225d21673159b5b6a3c&chksm=7a1ac3724d6d4a64b6434caa688eb1c3a17402509d5fd37ddfb3ae432e11e4949a3f9b1ed3d8#rd\",\"thumb_url\":\"http:\\/\\/mmbiz.qpic.cn\\/mmbiz_jpg\\/GvcDexibACicyiaVjeJjDicxia9GPjBictAibDmuBvV8jiaFVq75n62zXAWjQT4UY2RF9QtB4GicybfNvHJ3v3AzWRa04iag\\/0?wx_fmt=jpeg\",\"need_open_comment\":1,\"only_fans_can_comment\":0}],\"create_time\":1576654729,\"update_time\":1576720993},\"update_time\":1576720993}],\"total_count\":52,\"item_count\":2}";
            string response = webCli.UploadString(url, postData);
            //return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "获取成功", Data = response });


            try
            {
                List<WxArticalToShowDTO> list = new List<WxArticalToShowDTO>();
                List<WxArticalInfoDTO> infoList = new List<WxArticalInfoDTO>();

                JObject j = JObject.Parse(response);

                JArray jar = JArray.Parse(j["item"].ToString());

                foreach (var item in jar)
                {
                    JObject i = JObject.Parse(item["content"].ToString());

                    JArray in_li = JArray.Parse(i["news_item"].ToString());

                    foreach (var inItem in in_li)
                    {
                        infoList.Clear();
                        infoList.Add(new WxArticalInfoDTO
                        {
                            author = inItem["author"].ToString(),
                            title = inItem["title"].ToString(),
                            digest = inItem["digest"].ToString(),
                            thumb_url = inItem["thumb_url"].ToString(),
                            url = inItem["url"].ToString()
                        });
                    }

                    list.Add(new WxArticalToShowDTO
                    {
                        media_id = item["media_id"].ToString(),
                        update_time = item["update_time"].ToString(),
                        news_info = infoList.ToArray()
                    });
                }

                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "获取成功", Data = new { total_count = j["total_count"], item_count = j["item_count"], news_list = list } });
            }
            catch(Exception e)
            {
                return Content(HttpStatusCode.InternalServerError, new resultInfo { Code = 500, Message = "失败:" + e.Message, Data = "" });
            }

        }



        #endregion

        /// <summary>
        /// 添加用户信息至数据库
        /// </summary>
        /// <param name="addUserDTO"></param>
        /// <returns></returns>
        [HttpPost,Route("AddUser")]
        public IHttpActionResult AddUserInfo(AddUserDTO addUserDTO)
        {

            bool isExist = CB.UserInfo.Any(s => s.UserOpenID == addUserDTO.UserOpenID);

            if (isExist)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "该用户已经记录" });
            }
            else
            {

                UserInfo user = new UserInfo();
                user.UserOpenID = addUserDTO.UserOpenID;
                user.WxHeadImg = addUserDTO.WxHeadImg;
                user.City = addUserDTO.City;
                user.Province = addUserDTO.Province;
                user.Counrty = addUserDTO.Counrty;
                user.Gender = addUserDTO.Gender;
                user.Phone = addUserDTO.Phone;
                user.UserRealName = addUserDTO.UserRealName;
                user.WxNickName = addUserDTO.WxNickName;

                CB.UserInfo.Add(user);
                int i = CB.SaveChanges();
                if (i == 1)
                {
                    return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "记录成功" });
                }

                return Content(HttpStatusCode.BadRequest, new resultInfo { Code = 400, Message = "用户信息记录失败，请联系管理员" });
            }
            

        }

        /// <summary>
        /// 获取用户心愿夹列表
        /// </summary>
        /// <param name="openID">用户微信openID</param>
        /// <returns></returns>
        [HttpGet,Route("GetUserWishes")]
        public IHttpActionResult GetUserWishes(string openID)
        {
            var wishes = CB.Wishes.Where(s => s.UserOpenID == openID).Select(a => new
            {
                a.ProductID,
                a.ID,
                a.CABIProduct.CollectionImg,
                a.CABIProduct.NewTitle,
                a.CABIProduct.Price
            });
            if (wishes != null)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "OK", Data = wishes });
            }
            
            return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "NO" });

        }

        /// <summary>
        /// 删除心愿
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost,Route("DelWishes")]
        public IHttpActionResult DelWishes(int id)
        {
            int i = CB.Database.ExecuteSqlCommand("delete from Wishes where ID=@id", new SqlParameter("@id", id));
            if (i == 1)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "移除成功" });
            }
            return Content(HttpStatusCode.NotFound, new resultInfo { Code = 404, Message = "未找到相关记录" });
        }

        /// <summary>
        /// 从商品信息表中删除心愿
        /// </summary>
        /// <param name="obj">传入UserOpenID和pid</param>
        /// <returns></returns>
        [HttpPost,Route("DelWishesFromInfo")]
        public IHttpActionResult DelWishesFromInfo(JObject obj)
        {
            int i = CB.Database.ExecuteSqlCommand("delete from Wishes where UserOpenID=@uid and ProductID=@pid", new SqlParameter("@uid", obj["UserOpenID"].ToString()), new SqlParameter("@pid", obj["pid"].ToString()));

            if (i == 1)
            {
                return Content(HttpStatusCode.OK, new resultInfo { Code = 200, Message = "移除成功" });
            }
            return Content(HttpStatusCode.NotFound, new resultInfo { Code = 404, Message = "未找到相关记录" });
        }


        #region 方法合集
        /// <summary>
        /// 加一张图片方法
        /// </summary>
        /// <param name="jsonObj">流文件</param>
        /// <param name="ImgFirstName">图片前缀</param>
        /// <param name="objectPath">存储路径</param>
        /// <param name="endpoint">外网访问地址</param>
        /// <param name="accessKeyId">accessKey值 </param>
        /// <param name="accessKeySecret">secret值</param>
        /// <param name="bucketName">bucket值</param>
        /// <returns></returns>
        public string AddIMGFun(dynamic jsonObj, string ImgFirstName, string objectPath, string endpoint, string accessKeyId, string accessKeySecret, string bucketName)
        {
            var jsonStr = JsonConvert.SerializeObject(jsonObj); //先转换成json字符串
            var jsonParams = JsonConvert.DeserializeObject<dynamic>(jsonStr); //再转换成动态类型 ，这里是post传值出现的一些问题，里面有两个参数，所以要查找post传多个值的方法，这里选用了jobject方法
            string file64 = jsonParams.fileBase64; //接收两个参数，一个是fileBase64  图片转码后的内容  （这个是需要和前端统一的）
            string fileName = jsonParams.fileName;//这个是  fileName 用来放文件名
            byte[] bytes = Convert.FromBase64String(file64); //转换二进制
            var strDateTime = DateTime.Now.ToString("yyMMddhhmmssfff"); //取得当前时间字符串，用来拼接名字
            var strRan = Convert.ToString(new Random().Next(100, 999)); //生成三位随机数
            var fileExtension = Path.GetExtension(fileName); //获取名字的后缀
            var saveName = ImgFirstName + strDateTime + strRan + fileExtension; //名字的拼接 抬头命名 时间+随机数+后缀名
                                                                                // var savePath = "img/headimg/" + saveName; //oss存储的路径加上名字
            var savePath = objectPath + saveName; //oss存储的路径加随机生成的名字

            string filePath = "http://cabiproject.oss-cn-huhehaote.aliyuncs.com/" + savePath; //这里写地址加路径，拼接起来返回给前端

            //将文件上传到阿里云oss
            using (MemoryStream m = new MemoryStream(bytes))
            {
                var client = new OssClient(endpoint, accessKeyId, accessKeySecret); //填写三个参数
                var result = client.PutObject(bucketName, savePath, m); //填写哪个bucket ，路径是哪里，内容是什么 savepath是路径+名字，如果不指定路径就会在根目录下
                                                                        //--------------------------------------------------------

                // return filePath; //返回这个图片的路径
            }

            return filePath;


        }

        /// <summary>
        /// 删除图片
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="endpoint"></param>
        /// <param name="accessKeyId"></param>
        /// <param name="accessKeySecret"></param>
        /// <param name="substringURL">截取后的纯object路径,不要前面的域名！否则不能删除</param>
        public void RemoveIMGFun(string bucketName, string endpoint, string accessKeyId, string accessKeySecret, string stringURL, int substringNum)
        {
            var substringURL = stringURL.Substring(substringNum);
            //调用阿里云删除图片
            var Deleteclient = new OssClient(endpoint, accessKeyId, accessKeySecret);
            Deleteclient.DeleteObject(bucketName, substringURL);
        }
        public void RemoveIMGFun(string bucketName, string endpoint, string accessKeyId, string accessKeySecret, string stringURL)
        {
            // var substringURL = stringURL.Substring(substringNum);
            //把域名摘除


            string valstring = "https://" + bucketName + "." + endpoint + "/";

            string substringURL = stringURL.Replace(valstring.Trim(), string.Empty);


            //调用阿里云删除图片

            try
            {
                var Deleteclient = new OssClient(endpoint, accessKeyId, accessKeySecret);
                Deleteclient.DeleteObject(bucketName, substringURL);

            }
            catch (Exception E)
            {

                Console.WriteLine(E.ToString());
            }

        }


        /// <summary>
        /// 上传多张图片的方法
        /// </summary>
        /// <param name="base64">base64二进制</param>
        /// <param name="ImgFirstName">图片前缀名</param>
        /// <param name="objectPath">储存路径</param>
        /// <param name="endpoint">oss外网域名</param>
        /// <param name="accessKeyId">accesskeyid</param>
        /// <param name="accessKeySecret">accessKeySecret</param>
        /// <param name="bucketName">bucketName</param>
        /// <returns></returns>
        public dynamic IMGListFun(dynamic base64, string ImgFirstName, string objectPath, string endpoint, string accessKeyId, string accessKeySecret, string bucketName)
        {
            string[] IMGlist = Array.Empty<string>(); //返回一个空数组
            List<string> AddIMGlist = IMGlist.ToList();//后面可以添加

            for (int i = 0; i < base64.Count; i++)
            {


                dynamic base64vals = base64[i].baseURL.Replace("data:image/jpeg;base64,", "").Replace("data:image/png;base64,", "").Replace("data:image/jpg;base64,", "").Replace("data:image/gif;base64,", "").Replace("data:image/bmp;base64,", "");
                // AddIMGlist.Add(vals);

                // var file64 = ; //一个是fileBase64  图片转码后的内容  （这个是需要和前端统一的）
                // string fileExtension = Path.GetExtension(fileName); //获取名字的后缀
                string fileExtension = "." + base64[i].ext; //获取名字的后缀
                byte[] bytes = Convert.FromBase64String(base64vals); //转换二进制
                var strDateTime = DateTime.Now.ToString("yyMMddhhmmssfff"); //取得当前时间字符串，用来拼接名字
                var strRan = Convert.ToString(new Random().Next(100, 999)); //生成三位随机数

                var saveName = ImgFirstName + strDateTime + strRan + fileExtension; //名字的拼接 抬头命名 时间+随机数+后缀名
                                                                                    // var savePath = "img/headimg/" + saveName; //oss存储的路径加上名字
                var savePath = objectPath + saveName; //oss存储的路径加随机生成的名字

                string filePath = "http://cabiproject.oss-cn-huhehaote.aliyuncs.com/" + savePath; //这里写地址加路径，拼接起来返回给前端
                using (MemoryStream m = new MemoryStream(bytes))
                {
                    var client = new OssClient(endpoint, accessKeyId, accessKeySecret); //填写三个参数
                    var result = client.PutObject(bucketName, savePath, m); //填写哪个bucket ，路径是哪里，内容是什么 savepath是路径+名字，如果不指定路径就会在根目录下
                                                                            //--------------------------------------------------------
                    AddIMGlist.Add(filePath);



                    // return filePath; //返回这个图片的路径
                }
            }

            string resultstring = String.Join(",", AddIMGlist); //把数组转换为字符串分割的字符串

            return resultstring;
        }

        /// <summary>
        /// 发送http Get请求
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string CreateGetHttpResponse(string url)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";//链接类型 
            return GetResponseString((HttpWebResponse)request.GetResponse());
        }

        /// <summary>
        /// 从HttpWebResponse对象中提取响应的数据转换为字符串
        /// </summary>
        /// <param name="webresponse">从HttpWebResponse对象中提取响应的数据转换为字符串</param>
        /// <returns></returns>
        public string GetResponseString(HttpWebResponse webresponse)
        {
            using (Stream s = webresponse.GetResponseStream())
            {
                StreamReader reader = new StreamReader(s, Encoding.UTF8); return reader.ReadToEnd();
            }
        }

        #endregion

        /// <summary>
        /// 两个字段（用来上传图片用的）
        /// </summary>
        public class ImgInfo
        {
            /// <summary>
            /// base64二进制码
            /// </summary>
            public string baseURL { get; set; }
            /// <summary>
            /// 截取的后缀名
            /// </summary>
            public string ext { get; set; }
        }
    }
}
