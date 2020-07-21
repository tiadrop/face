using Lantern.Face;
using Lantern.Face.Parts.Html;

namespace Lantern.FaceDemo {
	public class EventListItem : ListItemElement {
		private LabelElement _label = new LabelElement();
		private Element _article = new ArticleElement();

		public EventListItem() {
			base.Content = new PartList { _label, _article };
			Label = ""; // ensure a _label[0].Content[0] exists, referenced in Label get
		}

		public new PartList<Part> Content {
			get => _article.Content;
			set => _article.Content = value;
		}

		public string Label {
			get {
				PlainText pt = (PlainText)_label.Content[0];
				return pt.Text;
			}
			set => _label.Content = new PlainText(value);
		}

		public override string[] GetClientRequires(){
			return Content.GetClientRequires();
		}

	}
}