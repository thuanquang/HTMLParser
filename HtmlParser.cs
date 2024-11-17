using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HtmlParserApp
{
    public partial class Form1 : Form
    {
        private TextBox textBoxInput;
        private Button buttonParse;
        private TreeView treeViewOutput;
        private TextBox textBoxError; // Renamed for clarity

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.Text = "HTML Parser";

            // Input TextBox
            textBoxInput = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Location = new Point(12, 12),
                Size = new Size(650, 150),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            textBoxInput.TextChanged += TextBoxInput_TextChanged;

            // Parse Button
            buttonParse = new Button
            {
                Text = "Parse HTML",
                Location = new Point(670, 12),
                Size = new Size(100, 30),
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            buttonParse.Click += buttonParse_Click;

            // TreeView for DOM output
            treeViewOutput = new TreeView
            {
                Location = new Point(12, 170),
                Size = new Size(758, 350),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Error TextBox (hidden by default)
            textBoxError = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Location = new Point(12, 530),
                Size = new Size(758, 60),
                Visible = false,
                BackColor = System.Drawing.Color.MistyRose,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Add controls to form
            Controls.AddRange(new Control[] {
                textBoxInput,
                buttonParse,
                treeViewOutput,
                textBoxError
            });
        }

        private void TextBoxInput_TextChanged(object sender, EventArgs e)
        {
            buttonParse.Enabled = !string.IsNullOrEmpty(textBoxInput.Text);
        }

        private void buttonParse_Click(object sender, EventArgs e)
        {
            treeViewOutput.Nodes.Clear();
            textBoxError.Visible = false;

            try
            {
                string html = textBoxInput.Text;
                Tokenizer tokenizer = new Tokenizer();
                List<string> tokens = tokenizer.Tokenize(html);

                Parser parser = new Parser();
                HtmlNode domTree = parser.Parse(tokens);

                TreeNode rootNode = CreateTreeNode(domTree);
                if (rootNode != null)
                {
                    treeViewOutput.Nodes.Add(rootNode);
                    treeViewOutput.ExpandAll();
                }
            }
            catch (ParsingException pe)
            {
                ShowError($"Parsing Error: {pe.Message}");
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            textBoxError.Text = message;
            textBoxError.Visible = true;
        }

        // Helper function to recursively create TreeNodes from HtmlNodes
        private TreeNode CreateTreeNode(HtmlNode htmlNode)
        {
            string nodeText;
            if (htmlNode.TagName == "#text")
            {
                nodeText = htmlNode.Content;
            }
            else
            {
                nodeText = htmlNode.TagName;
                if (htmlNode.Attributes.Count > 0)
                {
                    nodeText += " [" + string.Join(", ",
                        htmlNode.Attributes.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\"")) + "]";
                }
            }

            TreeNode treeNode = new TreeNode(nodeText);

            foreach (var child in htmlNode.Children)
            {
                treeNode.Nodes.Add(CreateTreeNode(child));
            }

            return treeNode;
        }

        // Custom Queue Data Structure
        public class MyQueue<T>
        {
            private class Node
            {
                public T Value { get; set; }
                public Node Next { get; set; }
                public Node Previous { get; set; }

                public Node(T value)
                {
                    Value = value;
                    Next = null;
                    Previous = null;
                }
            }

            private Node head;
            private Node tail;
            private int count;

            public int Count => count;

            public void Enqueue(T item)
            {
                Node newNode = new Node(item);
                if (IsEmpty())
                {
                    head = tail = newNode;
                }
                else
                {
                    tail.Next = newNode;
                    newNode.Previous = tail;
                    tail = newNode;
                }
                count++;
            }

            public T Dequeue()
            {
                if (IsEmpty())
                {
                    throw new InvalidOperationException("Queue is empty");
                }

                T value = head.Value;
                head = head.Next;
                if (head != null)
                {
                    head.Previous = null;
                }
                else
                {
                    tail = null;
                }
                count--;
                return value;
            }

            public T Peek()
            {
                if (IsEmpty())
                {
                    throw new InvalidOperationException("Queue is empty");
                }
                return head.Value;
            }

            public T PeekLast()
            {
                if (IsEmpty())
                {
                    throw new InvalidOperationException("Queue is empty");
                }
                return tail.Value;
            }

            public bool IsEmpty()
            {
                return count == 0;
            }

            public void Clear()
            {
                head = tail = null;
                count = 0;
            }
        }

        public class Tokenizer
        {
            public List<string> Tokenize(string html)
            {
                List<string> tokens = new List<string>();
                int i = 0;

                while (i < html.Length)
                {
                    if (html[i] == '<')
                    {
                        // Find the end of the tag
                        int tagEnd = html.IndexOf('>', i);
                        if (tagEnd == -1) break;

                        // Extract the complete tag
                        string tag = html.Substring(i, tagEnd - i + 1);
                        if (!string.IsNullOrWhiteSpace(tag))
                        {
                            tokens.Add(tag);
                        }
                        i = tagEnd + 1;
                    }
                    else
                    {
                        // Handle text content
                        int nextTag = html.IndexOf('<', i);
                        if (nextTag == -1) nextTag = html.Length;

                        string text = html.Substring(i, nextTag - i);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            tokens.Add(text.Trim());
                        }
                        i = nextTag;
                    }
                }
                return tokens;
            }
        }

        public class Parser
        {
            private readonly HashSet<string> voidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input",
        "link", "meta", "param", "source", "track", "wbr", "!doctype"
    };

            public HtmlNode Parse(List<string> tokens)
            {
                HtmlNode root = new HtmlNode { TagName = "root" };
                HtmlNode currentNode = root;
                MyQueue<HtmlNode> nodeQueue = new MyQueue<HtmlNode>();
                nodeQueue.Enqueue(root);

                foreach (string token in tokens)
                {
                    if (string.IsNullOrWhiteSpace(token)) continue;

                    if (token.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
                    {
                        // Handle DOCTYPE declaration
                        HtmlNode doctype = new HtmlNode { TagName = "!DOCTYPE" };
                        currentNode.Children.Add(doctype);
                        continue;
                    }

                    if (token.StartsWith("<") && !token.StartsWith("</"))
                    {
                        // Opening tag or void element
                        string tagName = ExtractTagName(token);

                        HtmlNode newNode = new HtmlNode
                        {
                            TagName = tagName,
                            Attributes = ParseAttributes(token),
                            Parent = currentNode
                        };

                        currentNode.Children.Add(newNode);

                        if (!voidElements.Contains(tagName))
                        {
                            currentNode = newNode;
                            nodeQueue.Enqueue(newNode);
                        }
                    }
                    else if (token.StartsWith("</"))
                    {
                        // Closing tag
                        string closingTag = ExtractTagName(token);

                        if (!nodeQueue.IsEmpty() && nodeQueue.PeekLast().TagName.Equals(closingTag, StringComparison.OrdinalIgnoreCase))
                        {
                            MyQueue<HtmlNode> tempQueue = new MyQueue<HtmlNode>();
                            HtmlNode lastNode = null;

                            // Find the matching opening tag
                            while (!nodeQueue.IsEmpty())
                            {
                                lastNode = nodeQueue.Dequeue();
                                if (lastNode.TagName.Equals(closingTag, StringComparison.OrdinalIgnoreCase))
                                {
                                    break;
                                }
                                tempQueue.Enqueue(lastNode);
                            }

                            // Restore the queue except for the closed tag
                            while (!tempQueue.IsEmpty())
                            {
                                nodeQueue.Enqueue(tempQueue.Dequeue());
                            }

                            if (!nodeQueue.IsEmpty())
                            {
                                currentNode = nodeQueue.PeekLast();
                            }
                        }
                        else
                        {
                            throw new ParsingException($"Mismatched closing tag: {closingTag}");
                        }
                    }
                    else
                    {
                        // Text content
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            HtmlNode textNode = new HtmlNode
                            {
                                TagName = "#text",
                                Content = token.Trim(),
                                Parent = currentNode
                            };
                            currentNode.Children.Add(textNode);
                        }
                    }
                }

                // Check for unclosed tags
                if (nodeQueue.Count > 1) // More than just root node
                {
                    MyQueue<string> unclosedTags = new MyQueue<string>();
                    while (nodeQueue.Count > 1)
                    {
                        unclosedTags.Enqueue(nodeQueue.Dequeue().TagName);
                    }
                    throw new ParsingException($"Unclosed tags found: {string.Join(", ", unclosedTags)}");
                }

                return root;
            }

            private string ExtractTagName(string tag)
            {
                string temp = tag.TrimStart('<', '/');
                int endIndex = temp.IndexOfAny(new char[] { ' ', '>', '/' });
                if (endIndex == -1) endIndex = temp.Length;
                return temp.Substring(0, endIndex);
            }

            private Dictionary<string, string> ParseAttributes(string tag)
            {
                Dictionary<string, string> attributes = new Dictionary<string, string>();

                int startIndex = tag.IndexOf(' ');
                if (startIndex == -1) return attributes;

                string attributesPart = tag.Substring(startIndex, tag.Length - startIndex - 1).Trim();

                Regex attrRegex = new Regex(@"(\w+)\s*=\s*(?:""([^""]*)""|'([^']*)'|(\S+))");
                MatchCollection matches = attrRegex.Matches(attributesPart);

                foreach (Match match in matches)
                {
                    string key = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    if (string.IsNullOrEmpty(value)) value = match.Groups[3].Value;
                    if (string.IsNullOrEmpty(value)) value = match.Groups[4].Value;

                    attributes[key] = value;
                }

                return attributes;
            }
        }

        public class HtmlNode
        {
            public string TagName { get; set; }
            public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
            public string Content { get; set; }
            public List<HtmlNode> Children { get; set; } = new List<HtmlNode>();
            public HtmlNode Parent { get; set; }

            public override string ToString()
            {
                if (TagName == "#text")
                    return Content;

                string attrs = "";
                if (Attributes.Count > 0)
                {
                    attrs = " " + string.Join(" ",
                        Attributes.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\""));
                }

                return $"{TagName}{attrs}";
            }
        }

        public class ParsingException : Exception
        {

            public ParsingException(string message) : base(message) { }

        }

        


        
    }

}