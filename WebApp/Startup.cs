using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using WebApp.Infrastructure.Messaging;
using WebApp.Models;
using WebApp.RESTClients;
using WebApp.Saga;
using WebApp.ViewModels;

namespace WebApp
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
            services.AddControllersWithViews();

            services.AddTransient<IMessagePublisher, MessagePublisher>();

            services.AddSingleton<ISagaMemoryStorage, SagaMemoryStorage>();

            services.AddTransient<ISagaOrchestratorBackgroundService, SagaOrchestratorBackgroundService>();
            
            services.AddTransient<ICustomerManagementAPI, CustomerManagementAPI>();
            services.AddTransient<IMaintenanceManagementAPI, MaintenanceManagementAPI>();

            services.AddHostedService<SagaOrchestratorBackgroundService>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddFile("Logs/GMS-webapp-{Date}.txt");
            
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
            SetupAutoMapper();
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void SetupAutoMapper()
        {
            // setup automapper
            var cfg = new AutoMapper.Configuration.MapperConfigurationExpression();
            cfg.CreateMap<HotelRegisterVM, Hotel>();
            cfg.CreateMap<CustomerRegisterVM, Customer>();
            cfg.CreateMap<VehicleRegisterVM, Vehicle>();
            cfg.CreateMap<PlanMaintenanceJobVM, MaintenanceJob>();
            Mapper.Initialize(cfg);
        }

    }
}
