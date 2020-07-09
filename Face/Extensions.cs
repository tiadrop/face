using System.Threading.Tasks;
using Lantern.Face;
using Lantern.Face.Parts.HTML;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Lantern.Face {
	static class Extensions {

		public static string EscapeHTML(this string s) {
			return s.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;")
			.Replace("'", "&apos;")
			.Replace("\"", "&quot;"); //etc?
		}
		public static void Append(this StringBuilder stringBuilder, Part part) => stringBuilder.Append(part.RenderHTML());
		public static async Task WriteAsync(this HttpResponse response, Page page) {
			await response.WriteAsync(await page.RenderHTML());
		}

		public static async Task WritePage<T>(this HttpContext context) where T : Page, new() {
			var p = new T();
			await p.Prepare(context);
			await context.Response.WriteAsync(p);
			return;
		}

	}
}