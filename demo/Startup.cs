using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Lantern.Face;

namespace Lantern.Face {

    public class Startup<TApp> where TApp : FaceDemo.App, new() {
        public void ConfigureServices(IServiceCollection services) {
        }

		private byte[] faviconPngContent;
		private FaceDemo.App _app;
		public Startup(){
			_app = new TApp();
			var iconPath = _app.GetResourcePath("favicon.png");
			faviconPngContent = File.Exists(iconPath) ? File.ReadAllBytes(iconPath) : null;
		}

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

			app.UseRouting();
            app.UseEndpoints(endpoints => {

				endpoints.MapGet("/favicon.png", async context => {
					if(faviconPngContent == null){
						await _app.Error404(context);
						return;
					}
					context.Response.ContentType = "image/png";
					await context.Response.Body.WriteAsync(faviconPngContent);
				});
				endpoints.MapGet("/.Face.css", async context => { // ".Face.css"
					context.Response.ContentType = "text/css";
					await context.Response.SendFileAsync(_app.GetResourcePath("Face.css"));
				});
				endpoints.MapGet("/.Face.js", async context => { // ".Face.js"
					context.Response.ContentType = "text/javascript";
					await context.Response.WriteAsync("// Face (c) thecynicslantern.net 2020\n");
					await context.Response.SendFileAsync(_app.GetResourcePath("Face.js"));
				});
				endpoints.MapGet("/{lib}.js", async context => {
					string filename = (string)context.Request.RouteValues["lib"] + ".js";
					FileInfo fiRequested = new FileInfo("js/" + filename);
					if(fiRequested.Exists){
						DirectoryInfo diRoot = new DirectoryInfo("js");
						bool isParent = fiRequested.Directory.FullName == diRoot.FullName;
						if(isParent){
							context.Response.ContentType = "text/javascript";
							await context.Response.SendFileAsync(fiRequested.FullName);
							return;
						}
					}

					await _app.Handle(context);
				});

				endpoints.MapPost(".Face/delegate/ping", async context => {
					StreamReader reader = new StreamReader(context.Request.Body);
					string body = await reader.ReadToEndAsync();
					string[] ids = body.Split(',');
					Lantern.Face.Parts.DelegateButton.Touch(ids);
				});
				endpoints.MapPost(".Face/delegate/{guid}", async context => {
					await Lantern.Face.Parts.DelegateButton.Fire((string)context.Request.RouteValues["guid"], context);
				});

				_app.ConfigureRouting(endpoints);

			});

			app.Use(async (ctx, next) => {
				await _app.Handle(ctx);
			});

			app.Use(async (ctx, next) => {
				await _app.Error404(ctx);
			});

		}
    }
}
