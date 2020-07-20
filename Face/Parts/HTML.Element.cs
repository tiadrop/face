using System.Threading;
using System.Collections;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Lantern.Face;

namespace Lantern.Face.Parts.Html {

	public abstract class Element<TChild> : Part where TChild : Part {

		public string Hint {
			get => Attribs["title"];
			set => Attribs["title"] = value;
		}

		public string ID {
			get {
				if(!Attribs.Keys.Contains("id")){
					var bytes = new byte[16];
					rnd.NextBytes(bytes);
					Attribs["id"] = Convert.ToBase64String(bytes);
				}
				return Attribs["id"];
			}
			set => Attribs["id"] = value;
		}

		private Random rnd = new Random();
		private static string[] noClose = { "input", "br", "hr", "link", "base", "meta", "body" };
		// we're dropping </body> because it's optional (by spec); nothing will follow it; omitting </html> for same
		public Element.Attributes Attribs = new Element.Attributes();
		private Element.DataAttributes _data; // set in constructor; requires (wraps) _attribs
		private UniqueList<string> _booleanAttribs = new UniqueList<string>();
		public Element.ClassList _classes = new Element.ClassList();

		public PartList<TChild> Content = new PartList<TChild>();

		public UniqueList<string> BooleanAttribs = new UniqueList<string>();

		public class ClassList : UniqueList<string> {
			public static implicit operator ClassList(string s) => new ClassList(s);
			public static implicit operator ClassList(string[] s) => new ClassList(s);
			public ClassList() { }
			public ClassList(string spaceSeparatedClasses) {
				foreach (var item in spaceSeparatedClasses.Split(" ")) {
					Add(item);
				}
			}
			public ClassList(string[] s) => AddRange(s);
			public override string ToString() => string.Join(' ', this); 
		}

		public ClassList Classes = new ClassList();

		public Element.IAttributes Data {
			get => _data;
			set{
				_data.Clear();
				foreach(var k in value.Keys) _data[k] = value[k];
			}
		}

		private bool isNoClose => noClose.Contains(Tag);
		public readonly string Tag;
		public Element(string tag){
			this.Tag = tag;
			_data = new Element.DataAttributes(this.Attribs);
		}

		public string Name {
			get => Attribs["name"];
			set => Attribs["name"] = value;
		}

		public void Append(TChild part) {
			this.Content.Add(part);
		}

		public override async Task<string> RenderHTML() {
			StringBuilder s = new StringBuilder();
			s.Append($"<{Tag}");
			if(Classes.Count > 0){
				s.Append(" class=\"");
				s.Append(Classes.ToString());
				s.Append("\"");
			}
			
			if(Attribs.Keys.Count() > 0) s.Append($" {string.Join(" ", Attribs.Select(a => $"{a.Key.EscapeHTML()}=\"{a.Value.EscapeHTML()}\""))}");
			if(BooleanAttribs.Count() > 0) s.Append(" " + string.Join(" ", BooleanAttribs));
			s.Append(">");
			s.Append(await Content.RenderHTML());
			if (isNoClose) return s.ToString();
			s.Append("</" + Tag + ">");
			return s.ToString();
		}

		public override string[] GetClientRequires() => Content.GetClientRequires();

	}


	public abstract class Element : Element<Part> {

		public interface IAttributes : IEnumerable<KeyValuePair<string, string>> {
			public string this[string key] { get; set; }
			public string[] Keys { get; }
			public bool Remove(string s);
		}

		public class DataAttributes : IAttributes {
			private IAttributes attribs;
			public DataAttributes(IAttributes source) {
				this.attribs = source;
			}
			public string this[string key] {
				get => attribs["data-" + key];
				set => attribs["data-" + key] = value;
			}
			public void Clear(){
				foreach(var k in attribs.Keys){
					if(k.Substring(0, 5) == "data-") attribs.Remove(k);
				}
			}
			public bool Remove(string key) => attribs.Remove("data-" + key);
			public string[] Keys => attribs.Keys.Where(k => k.Length > 5 && k.Substring(0, 5) == "data-").Select(k => k.Substring(5)).ToArray();


			public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => attribs.GetEnumerator();
			IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator() => GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		public class Attributes : IAttributes {
			Dictionary<string, string> _data = new Dictionary<string, string>();
			public string[] Keys {
				get {
					return _data.Keys.ToArray();
				}
			}
			public string this[string key] {
				get => _data[key];
				set => _data[key] = value;
			}
			public bool Remove(string s) => _data.Remove(s);

			public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _data.GetEnumerator();
			IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator() => GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		public Element(string tag) : base(tag) { }
	}


}