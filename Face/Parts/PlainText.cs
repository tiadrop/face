using System.Threading.Tasks;
namespace Lantern.Face.Parts.Html {
	public class PlainText : Part {
		public readonly string Text;
		public override Task<string> RenderHTML(){
			return Task.FromResult(Text.EscapeHTML());
		}

		public PlainText(string s = ""){
			this.Text = s;
		}

		public static implicit operator PlainText(string s) => new PlainText(s);

	}


}

