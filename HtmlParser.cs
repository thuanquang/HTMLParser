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
            InitializeUI();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.Text = "HTML Parser";
            this.BackColor = Color.FromArgb(26, 32, 40); // Set form background to dark color


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

        private void InitializeUI()
        {

            // Cyan-based theme colors
            Color primaryCyan = Color.FromArgb(0, 188, 212); //Brighter Cyan
            Color darkCyan = Color.FromArgb(0, 150, 136); //Dark Cyan - Teal


            // Input TextBox with cyan theme
            textBoxInput.BackColor = Color.FromArgb(38, 50, 56); // Dark gray background
            textBoxInput.ForeColor = Color.WhiteSmoke;        // Light text
            textBoxInput.BorderStyle = BorderStyle.FixedSingle;
            textBoxInput.Font = new Font("Consolas", 10);  // Monospace font for code
            textBoxInput.Padding = new Padding(5);

            // Parse Button with cyan theme
            buttonParse.BackColor = primaryCyan; // Use primary cyan for button
            buttonParse.FlatStyle = FlatStyle.Flat;
            buttonParse.FlatAppearance.BorderSize = 0;
            buttonParse.ForeColor = Color.White;
            buttonParse.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            buttonParse.Cursor = Cursors.Hand;


            // TreeView with cyan theme and alternating row colors
            treeViewOutput.BackColor = Color.FromArgb(38, 50, 56); //TreeView Background
            treeViewOutput.ForeColor = Color.WhiteSmoke; //TreeView Foreground
            treeViewOutput.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            treeViewOutput.DrawNode += treeViewOutput_DrawNode;



            // Error TextBox with cyan theme
            textBoxError.BackColor = Color.FromArgb(192, 57, 43);  // Red-ish error background (adjust as needed)
            textBoxError.ForeColor = Color.White;            // White error text
            textBoxError.BorderStyle = BorderStyle.FixedSingle;
            textBoxError.Font = new Font("Segoe UI", 9);
            textBoxError.Padding = new Padding(5);



        }

        // Event handler for custom TreeView drawing
        // Custom TreeView drawing with alternating row colors and cyan highlight
        private void treeViewOutput_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null) return;

            // Set the background color based on even/odd rows
            Color backColor = e.Node.Index % 2 == 0 ? Color.FromArgb(38, 50, 56) : Color.FromArgb(53, 73, 94); //Alternating dark and slightly lighter color

            if ((e.State & TreeNodeStates.Selected) != 0)
            {
                // Highlight selected node with cyan
                backColor = Color.FromArgb(0, 128, 128); // Change background color when selected to a darker cyan
                e.Graphics.FillRectangle(Brushes.DarkCyan, e.Node.Bounds); // Fill the background of the selected node
                TextRenderer.DrawText(e.Graphics, e.Node.Text, e.Node.TreeView.Font, e.Bounds, Color.White, TextFormatFlags.GlyphOverhangPadding);
                return; // Prevent default drawing if selected
            }


            // Draw the background
            using (Brush backgroundBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            }

            // Draw the node text
            TextRenderer.DrawText(e.Graphics, e.Node.Text, e.Node.NodeFont ?? e.Node.TreeView.Font,
                e.Bounds, Color.WhiteSmoke, TextFormatFlags.GlyphOverhangPadding); //Lighter Color for text




            e.DrawDefault = false;
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
        public class MyQueue<T> : IEnumerable<T>
        {
            private LinkedList<T> list = new LinkedList<T>();

            public int Count => list.Count;

            public bool IsEmpty() => Count == 0;

            public void Enqueue(T item)
            {
                list.AddLast(item);
            }

            public T Dequeue()
            {
                if (IsEmpty())
                    throw new InvalidOperationException("Queue is empty.");
                var value = list.First.Value;
                list.RemoveFirst();
                return value;
            }

            public T Peek()
            {
                if (IsEmpty())
                    throw new InvalidOperationException("Queue is empty.");
                return list.First.Value;
            }

            public T PeekLast()
            {
                if (IsEmpty())
                    throw new InvalidOperationException("Queue is empty.");
                return list.Last.Value;
            }

            public T DequeueLast()
            {
                if (IsEmpty())
                    throw new InvalidOperationException("Queue is empty.");
                var value = list.Last.Value;
                list.RemoveLast();
                return value;
            }

            // Implement IEnumerable<T>
            public IEnumerator<T> GetEnumerator()
            {
                return list.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
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
                MyQueue<HtmlNode> openNodes = new MyQueue<HtmlNode>();
                openNodes.Enqueue(root);

                foreach (string token in tokens)
                {
                    if (string.IsNullOrWhiteSpace(token))
                        continue;

                    if (token.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
                    {
                        // Handle DOCTYPE declaration
                        HtmlNode doctypeNode = new HtmlNode { TagName = "!DOCTYPE" };
                        currentNode.Children.Add(doctypeNode);
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
                            openNodes.Enqueue(newNode);
                        }
                    }
                    else if (token.StartsWith("</"))
                    {
                        // Closing tag
                        string closingTagName = ExtractTagName(token);

                        if (openNodes.IsEmpty() || !openNodes.PeekLast().TagName.Equals(closingTagName, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new ParsingException($"Unmatched closing tag: {closingTagName}");
                        }

                        // Pop the matching node from the queue
                        HtmlNode matchingNode = openNodes.DequeueLast();
                        currentNode = matchingNode.Parent ?? root;
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
                if (openNodes.Count > 1) // More than just the root node
                {
                    throw new ParsingException("Unclosed tags found: " +
                        string.Join(", ", openNodes.Select(node => node.TagName)));
                }

                return root;
            }

            private string ExtractTagName(string tag)
            {
                string tagContent = tag.Trim('<', '>', '/').Split(' ')[0];
                return tagContent;
            }

            private Dictionary<string, string> ParseAttributes(string tag)
            {
                var attributes = new Dictionary<string, string>();
                int startIndex = tag.IndexOf(' ');

                if (startIndex == -1)
                    return attributes;

                string attributesPart = tag.Substring(startIndex).Trim('>', ' ');
                Regex attrRegex = new Regex(@"(\w+)\s*=\s*(?:""([^""]*)""|'([^']*)'|(\S+))");
                foreach (Match match in attrRegex.Matches(attributesPart))
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