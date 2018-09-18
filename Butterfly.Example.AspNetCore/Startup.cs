using System;
using System.Threading.Tasks;
using Butterfly.Core.Database.Memory;
using Butterfly.Example.AspNetCore.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Butterfly.Example.AspNetCore
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
			services.AddCors(setup =>
			{
				setup.AddPolicy("CorsPolicy",
					builder => builder.WithOrigins("*")
						.AllowAnyMethod()
						.AllowAnyHeader()
						.AllowCredentials());
			});

			ConfigureButterflyServices(services);

			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
		}

		private void ConfigureButterflyServices(IServiceCollection services)
		{
			var memoryDatabase = new MemoryDatabase();
			Task.WaitAll(memoryDatabase.CreateFromSqlAsync(@"CREATE TABLE todo (
	                id VARCHAR(50) NOT NULL,
	                name VARCHAR(40) NOT NULL,
	                PRIMARY KEY(id)
                );"));
			memoryDatabase.SetDefaultValue("id", (tableName) =>
			{
				return $"{tableName}_{Guid.NewGuid().ToString()}";
			});

			// register for IoC
			services.AddSingleton(memoryDatabase);
			services.AddSingleton<WebSocketsSubscriptionApi>();
			services.AddSingleton<WebSocketsChannelConnection>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseCors("CorsPolicy");

			app.UseMvc();

			var webSocketOptions = new WebSocketOptions()
			{
				KeepAliveInterval = TimeSpan.FromSeconds(120),
				ReceiveBufferSize = 4 * 1024
			};
			app.UseWebSockets(webSocketOptions);
			app.Map("/ws", WebSocketsMiddleware.WebSocketHandler);			
		}
	}
}
