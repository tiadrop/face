using System.Collections.Generic;
using System.Threading.Tasks;
using Lantern.Face;
using Lantern.Face.Parts.HTML;

namespace Lantern.FaceDemo {

	public class Header : Part {

		private string _activeHRef;

		private List<AnchorElement> _links;
		private NavigationElement nav;

		public Header(){
			_links = new List<AnchorElement>(){
				new AnchorElement(){
					Attribs = new Element.Attributes(){ ["href"] = "/" },
					Content = new PlainText("Central"),
					Classes = new[] { "titleLink" },
				},
				new AnchorElement(){
					Attribs = new Element.Attributes(){ ["href"] = "/ops" },
					Content = new PlainText("Ops")
				},
				new AnchorElement(){
					Attribs = new Element.Attributes(){ ["href"] = "/converse" },
					Content = new PlainText("Converse")
				},
			};

			nav = new NavigationElement() {
				Content = _links.ToArray(),
			};
		}

		public override async Task<string> RenderHTML() {
			return await nav.RenderHTML();
		}

		public string ActiveHRef {
			get => _activeHRef;
			set {
				foreach(var link in _links){
					link.Classes.Toggle("active", link.HRef == value );
				}
				_activeHRef = value;
			}
		}


	}
}