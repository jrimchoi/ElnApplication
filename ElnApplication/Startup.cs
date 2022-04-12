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
            // ���� ����
            /*services.AddSession(options =>
            {
                options.Cookie.Name = ".ElnApplication.Session"; // ����Ʈ : .AspNetCore.Session
                //options.IdleTimeout = TimeSpan.FromSeconds(10);
                //options.Cookie.IsEssential = true;
            });*/
            services.AddSession();

            // cors ����
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
                    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null); // �߰� json ī��ǥ��� ���� ��ȯ ����
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //app.UseCors(allowSpecificOrigins); // cors ���� : Access to XMLHttpRequest at has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present on the requested resource.

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

            app.UseSession(); // ���� ����

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
