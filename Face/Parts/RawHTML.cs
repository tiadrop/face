using System.Threading.Tasks;

namespace Lantern.Face.Parts.Html {
	class RawHtml : Part {
		private readonly string _s;
		public RawHtml(string html) {
			_s = html;
		}
		public override Task<string> RenderHtml() => Task.FromResult(_s);
	}

}