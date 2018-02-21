using System.Threading.Tasks;

namespace WebArena {
	public interface IWebSocket {
		Task Send(byte[] data);
	}
}