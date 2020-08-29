using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Lantern.Face.Json;
using Microsoft.AspNetCore.Http;
using Lantern.Face.Parts.Html;

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
		protected string Title = "";
		protected readonly UniqueList<string> CssUrls = new UniqueList<string>();
		protected readonly List<string> JsUrls = new List<string>();

		public virtual Task Prepare(HttpContext context) { return Task.CompletedTask; }

		protected readonly Element Body = new BodyElement();

		public async Task<string> RenderHtml(){
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
			foreach(var url in CssUrls) headSection.Append(new OtherElement("link") {
				Attribs = new Element.Attributes{
					["rel"] = "stylesheet",
					["type"] = "text/css",
					["href"] = url
				}
			});

			string[] clientRequires = Body.GetClientRequires();
			headSection.Append(new RawHtml(String.Join("", JsUrls.Select(url 
				=> $"<script src=\"{url}\"></script>"
			).ToArray())));
			// todo: combine requires with jsUrls; hard insert <script .Face.js> if requires.length > 0
			if(clientRequires.Length > 0){
				headSection.Append(new OtherElement("script") {
					Content = new RawHtml($"face.require({String.Join(",", clientRequires.Select(src => src.ToJson()))});setTimeout(face.apply,1)")
				});
			}


			var sb = new StringBuilder();
			sb.Append("<!DOCTYPE html><html>");
			sb.Append(await headSection.RenderHtml());
			sb.Append(await Body.RenderHtml());
			var s = sb.ToString();
			return s;
		}

	}
}