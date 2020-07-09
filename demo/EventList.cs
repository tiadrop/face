using System.Linq;
using Lantern.Face.Parts.HTML;

namespace Lantern.FaceDemo {
	public class EventLog : Element<EventListItem> {
		public EventLog() : base("ul") {
			Data["face-script"] = "demo.EventLog";
			// Data["session"] = session.Ident; // being a signed ID
		}
		public override string[] GetClientRequires() {
			var list = Content.GetClientRequires().ToList();
			list.Add("demo");
			return list.ToArray();
		}
	}


}