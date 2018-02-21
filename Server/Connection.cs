using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serac.WebSockets;
using static System.Console;

namespace WebArena {
	public class Connection : IWebSocket {
		readonly WebSocket Socket;

		public Connection(WebSocket socket) {
			Socket = socket;
		}

		public async Task Send(byte[] data) => await Socket.Write(data);
	}
}