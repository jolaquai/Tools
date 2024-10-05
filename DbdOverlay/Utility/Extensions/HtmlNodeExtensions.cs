using HtmlAgilityPack;

namespace DbdOverlay.Utility.Extensions;

public static class HtmlNodeExtensions
{
    public static IEnumerable<HtmlNode> SiblingsAfter(this HtmlNode node) => node.ParentNode.ChildNodes.SkipWhile(n => n != node).Skip(1);
    public static IEnumerable<HtmlNode> SiblingsBefore(this HtmlNode node) => node.ParentNode.ChildNodes.TakeWhile(n => n != node);
}
