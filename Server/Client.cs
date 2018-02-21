using System.Runtime.InteropServices.ComTypes;
using Serac.WebSockets;
using System.Threading.Tasks;
using static System.Console;

namespace WebArena {
	public class Client : AsyncProtocolHandler {
		readonly WebSocket Socket;
		public Client(WebSocket socket) {
			Socket = socket;
		}

		public async Task Run() {
			await SendMessage(new MapChange {Path = "maps/q3tourney2.json"});
			await Task.Delay(15000);
			await SendMessage(new MapChange {Path = "maps/q3dm4.json"});
			while(true) {
				var msg = ParseMessage(await Socket.ReadBinary());
				WriteLine($"Got message '{msg}'");
				await Handle(msg);
			}
		}
		
		protected async override Task Handle(MapChange msg) {
			WriteLine($"Got map change from ... client? {msg}");
		}
		
		protected override async Task Send(byte[] data) => await Socket.Write(data);
	}
}