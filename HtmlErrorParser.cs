using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

public class HtmlErrorParser
{
    private List<string> _validHtmlTags = new List<string>
    {
        "a", "abbr", "address", "area", "article", "aside", "audio", "b", "base", "bdi", "bdo",
        "blockquote", "body", "br", "button", "canvas", "caption", "cite", "code", "col",
        "colgroup", "data", "datalist", "dd", "del", "details", "dfn", "dialog", "div", "dl",
        "dt", "em", "embed", "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2",
        "h3", "h4", "h5", "h6", "head", "header", "hr", "html", "i", "iframe", "img", "input",
        "ins", "kbd", "label", "legend", "li", "link", "main", "map", "mark", "meta", "meter",
        "nav", "noscript", "object", "ol", "optgroup", "option", "output", "p", "param", "picture",
        "pre", "progress", "q", "rp", "rt", "ruby", "s", "samp", "script", "section", "select",
        "small", "source", "span", "strong", "style", "sub", "summary", "sup", "svg", "table",
        "tbody", "td", "template", "textarea", "tfoot", "th", "thead", "time", "title", "tr",
        "track", "u", "ul", "var", "video", "wbr"
    };

    public List<string> DetectHtmlErrors(string htmlContent)
    {
        var errors = new List<string>();

        // 1. Missing Closing Tag Detection
        errors.AddRange(DetectMissingClosingTags(htmlContent));

        // 2. Mismatched Tag Detection
        errors.AddRange(DetectMisMatchedTags(htmlContent));

        // 3. Unquoted Attribute Values
        errors.AddRange(DetectUnquotedAttributeValues(htmlContent));

        // 4. Missing Attribute Values
        errors.AddRange(DetectMissingAttributeValues(htmlContent));

        // 5. Incorrect Nesting
        errors.AddRange(DetectIncorrectNesting(htmlContent));

        // 6. Invalid Tag Names
        errors.AddRange(DetectInvalidTagNames(htmlContent));

        // 7. Duplicate Attributes
        errors.AddRange(DetectDuplicateAttributes(htmlContent));

        // 8. Unescaped Special Characters
        errors.AddRange(DetectUnescapedSpecialCharacters(htmlContent));

        // 9. Missing DOCTYPE
        if (!DetectDocTypePresence(htmlContent))
        {
            errors.Add("Missing DOCTYPE declaration");
        }

        // 10. Unclosed Comments
        errors.AddRange(DetectUnclosedComments(htmlContent));

        return errors;
    }

