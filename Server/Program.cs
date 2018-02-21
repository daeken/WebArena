using Serac;
using Serac.Static;
using Serac.WebSockets;
using static System.Console;

namespace WebArena {
	class Program {
		static void Main(string[] args) {
			new WebServer()
				.EnableCompression()
				.EnableStaticCache()
				.WebSocket("/socket", async ws => await new Client(ws).Run())
				.Static("/assets", "../Converter/output/")
				.StaticFile("/", "../WebArena/bin/Debug/bridge/index.html")
				.Static("/", "../WebArena/bin/Debug/bridge/")
				.ListenOn(8080)
				.RunForever();
		}
	}
}