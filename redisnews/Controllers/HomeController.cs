using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ServiceStack.Redis;
using Microsoft.Extensions.Configuration;
using testredis.Models;

namespace testredis.Controllers
{
    public class HomeController : Controller
    {
        IConfiguration _configuration;
        RedisEndpoint conf;
        public static News model;
        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
            conf = new RedisEndpoint() { Host = _configuration.GetValue<string>("RedisConfig:Host"), Port = _configuration.GetValue<int>("RedisConfig:Port") };
            if (model == null)
            {
                model = new News()
                {
                    ID = 1,
                    Title = "19 MAYIS TÜM YURTTA COŞKUYLA KUTLANDI",
                    Detail = @"19 Mayıs Atatürk'ü Anma Gençlik ve Spor Bayramı tüm yurtta coşkuyla kutlandı. 
                    Mustafa Kemal Atatürk’ün beraberindeki 18 kişiyle 19 Mayıs 1919’da Samsun’a çıkmasıyla birlikte Milli Mücadele’nin meşalesi yakılmış, 
                    birkaç yıl içinde çağdaş Türkiye Cumhuriyeti doğmuştu.",
                    CreatedDate = DateTime.Now,
                    Image = "ondokuz.jpg",
                    UpdatedDate = null,
                    IsError = false
                };
            }
        }
        public IActionResult Index()
        {
            var data = AddRedisCache(model, "RedisNews");
            return View(data);
        }
        public IActionResult Admin(int? ID = 1, bool isError = false)
        {  
            var data = AddRedisCache(model, "RedisNews");
            data.IsError=isError;
            return View(data);
        }
        public async Task<IActionResult> UpdateNews(News news, IFormFile NewsImage)
        {
            using (IRedisClient client = new RedisClient(conf))
            {
                if (CheckDataStable(client, news.UpdatedDate))
                {
                    //Güncellenen Resim Yüklenir
                    if (NewsImage != null && NewsImage.Length > 0)
                    {
                        var fileName = ContentDispositionHeaderValue.Parse(NewsImage.ContentDisposition).FileName.Trim('"');
                        var imageGuidName = Convert.ToString(Guid.NewGuid());

                        var ImageExtension = Path.GetExtension(fileName);
                        var PureFileName = Path.GetFileNameWithoutExtension(fileName);
                        var newFileName = PureFileName + '_' + imageGuidName + ImageExtension;
                        news.Image = newFileName; //Yeni resmin Unique adı.                
                        fileName = Path.Combine("wwwroot/images") + $"/{newFileName}";
                        using (var stream = new FileStream(fileName, FileMode.Create))
                        {
                            await NewsImage.CopyToAsync(stream);
                        }
                    }
                    //1-)Var olan sunucudaki Redis Güncellenir. 2-) Microservis tetiklenir.              

                    news.UpdatedDate = DateTime.Now;
                    client.Set<News>("RedisNews", news);
                    var newsJson = JsonConvert.SerializeObject(news);
                    client.PublishMessage("News", newsJson);
                }
                else
                {
                    Console.WriteLine("Kayıt da tutarsızlık vardır!");
                    // BURADA Error Log'a yazılmalı ve SMS - EMAIL Atılmalıdır.    
                    return RedirectToAction("Admin", new { isError = true });
                }
            }


            return RedirectToAction("Index");
        }
        public News AddRedisCache(News news, string cacheKey)
        {
            using (IRedisClient client = new RedisClient(conf))
            {
                var redisNews = client.Get<News>(cacheKey);
                if (redisNews == null)
                {
                    client.Set<News>(cacheKey, news);
                    redisNews = news;
                }
                return redisNews;
            }
        }
        public bool CheckDataStable(IRedisClient client, DateTime? updatedDate)
        {
            var redisNews = client.Get<News>("RedisNews");
            if (redisNews != null && redisNews.UpdatedDate.ToString() != updatedDate.ToString())
            {
                return false;
            }
            return true;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
