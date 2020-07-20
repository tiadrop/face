using System;
using Lantern.Face;
using Lantern.Face.Parts;
using Lantern.Face.Parts.Html;
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Http;
using Lantern.FaceDemo;
//using Lantern.Face.JSON;
using System.Collections.Generic;

class Homepage : Page {

	public override Task Prepare(HttpContext context) {
		Title = "Central";
		Body.Classes.Add("homepage");
		cssUrls.Add("/.Face.css");
		jsUrls.Add("/.Face.js");

		PartList parts = new PartList();
		var uptime = Convert.ToInt64(System.Environment.TickCount64);
		var currentTime = Convert.ToInt64(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds());
		var powerOnUnixTime = Convert.ToInt64(Math.Round(Convert.ToDouble(currentTime - uptime)) / 1000);

		parts.Add(new DivElement {
			Classes = new[] { "panel" },
			Content = new Part[]{
				new HeadingElement(1){
					Content = "Information",
				},
				new SectionElement{
					Content = new Part[]{
						new LabelElement(){
							Content = new PlainText("Device"),
						},
						System.Environment.MachineName
					},
				},
				new SectionElement{
					Content = new Part[]{
						new LabelElement(){
							Content = new PlainText("Uptime"),
						},
						new TimeAgo(powerOnUnixTime)
					},
				},
				new SectionElement{
					Content = new Part[]{
						new LabelElement(){
							Content = new PlainText("Generated"),
						},
						new TimeAgo(currentTime / 1000),
						new PlainText(" ago"),
					},
				},
			}
		});

		parts.Add(new DivElement {
			Classes = new[] { "panel" },
			Content = new Part[]{
				new HeadingElement(1){
					Content = new PlainText("Event Log"),
				},
				new EventLog(){
					Classes ="eventLog",
					Content = new EventListItem[]{

						new EventListItem{
							Label = "KNotify",
							Content = new RawHTML("<b>Download complete:</b> Ainsley Harriot's Ultimate Reddy-Brek Recipies.pdf"),
						},
						new EventListItem{
							Label = "Authorised",
							Classes = new[]{ "good" },
							Content = new RawHTML("Your request for <em>skynet5 repository access</em> was accepted"),
						},
						new EventListItem{
							Label = "Message",
							Content = new RawHTML("Received a <a href='converse/rickyg'>message</a> from <em>Ricky G</em>"),
						},
						new EventListItem{
							Label = "Project Y",
							Classes = new []{ "projectY" },
							Content = new PlainText("The rabbit is in the hutch"),
						},
						new EventListItem{
							Label = "Incoming call",
							Classes = new[]{ "priority" },
							Content = new Part[] {
								new RawHTML("<em>Johnny Ten</em>, unscheduled<br>"),
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

					},
				},
			}
		});


		Body.Append(new StandardBody{
			Content = parts,
			ActiveHRef = "/"
		});
		return Task.CompletedTask;
	}
}