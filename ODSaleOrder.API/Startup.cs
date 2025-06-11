using DynamicSchema.Helper.Models.Header;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Services.Base;
using ODSaleOrder.API.Services.BaseLine;
using ODSaleOrder.API.Services.Ffa;
using ODSaleOrder.API.Services.Ffa.Interface;
using ODSaleOrder.API.Services.Inventory;
using ODSaleOrder.API.Services.Middleware;
using ODSaleOrder.API.Services.OneShop;
using ODSaleOrder.API.Services.OneShop.Interface;
using ODSaleOrder.API.Services.OrderStatusHistoryService;
using ODSaleOrder.API.Services.PrincipalService;
using ODSaleOrder.API.Services.SaleHistories;
using ODSaleOrder.API.Services.SaleOrder;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using ODSaleOrder.API.Services.SORecallService;
using ODSaleOrder.API.Services.TotalSalesToDate;
using ODSaleOrder.API.Services.TotalSalesToDate.Interface;
using ODSaleOrder.API.Services.Distributor;
using Serilog;
using Sys.Common;
using Sys.Common.Helper;
using Sys.Common.Models;
using SysAdmin.Models.SystemUrl;
using SysAdmin.Web.Services.SystemUrl;
using System;
using static SysAdmin.Models.StaticValue.CommonData;
using ODSaleOrder.API.Services.Manager;
using nProx.Helpers;
using ODSaleOrder.API.Services.Stock;
using ODSaleOrder.API.Services.DistributorOrder;
using ODSaleOrder.API.Services.CaculateTax;
using ODSaleOrder.API.Services.Dapper;


namespace ODSaleOrder.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public async void ConfigureServices(IServiceCollection services)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("Logs/log.txt",
            rollingInterval: RollingInterval.Month,
            outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
            long totalMemory = GC.GetTotalMemory(true);
            Serilog.Log.Information("Total Memory: " + totalMemory / 1024 + " MB");

            services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
            services.AddLogging();
            #region AutoMapper
            services.AddAutoMapper(typeof(Startup).Assembly);
            #endregion
            var connectStrings = Environment.GetEnvironmentVariable("CONNECTION");
            //connectStrings = "Server=db.rdos.online;Port=5494;Database=onesuat_system;User Id=postgresrdos;Password=RDOSGolive2022";
            // connectStrings = "Server=127.0.0.1;Port=9092;Database=rdos_system;User Id=postgresrdos;Password=PAssword65464";
            System.Diagnostics.Debug.WriteLine(connectStrings);
            CoreDependency.InjectDependencies(services, connectStrings);
            MinimalDepedency.InjectDependencies(services);
            services.AddHealthChecks();

            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddSingleton<IFirebaseHelper, FirebaseHelper>();
            services.AddDbContext<RDOSContext>(opt => opt.UseNpgsql(connectStrings));
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            #region local service
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<ISalesOrderService, SalesOrderService>();
            services.AddScoped<IReasonService, ReasonService>();
            services.AddScoped<ISaleOrderReturnService, SaleOrderReturnService>();
            services.AddScoped<ISumpickingService, SumpickingService>();
            services.AddScoped<IFfaSoOrderInformationService, FfaSoOrderInformationService>();
            services.AddScoped<ITotalSalesToDateService, TotalSalesToDateService>();
            services.AddScoped<IFfaSoOrderItemService, FfaSoOrderItemService>();
            services.AddScoped<ITempBudgetService, TempBudgetService>();
            services.AddScoped<IOrderStatusHistoryService, OrderStatusHistoryService>();
            services.AddScoped<ISORecallReqService, SORecallReqService>();
            services.AddScoped<ISyncCommonService, SyncCommonService>();
            services.AddScoped<ISORecallService, SORecallService>();
            services.AddScoped<IOSImportOrderService, OSImportOrderService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IBaseLineService, BaseLineService>();
            services.AddScoped<IOSNotificationService, OSNotificationService>(); 

            //Khoa enhance 
            services.AddScoped<IDistributorService, DistributorService>();
            services.AddScoped<IManagerInfoService, ManagerInfoService>();

            //DangMNN enhance
            services.AddScoped<ICalculateTaxService, CalculateTaxService>();

            // NK
            services.AddScoped<IStockService, StockService>();
            services.AddScoped<IFFASoSuggestOrderService, FFASoSuggestOrderService>();
            services.AddScoped<IDistributorSalesOrderService, DistributorSalesOrderService>();

            #endregion

            #region Temp_PromotionService
            services.AddScoped<IPromotionsService, PromotionsService>();
            services.AddScoped<IImportOrderService, ImportOrderService>();
            services.AddScoped<ISaleHistoriesService, SaleHistoriesService>();
            #endregion

            services.AddScoped<ISystemUrlService, SystemUrlService>();
            services.AddScoped<IPrincipalService, PrincipalService>();
            services.AddScoped<IDapperRepositories, DapperRepositories>();

            // Handle check site
            var serviceProvider = services.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                var principalSetting = scope.ServiceProvider.GetRequiredService<IPrincipalService>();
                principalSetting.IsODValidation().Wait();

                //if (Constant.IsODSiteConstant)
                //{
                //    services.AddSwaggerGen(c =>
                //    {
                //        c.OperationFilter<HeaderOperationFilter>();
                //    });
                //}

                services.AddSwaggerGen(c =>
                {
                    c.OperationFilter<HeaderOperationFilter>();
                });

                var systemUrlService = scope.ServiceProvider.GetRequiredService<ISystemUrlService>();
                SystemUrlListModel result = systemUrlService.GetAllSystemUrl().Result;
                SystemUrl = result.Items;
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider, RDOSContext _context)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OD.SaleOrderAPI v1"));
            app.UseSwagger(options => { options.RouteTemplate = "api-docs/{documentName}/docs.json"; });
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "api-docs";
                foreach (var description in provider.ApiVersionDescriptions)
                    options.SwaggerEndpoint($"/api-docs/{description.GroupName}/docs.json", description.GroupName.ToUpperInvariant());
            });
            app.UseRouting();
            ListServices services = new ListServices()
            {
                App = app,
                Env = env,
                Provider = provider
            };
            // _context.Database.Migrate();
            CoreDependency.Configure(services);
            MinimalDepedency.Configure(app, Environment.GetEnvironmentVariable("CONNECTION"));
            // global cors policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseAuthentication();
            app.UseMiddleware<HeaderMiddleware>();
            app.UseStaticFiles();


            app.UseRouting();
            app.UseHealthChecks("/ping");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
