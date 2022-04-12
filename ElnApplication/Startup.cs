using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ElnApplication
{
    public class Startup
    {
        public readonly string allowSpecificOrigins = "_allowSpecificOrigins";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            Constants.PPPort = configuration.GetValue<string>("Ports:PPPort");
            Constants.PPSamplePath = configuration.GetValue<string>("Urls:PPSamplePath");
            Constants.OracleConnectionStringEln = configuration.GetValue<string>("Databases:Eln");
            Constants.OracleConnectionStringElnIf = configuration.GetValue<string>("Databases:ElnIF");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // 세션 설정
            /*services.AddSession(options =>
            {
                options.Cookie.Name = ".ElnApplication.Session"; // 디폴트 : .AspNetCore.Session
                //options.IdleTimeout = TimeSpan.FromSeconds(10);
                //options.Cookie.IsEssential = true;
            });*/
            services.AddSession();

            // cors 설정
            /*services.AddCors(options =>
            {
                options.AddPolicy(allowSpecificOrigins,
                builder =>
                {
                    builder.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                });
            });*/
            //services.AddMemoryCache();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddControllersWithViews()
                    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null); // 추가 json 카멜표기로 강제 변환 해제
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //app.UseCors(allowSpecificOrigins); // cors 에러 : Access to XMLHttpRequest at has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present on the requested resource.

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseSession(); // 세션 설정

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
