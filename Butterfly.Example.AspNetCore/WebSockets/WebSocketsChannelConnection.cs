using Butterfly.Core.Channel;
using Butterfly.Core.Database.Memory;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Butterfly.Example.AspNetCore.WebSockets
{
	public class WebSocketsChannelConnection : BaseChannelConnection
	{
		public WebSocketsChannelConnection(MemoryDatabase memoryDatabase, WebSocketsSubscriptionApi subscriptionApi) : base(subscriptionApi)
		{
			_memoryDatabase = memoryDatabase;
			_channel = new Channel(this, "todos", null);
		}
		
		protected override async Task SendAsync(string text)
		{
			var msg = Encoding.UTF8.GetBytes(text);
			foreach (var webSocket in WebSockets)
			{
				await webSocket.SendAsync(new ArraySegment<byte>(msg, 0, msg.Length), WebSocketMessageType.Text, true, CancellationToken.None);
			}
		}
		
		private readonly Channel _channel;
		private readonly MemoryDatabase _memoryDatabase;
		private List<WebSocket> WebSockets { get; set; } = new List<WebSocket>();
		
		public async Task ReceiveAsync(HttpContext context, WebSocket webSocket)
		{
			WebSockets.Add(webSocket);
			var buffer = new byte[1024 * 4];
			
			WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
			
			while (!result.CloseStatus.HasValue)
			{
				var text = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
				await ReceiveMessageAsync(text);
				
				if (text.Contains("Subscribe:"))
					await _memoryDatabase.CreateAndStartDynamicViewAsync("todo", dataEventTransaction =>
					{
						_channel.Queue(dataEventTransaction);
					});

				result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
			}
			WebSockets.Remove(webSocket);
			await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
		}
	}
}
