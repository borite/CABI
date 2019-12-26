using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static CABIProgram.Class.aliyunconfig;

namespace ChinaAudio.Class
{
  

    /// <summary>
    /// Banner类阿里云配置字符串（察必Banner模块）
    /// </summary>
    /// 
  
    public class BannerOSSHelper : OssConfig
    {
      
       

        //bucket名字
        public static string bucketName = "cabiproject";
        //文件命名路径，后面需要填写随机数和后缀名加在后面，然后就可以放到指定目录了，这里写好文件夹就好
        public static  string objectPath = "CABIIMG/BannerIMG/";
        //图片前面的命名规则
       public static string ImgFirstName = "Banner" ;
        

    }



    /// <summary>
    /// 产品列表页页字符串配置
    /// </summary>
    public  class ProductsListOSSHelper : OssConfig
    {
      
       

        //bucket名字
        public static string bucketName = "cabiproject";
        //文件命名路径，后面需要填写随机数和后缀名加在后面，然后就可以放到指定目录了，这里写好文件夹就好
        public static string objectPath = "CABIIMG/ProductListIMG/";
        //图片前面的命名规则
        public static string ImgFirstName = "PdtList";


    }

    /// <summary>
    /// 产品收藏页字符串配置
    /// </summary>
    public  class ProductCollectionOSSHelper: OssConfig
    {
       
        //文件命名路径，后面需要填写随机数和后缀名加在后面，然后就可以放到指定目录了，这里写好文件夹就好
        public static string objectPath = "CABIIMG/PCollectionIMG/";
        //图片前面的命名规则
        public static string ImgFirstName = "Pdtcol";


    }


    /// <summary>
    /// 产品详情页字符串配置
    /// </summary>
    public class ProductsInfoOSSHelper: OssConfig
    {
        
        //文件命名路径，后面需要填写随机数和后缀名加在后面，然后就可以放到指定目录了，这里写好文件夹就好
        public static string objectPath = "CABIIMG/ProductInfoIMG/";
        //图片前面的命名规则
        public static string ImgFirstName = "Pdtinfo";


    }
    /// <summary>
    /// 产品编辑配置
    /// </summary>
    public class ProductEditIMG : OssConfig
    {

        //文件命名路径，后面需要填写随机数和后缀名加在后面，然后就可以放到指定目录了，这里写好文件夹就好
        public static string objectPath = "CABIIMG/ProductEditIMG/";
        //图片前面的命名规则
        public static string ImgFirstName = "Pdtedit";


    }

}