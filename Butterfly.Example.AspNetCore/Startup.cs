using System;
using System.Threading.Tasks;
using Butterfly.Core.Database.Memory;
using Butterfly.Example.AspNetCore.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Butterfly.Example.AspNetCore
{
	public class Startup
	{
		private WebSocketsSubscriptionApi WebSocketsSubscriptionApi { get; set; } = new WebSocketsSubscriptionApi();
		private WebSocketsChannelConnection WebSocketsChannelConnection { get; set; }
		private MemoryDatabase MemoryDatabase { get; set; }

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

			MemoryDatabase = new MemoryDatabase();
			Task.WaitAll(MemoryDatabase.CreateFromSqlAsync(@"CREATE TABLE todo (
	                id VARCHAR(50) NOT NULL,
	                name VARCHAR(40) NOT NULL,
	                PRIMARY KEY(id)
                );"));
			MemoryDatabase.SetDefaultValue("id", (tableName) =>
			{
				return $"{tableName}_{Guid.NewGuid().ToString()}";
			});

			// register in IoC
			services.AddSingleton(MemoryDatabase);

			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
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

			// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-2.1#how-to-use-websockets
			app.Use(async (context, next) =>
			{
				if (context.Request.Path != "/ws"){
					await next();
					return;
				}

				if (!context.WebSockets.IsWebSocketRequest) {
					context.Response.StatusCode = 400;
					return;
				}
				
				var webSocket = await context.WebSockets.AcceptWebSocketAsync();

				if (WebSocketsChannelConnection == null)
					WebSocketsChannelConnection = new WebSocketsChannelConnection(MemoryDatabase, WebSocketsSubscriptionApi);

				await WebSocketsChannelConnection.ReceiveAsync(context, webSocket);
			});
		}
	}
}
