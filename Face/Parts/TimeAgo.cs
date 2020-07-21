using System;
using Lantern.Face.Parts.Html;

namespace Lantern.Face.Parts {
	class TimeAgo : SpanElement {
		public TimeAgo(Int64 timestamp){
			Data = new Attributes{
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