using System.Threading.Tasks;
using Lantern.Face.Parts.HTML;
using Lantern.Face.Parts;
namespace Lantern.Face.Parts {
	class ProgressBar : Part {

		private float _position = 0;
		public float Position {
			get => _position;
			set => _position = value;
		}

		public async override Task<string> RenderHTML() {
			var outer = new DivElement() {
				Classes = new[]{ "face-progressbar-out" }
			};
			outer.Append(new DivElement() {
				Classes = new[] { "face-progressbar-in" }
			});
			return await outer.RenderHTML();
		}
	}
}

