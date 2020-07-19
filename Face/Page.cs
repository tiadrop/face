using System.Runtime.CompilerServices;
using System.Reflection;
using System.Net;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Lantern.Face.Parts;
using Lantern.Face.Parts.HTML;

namespace Lantern.Face {
	public class UniqueList<T> : IEnumerable<T> {
		private List<T> _list = new List<T>();
		public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public bool this[T key] {
			get => _list.Contains(key);
			set => Toggle(key, value);
		}

		public static implicit operator UniqueList<T>(T[] items){
			var ul = new UniqueList<T>();
			foreach (var item in items){
				ul.Add(item);
			}
			return ul;
		}
		
		public bool Add(T s){
			if (!Contains(s)) {
				_list.Add(s);
				return true;
			}
			return false;
		}
		public bool Remove(T s) => _list.Remove(s);

		public bool Contains(T s) => _list.Contains(s);
		public void Toggle(T s){
			if(Contains(s)){ Remove(s); }
			else Add(s);
		}
		public void Toggle(T s, bool forceTo){ // matches JS's ClassList#toggle overloads
			if(forceTo){ Add(s); }
			else Remove(s);
		}
		public int Count => _list.Count;

		public void Clear() => _list.Clear();
		public void AddRange(IEnumerable<T> source) => _list.AddRange(source);

		public T[] ToArray() => _list.ToArray();

	}

	public abstract class Page {

		public string Title = "";
		protected UniqueList<string> cssUrls = new UniqueList<string>();
		protected List<string> jsUrls = new List<string>();

		public virtual Task Prepare(HttpContext context) { return Task.CompletedTask; }

		public readonly Element Body = new BodyElement();

		public async Task<string> RenderHTML(){
			var headSection = new OtherElement("head") {
				Content = new Part[]{
					new OtherElement("meta"){
						Attribs = new Element.Attributes(){
							["charset"] = "UTF-8"
						}
					},
					new OtherElement("title"){
						Content = Title
					},
					new OtherElement("link"){
						Attribs = new Element.Attributes{
							["rel"] = "shortcut icon",
							["type"] = "image/png",
							["href"] = "/favicon.png"		
						}
					}
				}
			};
			foreach(var url in cssUrls) headSection.Append(new OtherElement("link") {
				Attribs = new Element.Attributes{
					["rel"] = "stylesheet",
					["type"] = "text/css",
					["href"] = url
				}
			});
			
			var sb = new StringBuilder();
			sb.Append("<!DOCTYPE html><html>");
			sb.Append(await headSection.RenderHTML());
			sb.Append(String.Join("", jsUrls.Select(url => $"<script src=\"{url}\"></script>").ToArray()));

			string[] clientRequires = Body.GetClientRequires();
			if(clientRequires.Length > 0){
				sb.Append($"<script>face.require(");
				sb.Append(String.Join(", ", clientRequires.Select(s => $"\"{s}\"")));
				sb.Append(");</script>");
			}
			sb.Append(await Body.RenderHTML());
			var s = sb.ToString();
			return s;
		}

	}
}