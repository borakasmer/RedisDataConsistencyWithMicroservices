using System;
using ServiceStack.Redis;
using Newtonsoft.Json;

namespace redisnewsservices
{
    class Program
    {
        static void Main(string[] args)
        {
            var conf = new RedisEndpoint() { Host = "127.0.0.1", Port = 6379 };
            Console.WriteLine("Services Start...");
            using (IRedisClient client = new RedisClient(conf))
            {
                IRedisSubscription sub = null;
                using (sub = client.CreateSubscription())
                {
                    sub.OnMessage += (channel, news) =>
                    {
                        try
                        {
                            News _news = JsonConvert.DeserializeObject<News>(news);
                            Console.WriteLine(_news.Title);
                            
                            //Güncellenecek 1. Redis Server'ı
                            var conf2 = new RedisEndpoint() { Host = "10.211.55.9", Port = 6379 };
                            using (IRedisClient clientServer = new RedisClient(conf2))
                            {
                                clientServer.Set<News>("RedisNews", _news);                                
                            }

                            //Güncellenecek 2. Redis Server'ı
                            var conf3 = new RedisEndpoint() { Host = "192.168.1.234", Port = 6379 };
                            using (IRedisClient clientServer2 = new RedisClient(conf3))
                            {
                                clientServer2.Set<News>("RedisNews", _news);                                
                            }
                            
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Hata :"+ex.Message);
                        }
                    };
                    sub.SubscribeToChannels(new string[] { "News" });
                }
            }
            Console.ReadLine();
        }
    }
}
