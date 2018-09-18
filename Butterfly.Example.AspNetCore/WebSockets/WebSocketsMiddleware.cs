using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Butterfly.Example.AspNetCore.WebSockets
{
	public class WebSocketsMiddleware
	{
		public static void WebSocketHandler(IApplicationBuilder app)
		{
			app.Run(async context =>
			{
				if (!context.WebSockets.IsWebSocketRequest)
				{
					context.Response.StatusCode = 400;
					return;
				}

				var webSocket = await context.WebSockets.AcceptWebSocketAsync();
				var webSocketsChannelConnection = context.RequestServices.GetService<WebSocketsChannelConnection>();

				await webSocketsChannelConnection.ReceiveAsync(context, webSocket);
			});
		}
	}
}
