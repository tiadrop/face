using System.Net;
using System.Linq;
using System;
using Lantern.Face.Parts.Html;
using Lantern.Face.Parts;
using Lantern.Face;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
namespace Lantern.FaceDemo {

	class ErrorPage : Page {

		private int _errorCode;
		public ErrorPage(int errorCode) {
			_errorCode = errorCode;
		}

		private static Dictionary<int, string> codeDescriptions = new Dictionary<int, string> {
			[400] = "Bad Request",
			[401] = "Unauthorized",
			[402] = "Payment Required",
			[403] = "Forbidden",
			[404] = "Not Found",
			[500] = "Internal Server Error",
			// etc?
		};
		private static string getStatusDescription(int code) {
			return codeDescriptions.GetValueOrDefault(code, "Problem");
		}

		public override Task Prepare(HttpContext context) {
			Title = "ERROR | Face";
			Body.Classes.Add("errorPage");
			cssUrls.Add("/.Face.css");
			jsUrls.Add("/.Face.js");

			PartList parts = new PartList{
				new DivElement() {
					Classes = new[] { "panel" },
					Content = new Part[]{
						new HeadingElement(1){
							Content = new[]{ new PlainText("ERROR") },
						},
						new SectionElement(){
							Content = new Part[]{
								new LabelElement(){
									Content = new PlainText("0x" + _errorCode.ToString("X")),
								},
								new PlainText(getStatusDescription(_errorCode))
							},
						},
					}
				}
			};

			Body.Append(new StandardBody {
				Content = parts,
			});
			return Task.CompletedTask;
		}
	}
}