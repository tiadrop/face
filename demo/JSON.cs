using System;
using System.IO;
using Lantern.Face.JSON;
using System.Collections.Generic;
using System.Linq;

public class User : IJSONEncodable {
	public readonly string Name;
	public User(string name) => Name = name;
	public bool Verified = false;
	public JSValue ToJSValue() => new Dictionary<string, JSValue> {
		["name"] = Name,
		["email_verified"] = Verified,
	};
	public JSValue GetClientInfo() => new Dictionary<string, JSValue> {
		["name"] = Name,
	};
	public static User FromJSValue(JSValue jsValue) {
		var u = new User(jsValue["name"]);
		if (jsValue.ContainsKey("email_verified")) u.Verified = jsValue["email_verified"];
		return u;
	}
}

public class JSONDemo {
	public static void Run(){
		string json = File.ReadAllText("jsontest");
		JSValue jsobj = JSValue.ParseJSON(json);
		Console.WriteLine("[json demo] First key of object.hello[6]: " + jsobj["hello"][6].Keys.First());

		User[] users = jsobj["hello"][6]["users"].ArrayValue
			.Select(jv => User.FromJSValue(jv)).ToArray();

		Console.WriteLine("[json demo] " + users[1].Name + " verified: " + users[1].Verified.ToString());

		Console.WriteLine("[json demo] Extract strings: " + new JSValue(jsobj["hello"]
			.ArrayValue.Where(j
				=> j.DataType == JSType.String
			).ToArray()).ToJSON());

		Dictionary<string, JSValue> dictionary = new Dictionary<string, JSValue> {
			["store_id"] = "C555",
			["pending_user_accounts"] = new User[] { new User("Jedward"), new User("Hoagie"), },
			["unread_notice_ids"] = new int[]{ 52, 111 },
		};
		Console.WriteLine("[json demo] dictionary.ToJSON(): " + dictionary.ToJSON());

		/*
			[json demo] First key of object.hello[6]: animal
			[json demo] Linda Picklesbury ✨ verified: True
			[json demo] Extract strings: ["spaghetti","escaping with \"\\\""]
			[json demo] dictionary.ToJSON(): {"store_id":"C555","pending_user_accounts":[{"name":"Jedward","email_verified":false},{"name":"Hoagie","email_verified":false}],"unread_notice_ids":[52,111]}
		*/
	}
}