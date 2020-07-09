using System.Collections.Generic;
using System;
using System.Linq;

namespace Lantern.Face.Parts.HTML {

	public abstract class InteractiveElement<TChild> : Element<TChild> where TChild : Part {
		public InteractiveElement(string tag) : base(tag) { }
		public bool Enabled {
			get => !Attribs.Keys.Contains("disabled");
			set {
				if (value) {
					Attribs.Remove("disabled");
				} else {
					Attribs["disabled"] = "";
				}
			}
		}
		public int TabIndex {
			get => Attribs.Keys.Contains("tabindex") ? Convert.ToInt32(Attribs["tabindex"]) : -1;
			set {
				if(value == -1){
					Attribs.Remove("tabindex");
				} else {
					Attribs["tabindex"] = value.ToString();
				}
			}
		}
	}
	public abstract class InteractiveElement : InteractiveElement<Part> {
		public InteractiveElement(string tag) : base(tag) { }
	}

	public class OtherElement<TChild> : Element<TChild> where TChild : Part {
		public OtherElement(string tag) : base(tag) { }
	}

	public class OtherElement : OtherElement<Part> {
		public OtherElement(string tag) : base(tag) { }
	}

	public class DivElement : Element {
		public DivElement() : base("div") { }
	}

	public class BodyElement : Element {
		public BodyElement() : base("body") { }
	}

	public class SpanElement : Element {
		public SpanElement() : base("span") { }
	}

	public class AnchorElement : Element {
		public AnchorElement() : base("a") { }
		public string HRef {
			get => Attribs["href"];
			set => Attribs["href"] = value;
		}
	}

	public class SectionElement : Element {
		public SectionElement() : base("section") { }
	}

	public class ArticleElement : Element {
		public ArticleElement() : base("article") { }
	}

	public class ButtonElement : InteractiveElement {
		public ButtonElement() : base("button") { }
	}

	public class ImageElement : Element {
		public ImageElement() : base("img") { }
		public string SourceUrl {
			get => Attribs["src"];
			set => Attribs["src"] = value;
		}
		public string AlternativeText {
			get => Attribs["alt"];
			set => Attribs["alt"] = value;
		}
	}

	public class BreakElement : Element {
		public BreakElement() : base("br") { }
	}

	public class HeadingElement : Element {
		private byte _level;
		public byte Level {
			get => _level;
			set {
				if (value < 1 || value > 6) throw new ArgumentException("Invalid Heading level");
				_level = value;
			}
		}
		public HeadingElement(byte level) : base("h" + level.ToString()) {
			Level = level;
		}
	}

	public class OrderedListElement : Element<ListItemElement> {
		public OrderedListElement() : base("ol") { }
	}
	public class UnorderedListElement : Element<ListItemElement> {
		public UnorderedListElement() : base("ul") { }
	}

	public class ListItemElement : Element {
		public ListItemElement() : base("li") { }
	}

	public class ParagraphElement : Element {
		public ParagraphElement() : base("p") { }
	}

	public class PreFormattedElement : Element {
		public PreFormattedElement() : base("pre") { }
	}

	public class BoldElement : Element {
		public BoldElement() : base("b") { }
	}

	public class ItalicElement : Element {
		public ItalicElement() : base("i") { }
	}


	public class StrikethroughElement : Element {
		public StrikethroughElement() : base("s") { }
	}

	public class EmphasisElement : Element {
		public EmphasisElement() : base("em") { }
	}

	public class TableElement : Element<TableBodyElement> {
		public TableElement() : base("table") { }
	}
	public class TableBodyElement : Element<TableRowElement> {
		public TableBodyElement() : base("tbody") { }
	}


	public abstract class TableCell : Element {
		public TableCell(string tag) : base(tag) { }
	}

	public class TableRowElement : Element<TableCell> {
		public TableRowElement() : base("tr") { }
	}

	public class TableHeadingElement : TableCell {
		public TableHeadingElement() : base("th") { }
	}
	public class TableCellElement : TableCell {
		public TableCellElement() : base("td") { }
	}

	public class NavigationElement : Element {
		public NavigationElement() : base("nav") { }
	}

	public class HeaderElement : Element {
		public HeaderElement() : base("header") { }
	}

	public class FooterElement : Element {
		public FooterElement() : base("footer") { }
	}

	public class InputElement : InteractiveElement {
		public InputElement() : base("input") { }
		public string Value {
			get => Attribs["value"];
			set => Attribs["value"] = value;
		}
	}

	public class LabelElement : InteractiveElement {
		public Element For {
			set => Attribs["for"] = value.ID;
		}
		public LabelElement() : base("label") {}
	}
	public class FormElement : Element {
		public FormElement() : base("form") { }
	}
	public class BlockQuoteElement : Element {
		public BlockQuoteElement() : base("blockquote") { }
	}
	public class CodeElement : Element {
		public CodeElement() : base("code") { }
	}

	public abstract class MediaSourcedElement : InteractiveElement {

		public class SourceElement : Element {
			public SourceElement(string sourceUrl) : base("source") {
				SourceUrl = sourceUrl;
			}
			public string SourceUrl {
				get => Attribs["src"];
				set => Attribs["src"] = value;
			}
		}

		public MediaSourcedElement(string tag, string[] sources) : base(tag){
			if(sources != null){
				foreach(string url in sources) Append(new SourceElement(url));
			}
		}

		public string[] Sources {
			set{
				foreach(Part part in Content.ToArray()){
					if(part is SourceElement){
						Content.Remove(part);
					}
				}
				foreach(string url in value) Append(new SourceElement(url));
			}
			get => Content.Where(part => part is SourceElement).Select(part => ((SourceElement)part).SourceUrl).ToArray();
		}
	}

	public class OptionElement : Element<PlainText> {
		public OptionElement() : base("option") {}
	}

	public class SelectElement : InteractiveElement<OptionElement> {
		public Dictionary<string, string> Options {
			set {
				Content = new PartList<OptionElement>();
				foreach (var key in value.Keys){
					Content.Add(new OptionElement {
						Name = key,
						Content = new PlainText(value[key])
					});
				}
			}
		}
		public SelectElement() : base("select") {}
	}

	public class TextAreaElement : InteractiveElement<PlainText> {
		public TextAreaElement() : base("textarea") {}
		public string Value {
			get => String.Join("", Content.Select(pt => pt.Text));
			set => Content = new PlainText(value);
		}
	}

	public class AudioElement : MediaSourcedElement {
		public AudioElement(string[] sources) : base("audio", sources) { }
	}

	public class VideoElement : MediaSourcedElement {
		public VideoElement(string[] sources) : base("video", sources) { }
		public bool ShowControls {
			get => BooleanAttribs["controls"];
			set => BooleanAttribs["controls"] = value;
		}
	}

}