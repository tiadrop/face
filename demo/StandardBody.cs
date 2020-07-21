using System.Threading.Tasks;
using Lantern.Face;
using Lantern.Face.Parts.Html;

namespace Lantern.FaceDemo {

	class StandardBody : Part {
		private Header _header = new Header();

		public string ActiveHRef{
			get => _header.ActiveHRef;
			set => _header.ActiveHRef = value;
		}

		private DivElement _rootDiv = new DivElement {
			Classes = new[] { "r00t" }
		};

		public PartList<Part> Content {
			get => _rootDiv.Content;
			set => _rootDiv.Content = value;
		}

		public override async Task<string> RenderHtml() {
			var s = await _header.RenderHtml();
			foreach(var part in Content) s += await part.RenderHtml();
			return s;
		}

		public override string[] GetClientRequires(){
			UniqueList<string> requires = new UniqueList<string>();
			foreach (Part part in Content) {
				foreach (string name in part.GetClientRequires()) {
					requires.Add(name);
				}
			}
			return requires.ToArray();
		}

	}

}