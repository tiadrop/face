using Lantern.Face;
using System.Threading.Tasks;
namespace Lantern.Face.Parts.HTML {
	class RawHTML : Part {
		private string _s;
		public RawHTML(string html) {
			_s = html;
		}
		public override Task<string> RenderHTML() => Task.FromResult(_s);
	}

}