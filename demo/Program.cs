using System.IO;
using System;
using System.Threading.Tasks;
using face.demo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Lantern.Face;
using Lantern.Face.Parts;
using Lantern.Face.Parts.Html;

namespace Lantern.FaceDemo {

	public class Server {
		public static readonly string LibName = "Face";
		public static readonly string LibShortName = "Face";

		public static void Start(){
			CreateHostBuilder(new string[] { }).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder => {
				webBuilder.UseStartup<Startup<App>>();
			});
	}


	public class App {
		public struct Configuration {
			public int Port;
			public string ResourcePath;
		}
		private Configuration _options;
		public Configuration Options => _options;
		public App() {
			_options = GetOptions();
		}

		public Task Error403(HttpContext context) => HTTPError(context, 403, "Forbidden");
		public Task Error404(HttpContext context) => HTTPError(context, 404, "Not Found");
		public Task Error500(HttpContext context) => HTTPError(context, 500, "Internal Server Error");

		public string GetResourcePath(string filename) => Path.Combine(Options.ResourcePath, filename);
		public string GetResourcePath(string subdir, string filename) => Path.Combine(Options.ResourcePath, subdir, filename);

		public Configuration GetOptions() {
			return new Configuration {
				ResourcePath = "demo",
				// Port = 818
			};
		}
		public async Task Handle(HttpContext context) {
			Page page = new ErrorPage(404);
			await page.Prepare(context);
			await context.Response.WriteAsync(await page.RenderHtml());
		}

		public static async Task Respond(HttpContext context, Page page){
			await page.Prepare(context);
			await context.Response.WriteAsync(await page.RenderHtml());
		}

		public delegate EventListItem GetEvent();
		public static GetEvent[] RandomEventFactoryPool = new GetEvent[]{
			() => new EventListItem{ Label = "KNotify", Content = "Download complete: Ainsley Harriot's Ultimate Reddy-Brek Recipies.pdf"},
			() => new EventListItem{ Label = "Authorised", Content = "Your request for skynet5 repository access was accepted"},
			() => new EventListItem{
				Label = "Message",
				Content = new RawHtml("Received a <a href='converse/rickyg'>message</a> from <em>Ricky G</em>"),
			},
			() => new EventListItem{ Label = "Project Y", Content = "The rabbit is in the hutch", Classes = "projectY"},
			() => new EventListItem{
				Label = "Incoming call",
				Classes = new[]{ "priority" },
				Content = new Part[] {
					new RawHtml("<em>Johnny Ten</em>, unscheduled<br>"),
					new DelegateButton{
						OnClick = (context, post) => {
							Console.WriteLine("Connecting call");
							return Task.CompletedTask;
						},
						Once = true,
						TimeToLiveSeconds = 90, // 0 = until ping timeout
						Content = new PlainText("Answer")
					},
				}
			},
		};

		public static async Task WriteRandomEvents(HttpContext context){
			var rnd = new Random();
			while(rnd.NextDouble() < .3){
				var item = RandomEventFactoryPool[rnd.Next(RandomEventFactoryPool.Length)]();
				await context.Response.WriteAsync(await item.RenderHtml());
			}
		}

		public void ConfigureRouting(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) {
			endpoints.MapGet("/", async context => await Respond(context, new Homepage()));
			endpoints.MapGet("/ops", async context => await Respond(context, new OpsPage()));
			endpoints.MapGet("_eventLog", WriteRandomEvents);
			endpoints.MapGet("/500", Error500);
		}

		public async Task HTTPError(HttpContext context, int code, string description) {
			var page = new ErrorPage(code);
			await page.Prepare(context);
			await context.Response.WriteAsync(await page.RenderHtml());
		}
	}


	public class Program {
		public static void Main(string[] args) {
			// uncomment as wanted
			JsonTest.Run();
			JsonDemo.Run();
			//Server.Start();
		}
	}
}