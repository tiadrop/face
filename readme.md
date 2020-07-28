###Synopsis

Face is a language learning project and probably shouldn't be used in the real world. The goal is to practice using type parameters by writing a server-side webpage/component framework that's comfortable and intuitive and where the language of HTML is like a 'compile target', largely absent from the abstraction at hand. It will ideally provide under-the-hood automations such that the abstraction it offers is consistent, versatile and easy on mental resources.

###Direction

A Page's Body is an object of Classes, Attributes and a collection of Parts.

Parts can be Elements. Element Parts expose methods and properties that pertain to HTML elements, with interfaces to manipulate a class list, attributes, `data-` attributes and `List<T> where T: Part` content. A Page's header section is generated on render and manipulated via members such as `Title`. A Page's Body is actually an Element.

Parts can be components; essentially factories for producing Element structures depending on their own applicable properties. See [Face/Parts/*](Face/Parts/) for examples of components. See [Face/parts/TimeAgo.cs](Face/Parts/TimeAgo.cs) in particular for an example of client-side JavaScript involvement in components.

There are two ways to involve JavaScript. The simplest is to `JsUrls.Add(url)` inside your Page derivative's `Prepare()` override. This will include the script in the rendered `<head>` via `face.require()`.

Face adds another client-side scripting mechanism which I've found to be convenient and enjoyable. An *element script* can be registered with `face.register()`, then it'll be called against all elements whose `data-script` attribute matches the registered name. Registration will usually happen in a .js file loaded via `page.JsUrls`, itself usually having been automatically included due to the optional Part override `GetClientRequires()`. Client-side requirements are considered when writing a Part but not when consuming it. See [Face/parts/TimeAgo.cs](Face/Parts/TimeAgo.cs) and [Face.Parts.General.js](js/Face.Parts.General.js) for an example of this approach.

The element script system was designed to allow for asynchronous initialisation. Pass a `Promise` to `face.await()` in your custom JS file and we'll wait until your script's loaded and ready.

After dynamically adding content `face.apply()` should be called, optionally passing in the element containing the new content, to apply element scripts. `face.require()` may also be called freely to load dependencies of such content, or they could be preemptively added to the Page's `JsUrls`.

CSS links can likewise be added to a Page's head via `page.CssUrls`.

Reading an Element's ID will generate one if empty, so we can think of all Elements as simply 'having' a unique ID.

```c#  
InputElement input = new InputElement(){ Value = "wazzaaaa" };  
Console.WriteLine(input.RenderHtml());  
LabelElement label = new LabelElement(){ For = input }; // set => Attribs["for"] = value.Id  
Console.WriteLine(label.Attribs["for"]);  
Console.WriteLine(input.RenderHtml());  
// <input value="wazzaaaa">  
// 92a73ac38  
// <input value="wazzaaaa" id="92a73ac38">
```

Included is a similar system written in JavaScript. It was written for private use but I feel its similarity and ancestry to this project make it an ideal inclusion to give a degree of cognitive consistency between server and client code. For code and examples see [Face/js-src/jel/](Face/js-src/jel/).

###JSON

Face.Json provides an `IJsonEncodable` interface and an extension method `ToJson()` to produce JSON-formatted data from each of IJsonEncodable, string, int, double, bool, T[] and IDictionary<string, T> where T : IJsonEncodable or a listed primitive.

Rather than attempting to construct native objects from JSON data, `IJsonEncodable.ToJsValue()` and `JsValue.FromJson()` allow for manually-processed dynamic structure with runtime type-checking. Each produces a JsValue object with interfaces to read its internal value as any compatible type including `ReadOnlyDictionary<string, JsValue>` and `JsValue[]`. JS object properties can be read via `value[string]` and JS array entries can be read via `value[int]`. A JsValue reveals its type via `value.DataType`, just as one might use `typeof` in JSON's most natural habitat.

I believe JSON's dynamically-structured nature is a strength and have aimed to take advantage of it while providing type information and safety where appropriate. See [demo/JsonDemo.cs](demo/JsonDemo.cs) for examples of importing and exporting JSON using this system.

According to rough but generally consistent [benchmark](jsonbenchmark.log) ([JsonTest.cs](demo/JsonTest.cs)), `JsValue.FromJson()`'s performance is comparable with that of Utf8Json and Text.Json's equivalents in most cases and outperforms both in some common cases.