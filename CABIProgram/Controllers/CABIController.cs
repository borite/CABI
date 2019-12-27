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
using static CABIProgram.DTO.ProductDTO;
using static ChinaAudio.Class.Code;

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
        public IHttpActionResult BannerList()
        {
            var cc = CB.Banner.AsNoTracking().Where(a => a.IsLocked == false).OrderByDescending(a => a.Display);
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
        public string AdminBannerList()
        {
            var cc = CB.Banner.AsNoTracking().OrderByDescending(a => a.Display);
            return code.returnSuccess(cc, "返回Banner列表，admin使用");
        }


        /// <summary>
        /// 增加一个Banner数据
        /// </summary>
        /// <param name="jsonObj"></param>
        /// <returns></returns>
        [HttpPost, Route("AddBanner")]
        public string AddBanner(Banner jsonObj)
        {


            Banner res = new Banner();
            res.Title = jsonObj.Title;
            // res.Img = filePath; //图片链接地址保存

            res.URL = jsonObj.URL;
            res.IsLocked = jsonObj.IsLocked;
            res.Display = jsonObj.Display;


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
        [HttpPost, Route("UpdateBanner")]
        public string UpdateBanner(Banner obj)
        {
            var cc = CB.Banner.Where(a => a.ID == obj.ID).FirstOrDefault();
            cc.Title = obj.Title;
            cc.URL = obj.URL;
            cc.IsLocked = obj.IsLocked;
            cc.Display = obj.Display;
            CB.SaveChanges();
            return code.returnSuccess("更新banner成功", "");
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

            var list = CB.CABIProduct.Where(a => a.IsLocked == false && a.TopRecommend == true).OrderByDescending(a => a.TopDesplay).Skip(pageSize * (pageIndex - 1)).Take(pageSize).Select(s=>new
            {
                s.ID,
                s.CollectionImg
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
        /// 列表页多图片上传(更新加上传)
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


        /// <summary>
        /// 详情页多图片上传
        /// </summary>
        /// <param name="obj">var businessParam = { imgs: imgurl, ID: IDval };</param>
        /// <returns></returns>
        [HttpPost, Route("productsInfoList")]
        public string productsInfolist([FromBody] JObject obj)
        {
            var ID = Convert.ToInt32(obj["ID"]);

            string jsonval = obj["imgs"].ToString();
            var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval);
            //上传多张图片的方法，返回一个,分隔的图片地址字符串
            string urllist = IMGListFun(reslist, ProductsInfoOSSHelper.ImgFirstName, ProductsInfoOSSHelper.objectPath, ProductsInfoOSSHelper.endpoint, ProductsInfoOSSHelper.accessKeyId, ProductsInfoOSSHelper.accessKeySecret, ProductsInfoOSSHelper.bucketName);
            var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
            cc.ImgList = urllist;
            CB.SaveChanges();
            return code.returnSuccess("详情页上传成功", cc.ImgList);
        }

        /// <summary>
        /// 编辑器增加oss图片数组
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("ContentIMGList")]
        public IHttpActionResult ContentIMGList([FromBody] JObject obj)
        {

            var ID = Convert.ToInt32(obj["ID"]);

            string jsonval = obj["imgs"].ToString();
            var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval);
            //上传多张图片的方法，返回一个,分隔的图片地址字符串
            string urllist = IMGListFun(reslist, ProductEditIMG.ImgFirstName, ProductEditIMG.objectPath, ProductEditIMG.endpoint, ProductEditIMG.accessKeyId, ProductEditIMG.accessKeySecret, ProductEditIMG.bucketName);
            var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
            cc.ImgList = urllist;
            CB.SaveChanges();
            return Content(HttpStatusCode.OK, code.returnSuccess("成功", urllist));
        }

        /// <summary>
        /// 更新编辑器图，可更新可添加，用一个即可
        /// </summary>
        /// <param name="obj">需要产品ID和imgs数组</param>
        /// <returns></returns>
        [HttpPost, Route("UpdateEdit")]
        public IHttpActionResult UpdateEdit([FromBody] JObject obj)
        {
            try
            {

                var ID = Convert.ToInt32(obj["ID"]);
                var search = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                string jsonval = obj["imgs"].ToString();
                var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval); //反序列化成json

                var urlListstr = search.ContentIMGList; //读取数据库中的字符串
                if (string.IsNullOrEmpty(urlListstr)) //如果是空的，直接添加新的字符串，不删除 这是增加操作
                {

                    //上传多张图片的方法，返回一个,分隔的图片地址字符串
                    string urllist = IMGListFun(reslist, ProductEditIMG.ImgFirstName, ProductEditIMG.objectPath, ProductEditIMG.endpoint, ProductEditIMG.accessKeyId, ProductEditIMG.accessKeySecret, ProductEditIMG.bucketName);
                    // var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                    //数据库赋值
                    search.ContentIMGList = urllist; //注意这里每次记得修改，数据库对应字段赋值
                    CB.SaveChanges();
                    return Content(HttpStatusCode.OK, code.returnSuccess("如果是空的，相当于插入直接更新", search.ContentIMGList));


                }
                else //如果有内容删掉之前的oss文件，再写入新的oss文件
                {
                    string[] arraystring = urlListstr.Split(','); //转化一下，先删除数据库中的图片
                    foreach (var item in arraystring) //删除oss的对应图片
                    {
                        // var substringURL = item.Substring(49);
                        RemoveIMGFun(ProductsInfoOSSHelper.bucketName, ProductEditIMG.endpoint, ProductEditIMG.accessKeyId, ProductEditIMG.accessKeySecret, item, 49);

                    }
                    //上传多张图片的方法，返回一个,分隔的图片地址字符串
                    string urllist = IMGListFun(reslist, ProductEditIMG.ImgFirstName, ProductEditIMG.objectPath, ProductEditIMG.endpoint, ProductEditIMG.accessKeyId, ProductEditIMG.accessKeySecret, ProductEditIMG.bucketName);
                    // var cc = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();
                    //数据库赋值
                    search.ContentIMGList = urllist;
                    CB.SaveChanges();
                    return Content(HttpStatusCode.OK, code.returnSuccess("数据更新", search.ContentIMGList));
                }
            }
            catch (Exception ex)
            {

              //  return code.returnFail(ex);
                return Content(HttpStatusCode.BadRequest,code.returnFail(ex))  ;
            }






        }


    


        /// <summary>
        /// 收藏图上传
        /// </summary>
        /// <param name="obj">传入ID还有一个 businessParam的数组 里面的格式是{ imgs: （二进制文件）imgurl,ID: (产品ID)IDval}  </param>
        /// <returns>data返回具体地址</returns>
        [HttpPost, Route("AddCollection")]
        public string AddCollection([FromBody]JObject obj)
        {
            try
            {

                var ID = Convert.ToInt32(obj["ID"]);
                var search = CB.CABIProduct.Where(a => a.ID == ID).FirstOrDefault();



                string jsonval = obj["imgs"].ToString();
                var reslist = JsonConvert.DeserializeObject<List<ImgInfo>>(jsonval);
                //上传多张图片的方法，返回一个,分隔的图片地址字符串
                string urllist = IMGListFun(reslist, ProductCollectionOSSHelper.ImgFirstName, ProductCollectionOSSHelper.objectPath, ProductCollectionOSSHelper.endpoint, ProductCollectionOSSHelper.accessKeyId, ProductCollectionOSSHelper.accessKeySecret, ProductCollectionOSSHelper.bucketName);
                search.CollectionImg = urllist;
                CB.SaveChanges();
                return code.returnSuccess("收藏图上传成功", search.CollectionImg);

            }
            catch (Exception ex)
            {

                return code.returnFail(ex);
            }




        }

        /// <summary>
        /// 更新一个详情图
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

                var urlListstr = search.ImgList; //读取数据库中的字符串
                if (string.IsNullOrEmpty(urlListstr)) //如果是空的，直接添加新的字符串，不删除
                {

                    //上传多张图片的方法，返回一个,分隔的图片地址字符串
                    string urllist = IMGListFun(reslist, ProductsInfoOSSHelper.ImgFirstName, ProductsInfoOSSHelper.objectPath, ProductsInfoOSSHelper.endpoint, ProductsInfoOSSHelper.accessKeyId, ProductsInfoOSSHelper.accessKeySecret, ProductsInfoOSSHelper.bucketName);
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
                        RemoveIMGFun(ProductsInfoOSSHelper.bucketName, ProductsListOSSHelper.endpoint, ProductsListOSSHelper.accessKeyId, ProductsListOSSHelper.accessKeySecret, item, 49);

                    }
                    //上传多张图片的方法，返回一个,分隔的图片地址字符串
                    string urllist = IMGListFun(reslist, ProductsInfoOSSHelper.ImgFirstName, ProductsInfoOSSHelper.objectPath, ProductsInfoOSSHelper.endpoint, ProductsInfoOSSHelper.accessKeyId, ProductsInfoOSSHelper.accessKeySecret, ProductsInfoOSSHelper.bucketName);
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
        /// 更新一个收藏图
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
                    return code.returnSuccess("详情页更新成功", search.ListImg);

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
        /// 删除产品(列表图、编辑器图、收藏图、详情图)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost, Route("DeleteProduct")]
        public string DeleteProduct(CABIProduct obj)
        {

            var cc = CB.CABIProduct.Where(a => a.ID == obj.ID).FirstOrDefault();
            string[] IMGlist = Array.Empty<string>(); //返回一个空数组
            List<string> AddIMGlist = IMGlist.ToList();//后面可以添加
            AddIMGlist.Add(cc.ImgList);
            AddIMGlist.Add(cc.ListImg);
            AddIMGlist.Add(cc.CollectionImg);
            AddIMGlist.Add(cc.ContentIMGList);
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
        public string AddOrder(UserOrder obj)
        {


            //  var order= CB.UserOrder
            UserOrder res = new UserOrder();
            res.OrderOpenID = obj.OrderOpenID;
            res.OrderPhone = obj.OrderPhone;
            res.OrderName = obj.OrderName;
            res.OrderHeadImg = obj.OrderHeadImg;
            res.OrderSex = obj.OrderSex;
            res.OrderSex = obj.OrderSex;
            // res.TimeQuantum = obj.TimeQuantum;
            res.OrderTime = obj.OrderTime;
            res.OrderProduct = obj.OrderProduct;
            res.OrderProductID = obj.OrderProductID;
            //res.Description = obj.Description;
            res.OrderContact = obj.OrderContact; //预约状态（1.待试衣2.订单完成3.订单失效）
            res.SubmitTime = DateTime.Now; //提交时间
                                           //  res.AdminDescription = obj.AdminDescription;//商家备注
            CB.UserOrder.Add(res);
            //产品表增加一次预约计数
            var cc = CB.CABIProduct.Where(a => a.ID == res.OrderProductID).FirstOrDefault();

            cc.OrderNum += 1; //预约量+1
            CB.Entry(cc).Property("OrderNum").IsModified = true;



            try { CB.SaveChanges(); }
            catch (Exception ex)
            {
                throw;
            }

            return code.returnSuccess("订单提交成功", "");

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
        public string AdminOrderList2(fenye obj)
        {

            //每页展示几条内容
            int pageSize = obj.pageSize;
            //当前页码
            int pageIndex = obj.pageIndex;




            var total = CB.UserOrder.Count();

            var list = CB.UserOrder.OrderByDescending(a => a.SubmitTime).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess2(list, total, " admin查看预约列表（按提交时间排序）");

        }

        /// <summary>
        /// 前台查看信息(按预约时间最近排序)
        /// </summary>
        /// <param name="obj">pageSize  pageIndex   OrderOpenID(预约ID) </param>
        /// <returns></returns>
        [HttpPost, Route("OrderList1")]
        public string OrderList1([FromBody] JObject obj)
        {

            //m每页显示几条内容
            int pageSize = Convert.ToInt16(obj["pageSize"]);
            //当前页码
            int pageIndex = Convert.ToInt16(obj["pageIndex"]);

            string OrderOpenID = obj["OrderOpenID"].ToString();


            var total = CB.UserOrder.Where(a => a.OrderOpenID == OrderOpenID).Count();

            var list = CB.UserOrder.Where(a => a.OrderOpenID == OrderOpenID).OrderByDescending(a => a.SubmitTime).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return code.returnSuccess2(list, total, "前台查看信息(按预约时间最近排序)，当前是第" + pageIndex + "页，每页显示" + pageSize + "条内容");


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
        [HttpGet, Route("GetToken")]
     //   [CacheOutput(ClientTimeSpan = 5, ServerTimeSpan = 5)]
        [CacheOutput( ServerTimeSpan = 7000)]
        public string GetToken()
        {
            var url = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid=wx0e699cb1fef36b68&secret=7f3bea2f74a7e213e744e7eb02c7fbe2";

            string getval = CreateGetHttpResponse(url);
            wxstate jsonobj = JsonConvert.DeserializeObject<wxstate>(getval);
            return jsonobj.access_token;//返回token
        }


        #endregion

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
