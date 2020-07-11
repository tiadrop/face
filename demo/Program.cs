using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Lantern.Face;
using Lantern.Face.Parts;
using Lantern.Face.Parts.HTML;
using System.Collections.Generic;
using Lantern.Face.JSON;

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
				Port = 818
			};
		}
		public async Task Handle(HttpContext context) {
			Page page = new ErrorPage(404);
			await page.Prepare(context);
			await context.Response.WriteAsync(await page.RenderHTML());
		}

		public static async Task Respond(HttpContext context, Page page){
			await page.Prepare(context);
			await context.Response.WriteAsync(await page.RenderHTML());
		}

		public delegate EventListItem GetEvent();
		public static GetEvent[] RandomEventFactoryPool = new GetEvent[]{
			() => new EventListItem{ Label = "KNotify", Content = "Download complete: Ainsley Harriot's Ultimate Reddy-Brek Recipies.pdf"},
			() => new EventListItem{ Label = "Authorised", Content = "Your request for skynet5 repository access was accepted"},
			() => new EventListItem{
				Label = "Message",
				Content = new RawHTML("Received a <a href='converse/rickyg'>message</a> from <em>Ricky G</em>"),
			},
			() => new EventListItem{ Label = "Project Y", Content = "The rabbit is in the hutch", Classes = "projectY"},
			() => new EventListItem{
				Label = "Incoming call",
				Classes = new[]{ "priority" },
				Content = new Part[] {
					new RawHTML("<em>Johnny Ten</em>, unscheduled<br>"),
					new DelegateButton{
						OnClick = context => {
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
				await context.Response.WriteAsync(await item.RenderHTML());
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
			await context.Response.WriteAsync(await page.RenderHTML());
		}
	}



	public class User { // demoing a JSON engine use case
		public readonly string Name;
		public User(string name) => Name = name;
		public static implicit operator JSValue(User source) => new JSValue(new Dictionary<string, JSValue> {
			["name"] = source.Name,
			["email_verified"] = false,
		});
	}

	public class Program {
		public static void Main(string[] args) {
			string json = File.ReadAllText("jsontest");
			JSValue jsobj = JSValue.ParseJSON(json);
			Console.WriteLine("[json test] Second user name: " + jsobj["hello"][6]["users"][1]["name"].StringValue);
			Console.WriteLine("[json test] Strings: " + new JSValue(jsobj["hello"].ToArray().Where(j => j.DataType == JSType.String).ToArray()).ToJSON());

			User ExampleUser = new User("Eric123");

			jsobj = new JSValue(new Dictionary<string, JSValue> {
				["name"] = "Eric",
				["account_info"] = ExampleUser,
				["stats"] = new Dictionary<string, JSValue>{
					["strength"] = 51,
					["speed"] = 30,
				},
				["abilities"] = new JSValue[]{
					"heal", "cross-slash"
				}
			});
			Console.WriteLine("[json test] ToJSON(): " + jsobj.ToJSON());

			ReadOnlyDictionary<string, JSValue> stats = jsobj["stats"];
			double speed = stats["speed"];
			Console.WriteLine("[json test] " + jsobj["name"] + "'s speed: " + speed.ToString());

			Server.Start();
		}
	}
}