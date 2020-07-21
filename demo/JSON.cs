using System;
using System.IO;
using Lantern.Face.Json;
using System.Collections.Generic;
using System.Linq;
using face.demo;

public class User : IJsonEncodable {
	public readonly string Name;
	public User(string name) => Name = name;
	public bool Verified = false;
	public JsValue ToJsValue() => new Dictionary<string, JsValue> {
		["name"] = Name,
		["email_verified"] = Verified,
	};
	public JsValue GetClientInfo() => new Dictionary<string, JsValue> {
		["name"] = Name,
	};
	public static User FromJsValue(JsValue jsValue) {
		var u = new User(jsValue["name"]);
		if (jsValue.ContainsKey("email_verified")) u.Verified = jsValue["email_verified"];
		return u;
	}
}

public static class JsonDemo {
	public static void Run(){
		Test.Run();
		string json = File.ReadAllText("jsontest");
		JsValue jsobj = JsValue.ParseJson(json);
		Console.WriteLine("[json demo] First key of object.hello[6]: " + jsobj["hello"][6].Keys.First());

		User[] users = jsobj["hello"][6]["users"].ArrayValue
			.Select(User.FromJsValue).ToArray();

		Console.WriteLine("[json demo] " + users[1].Name + " verified: " + users[1].Verified.ToString());

		Console.WriteLine("[json demo] Extract strings: " + new JsValue(jsobj["hello"]
			.ArrayValue.Where(j
				=> j.DataType == JsType.String
			).ToArray()).ToJson());

		Dictionary<string, JsValue> dictionary = new Dictionary<string, JsValue> {
			["store_id"] = "C555",
			["pending_user_accounts"] = new [] { new User("Jedward"), new User("Hoagie"), },
			["unread_notice_ids"] = new []{ 52, 111 },
		};
		Console.WriteLine("[json demo] dictionary.ToJson(): " + dictionary.ToJson());

		/*
			[json demo] First key of object.hello[6]: animal
			[json demo] Linda Picklesbury ✨ verified: True
			[json demo] Extract strings: ["spaghetti","escaping with \"\\\""]
			[json demo] dictionary.ToJSON(): {"store_id":"C555","pending_user_accounts":[{"name":"Jedward","email_verified":false},{"name":"Hoagie","email_verified":false}],"unread_notice_ids":[52,111]}
		*/
	}
}