// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;

namespace Mvc.RenderViewToString
{
    public class Program
    {
        public static void Main()
        {
            var serviceScopeFactory = InitializeServices();
            var emailContent = RenderViewAsync(serviceScopeFactory).Result;

            Console.WriteLine(emailContent);
        }

        public static IServiceScopeFactory InitializeServices(string customApplicationBasePath = null)
        {
            // Initialize the necessary services
            var services = new ServiceCollection();
            ConfigureDefaultServices(services, customApplicationBasePath);

            // Add a custom service that is used in the view.
            services.AddSingleton<EmailReportGenerator>();

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IServiceScopeFactory>();
        }

        public static Task<string> RenderViewAsync(IServiceScopeFactory scopeFactory)
        {
            using (var serviceScope = scopeFactory.CreateScope())
            {
                var helper = serviceScope.ServiceProvider.GetRequiredService<RazorViewToStringRenderer>();

                var model = new EmailViewModel
                {
                    UserName = "User",
                    SenderName = "Sender",
                    UserData1 = 1,
                    UserData2 = 2
                };

                return helper.RenderViewToStringAsync("Views/EmailTemplate.cshtml", model);
            }
        }

        private static void ConfigureDefaultServices(IServiceCollection services, string customApplicationBasePath)
        {
            string applicationName;
            string rootPath;

            if (!string.IsNullOrEmpty(customApplicationBasePath))
            {
                applicationName = Path.GetFileName(customApplicationBasePath);
                rootPath = customApplicationBasePath;
            }
            else
            {
                applicationName = Assembly.GetEntryAssembly().GetName().Name;
                rootPath = Directory.GetCurrentDirectory();
            }

            var fileProvider = new PhysicalFileProvider(rootPath);

            var environment = new CustomHostingEnvironment
            {
                WebRootFileProvider = fileProvider,
                ApplicationName = applicationName,
                ContentRootPath = rootPath,
                WebRootPath = rootPath,
                EnvironmentName = "DEVELOPMENT",
                ContentRootFileProvider = fileProvider
            };

            services.AddSingleton<IWebHostEnvironment>(environment);

            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddSingleton<DiagnosticSource>(diagnosticSource);
            services.AddSingleton<DiagnosticListener>(diagnosticSource);
            services.AddLogging();
            services.AddMvcCore()
                .AddRazorViewEngine();
            services.AddTransient<RazorViewToStringRenderer>();
        }
    }

    public class CustomHostingEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