    private List<string> DetectMissingClosingTags(string html)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);
        var errors = new List<string>();

        // Custom check for unclosed tags
        var tags = new Stack<string>();
        var regex = new Regex(@"<(/)?(\w+)[^>]*>");
        var matches = regex.Matches(html);

        foreach (Match match in matches)
        {
            string tag = match.Groups[2].Value.ToLower();
            bool isClosing = match.Groups[1].Success;

            if (!isClosing)
            {
                tags.Push(tag);
            }
            else
            {
                if (tags.Count == 0 || tags.Peek() != tag)
                {
                    errors.Add($"Mismatched or unclosed tag: <{tag}>");
                }
                else
                {
                    tags.Pop();
                }
            }
        }

        // Check for remaining unclosed tags
        while (tags.Count > 0)
        {
            errors.Add($"Unclosed tag: <{tags.Pop()}>");
        }

        return errors;
    }

    private List<string> DetectMisMatchedTags(string html)
    {
        var errors = new List<string>();
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        try
        {
            CheckNodeNesting(doc.DocumentNode, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"Error during tag matching: {ex.Message}");
        }

        return errors;
    }

    private void CheckNodeNesting(HtmlNode node, List<string> errors)
    {
        for (int i = 0; i < node.ChildNodes.Count; i++)
        {
            var currentNode = node.ChildNodes[i];
            if (currentNode.NodeType == HtmlNodeType.Element)
            {
                // Check for potential mismatched closing tags
                if (i + 1 < node.ChildNodes.Count)
                {
                    var nextNode = node.ChildNodes[i + 1];
                    if (nextNode.NodeType == HtmlNodeType.Element &&
                        currentNode.Name != nextNode.Name &&
                        currentNode.Name != nextNode.ParentNode.Name)
                    {
                        errors.Add($"Potential mismatched tags: <{currentNode.Name}> and <{nextNode.Name}>");
                    }
                }

                CheckNodeNesting(currentNode, errors);
            }
        }
    }

    private List<string> DetectUnquotedAttributeValues(string html)
    {
        var errors = new List<string>();
        var regex = new Regex(@"(\w+)=([^\s""']+)");
        var matches = regex.Matches(html);

        foreach (Match match in matches)
        {
            string attribute = match.Groups[1].Value;
            string value = match.Groups[2].Value;
            errors.Add($"Unquoted attribute value: {attribute}={value}");
        }

        return errors;
    }

    private List<string> DetectMissingAttributeValues(string html)
    {
        var errors = new List<string>();
        var regex = new Regex(@"(\w+)=(?:\s*[""']?)?\s*[""']?");
        var matches = regex.Matches(html);

        foreach (Match match in matches)
        {
            string attribute = match.Groups[1].Value;
            errors.Add($"Missing attribute value for: {attribute}");
        }

        return errors;
    }

    private List<string> DetectIncorrectNesting(string html)
    {
        var errors = new List<string>();
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        CheckTagNestingRecursive(doc.DocumentNode, new Stack<string>(), errors);

        return errors;
    }

    private void CheckTagNestingRecursive(HtmlNode node, Stack<string> tagStack, List<string> errors)
    {
        if (node.NodeType == HtmlNodeType.Element)
        {
            if (tagStack.Count > 0 && !IsValidNesting(tagStack.Peek(), node.Name))
            {
                errors.Add($"Incorrect nesting: <{tagStack.Peek()}> contains <{node.Name}>");
            }

            tagStack.Push(node.Name);
        }

        foreach (var childNode in node.ChildNodes)
        {
            CheckTagNestingRecursive(childNode, new Stack<string>(tagStack), errors);
        }

        if (node.NodeType == HtmlNodeType.Element)
        {
            tagStack.Pop();
        }
    }

    private bool IsValidNesting(string parentTag, string childTag)
    {
        // Define specific nesting rules here
        // This is a simple implementation and can be expanded
        return true;
    }

    private List<string> DetectInvalidTagNames(string html)
    {
        var errors = new List<string>();
        var regex = new Regex(@"<(\w+)");
        var matches = regex.Matches(html);

        foreach (Match match in matches)
        {
            string tag = match.Groups[1].Value.ToLower();
            if (!_validHtmlTags.Contains(tag))
            {
                errors.Add($"Invalid HTML tag: <{tag}>");
            }
        }

        return errors;
    }

    private List<string> DetectDuplicateAttributes(string html)
    {
        var errors = new List<string>();
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        foreach (var node in doc.DocumentNode.Descendants())
        {
            var attributeNames = node.Attributes.Select(a => a.Name).ToList();
            var duplicates = attributeNames.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var duplicate in duplicates)
            {
                errors.Add($"Duplicate attribute in <{node.Name}>: {duplicate}");
            }
        }

        return errors;
    }

    private List<string> DetectUnescapedSpecialCharacters(string html)
    {
        var errors = new List<string>();
        var specialChars = new Dictionary<string, string>
        {
            { "&", "&amp;" },
            { "<", "&lt;" },
            { ">", "&gt;" }
        };

        foreach (var specialChar in specialChars)
        {
            var regex = new Regex($@"(?<!&\w+;){Regex.Escape(specialChar.Key)}");
            if (regex.IsMatch(html))
            {
                errors.Add($"Unescaped special character: {specialChar.Key}");
            }
        }

        return errors;
    }

    private bool DetectDocTypePresence(string html)
    {
        return Regex.IsMatch(html, @"<!DOCTYPE\s+html", RegexOptions.IgnoreCase);
    }

    private List<string> DetectUnclosedComments(string html)
    {
        var errors = new List<string>();
        var unclosedCommentRegex = new Regex(@"<!--[^>]*$", RegexOptions.Multiline);

        if (unclosedCommentRegex.IsMatch(html))
        {
            errors.Add("Unclosed HTML comment detected");
        }

        return errors;
    }

    // Example usage method
    public void ParseAndReportHtmlErrors(string htmlContent)
    {
        var errors = DetectHtmlErrors(htmlContent);

        if (errors.Any())
        {
            var errorMessage = string.Join(Environment.NewLine, errors);
            MessageBox.Show(errorMessage, "HTML Parsing Errors", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            MessageBox.Show("No HTML errors detected", "HTML Parsing", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}