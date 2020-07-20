using Lantern.Face;
using Lantern.Face.Parts.Html;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Lantern.FaceDemo {

	class OpsPage : Page {
		public override Task Prepare(HttpContext context) {
			Title = "Ops";
			cssUrls.Add("/.Face.css");
			jsUrls.Add("/.Face.js");

			PartList parts = new PartList{
				new HeadingElement(1) {
					Content = new PlainText("Ops")
				},
				new PlainText("Here's an image"),
				new BreakElement(),
				new ImageElement() {
					Classes = new[] { "crabs" },
					SourceUrl = "https://www.citarella.com/media/catalog/product/cache/1/image/97a78116f02a369697db694bbb2dfa59/0/2/024029200000_01_1.jpg"
				},
				":D"
			};

			parts.Add(":D");

			Body.Append(new StandardBody{
				Content = parts,
				ActiveHRef = "/ops"
			});
			return Task.CompletedTask;
		}
	}
}