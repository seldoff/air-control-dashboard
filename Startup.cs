using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AirControlDashboard
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddMetrics(AppMetrics.CreateDefaultBuilder().Build());
            services.Configure<AirControlOptions>(Configuration.GetSection("AirControl"));
            services.AddHostedService<AirControlService>();
            services.AddLogging(b => b.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss "));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseMetricsEndpoint(new MetricsPrometheusTextOutputFormatter());
        }
    }
}
