using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TGSentry.Infra.Contract;
using TGSentry.Logic;
using TGSentry.Logic.Contract;

namespace TGSentry.ConsoleApp
{
    public class Startup
    {
        public async Task InitAndRun(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();

            using var serviceScope = host.Services.CreateScope();
            var provider = serviceScope.ServiceProvider;

            var notificator = provider.GetRequiredService<INotificator>();
            
            
            await notificator.SendMessage($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] Buon giorno!");
        }
        private IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostBuilderContext, services) =>
                {
                    SetLogging(services);
                    
                    SetConfiguration(args, services);
                    
                    var types = InitializeAndGetTypes();
                    foreach (var type in types)
                    {
                        if (!type.IsGenericTypeDefinition)
                        {
                            AutoRegister(type, services);
                        }
                    }
                });
        }

        private static void SetLogging(IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
        }

        private static void SetConfiguration(string[] args, IServiceCollection services)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            services.AddOptions()
                .Configure<TelegramSettings>(configuration.GetSection("TelegramSettings"));
        }

        private void AutoRegister(Type type, IServiceCollection serviceCollection)
        {
            if (typeof(IScoped).IsAssignableFrom(type))
            {
                var interfaces = type.GetInterfaces().Where(x => x.Name is not nameof(IScoped)).ToList();

                foreach (var @interface in interfaces)
                {
                    serviceCollection.AddScoped(@interface, type);
                }
            }

            if (typeof(ITransient).IsAssignableFrom(type))
            {
                var interfaces = type.GetInterfaces().Where(x => x.Name is not nameof(ITransient)).ToList();

                foreach (var @interface in interfaces)
                {
                    serviceCollection.AddTransient(@interface, type);
                }
                
            }
            
            if (typeof(ISingleton).IsAssignableFrom(type))
            {
                var interfaces = type.GetInterfaces().Where(x => x.Name is not nameof(ISingleton)).ToList();

                foreach (var @interface in interfaces)
                {
                    serviceCollection.AddSingleton(@interface, type);
                }
            }
        }


        private Type[] InitializeAndGetTypes()
        {
            var types = DependencyContext
                .Default
                .RuntimeLibraries
                .Where(x => x.Name.ToLower().Contains("tgsentry"))
                .Select(LoadLibrary)
                .Where(x => x != null)
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass &&
                            !x.IsAbstract &&
                            !x.GetCustomAttributes<CompilerGeneratedAttribute>(true).Any() &&
                            !x.GetTypeInfo().IsSubclassOf(typeof(Delegate)))
                .OrderBy(x => x.FullName)
                .ToArray();
            return types;
        }
        
        private Assembly LoadLibrary(RuntimeLibrary library)
        {
            try
            {
                return Assembly.Load(new AssemblyName(library.Name));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error loading assembly {library.Name}: {e}");
                return null;
            }
        }
        
    }
}