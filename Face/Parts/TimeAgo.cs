using System;
using System.Threading.Tasks;
using Lantern.Face;
using Lantern.Face.Parts.HTML;
namespace Lantern.Face.Parts {
	class TimeAgo : SpanElement {
		public TimeAgo(Int64 timestamp){
			Data = new Element.Attributes{
				["face-script"] = "Face.Parts.TimeAgo",
				["timestamp"] = timestamp.ToString()
			};
		}

		public override string[] GetClientRequires(){
			return new[]{
				"Face.Parts.General"
			};
		}

	}
}