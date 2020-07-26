namespace Lantern.Face.Json {
    /// <summary>
    /// Implement to allow implicit and explicit casting to JsValue. Confers a ToJson() extension method.
    /// </summary>
    public interface IJsonEncodable {
        /// <summary>
        /// Express the object as a JsValue
        /// </summary>
        /// <returns>A JsValue representing this object</returns>
        JsValue ToJsValue();
    }
}