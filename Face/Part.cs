using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Lantern.Face.Parts.Html;

namespace Lantern.Face {

	public abstract class Part {
		public abstract Task<string> RenderHtml();
		public virtual string[] GetClientRequires() => new string[] { };

		public static implicit operator Part(string s) => new PlainText(s);

		public virtual string RenderJson() {
			throw new NotImplementedException();
		}
	}

	public class PartList<T> : List<T> where T : Part {
		public PartList(IEnumerable<T> parts = null) {
			if (parts != null) this.AddRange(parts);
		}
		public async Task<string> RenderHtml() {
			var renderTasks = this.Select(part => part.RenderHtml());
			// nested parallel async rendering feels nice
			var strings = await Task.WhenAll(renderTasks);
			return string.Join("", strings);
		}
		public static implicit operator PartList<T>(T[] parts) => new PartList<T>(parts);
		public static implicit operator PartList<T>(T part) => new PartList<T> { part };
		public static implicit operator PartList<T>(string s) => new PartList<T> { (T)s };

		public string[] GetClientRequires(){
			UniqueList<string> requires = new UniqueList<string>();
			foreach (var part in this) {
				foreach (string name in part.GetClientRequires()) {
					requires.Add(name);
				}
			}
			return requires.ToArray();
		}

	}



	public class PartList : PartList<Part> { }



}