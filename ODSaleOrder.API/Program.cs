using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ODSaleOrder.API
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var config = new ConfigurationBuilder()
                    .AddJsonFile(option =>
                    {
                        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                        if (env != null)
                        {
                            if (env.ToLower() == "prod")
                                option.Path = $"appsettings.prod.json";
                            else if (env.ToLower() == "qc")
                                option.Path = $"appsettings.qc.json";
                            else if (env.ToLower() == "development")
                                option.Path = $"appsettings.Development.json";
                            else
                                option.Path = "appsettings.json";

                            UpdateConnection(option.Path);
                        }
                        else
                            option.Path = "appsettings.json";
                        option.Optional = false;
                    })
                .Build();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        private static bool UpdateConnection(string file)
        {
            try
            {
                string linux = "/app/" + file;
                string new_cnn = Environment.GetEnvironmentVariable("CONNECTION");
                string regex = "(Server=)[a-zA-Z.01234567890;=_ ]{10,}";

                
                if (!File.Exists(linux) || string.IsNullOrEmpty(new_cnn))
                    return false;
                string conf = File.ReadAllText(linux);
                new_cnn = Regex.Replace(conf, regex, new_cnn);
                File.WriteAllText(linux, new_cnn);
                return true;
            }
            catch (Exception)
            {
                // Log.Fatal(ex, ex.Message);
                return false;
            }
        }
    }
}
