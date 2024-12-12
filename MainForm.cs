using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using HtmlAgilityPack;

namespace HTMLParserApp
{
    public partial class MainForm : Form
    {
        private IContainer components = null;
        private RichTextBox htmlInputTextBox;
        private RichTextBox parseOutputTextBox;
        private Button parseButton;
        private Panel renderPanel;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {

            // Create components
            components = new Container();

            // HTML Input TextBox
            htmlInputTextBox = new RichTextBox
            {
                Dock = DockStyle.Top,
                Height = 250,
                Font = new Font("Consolas", 10),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            // Parse Button
            parseButton = new Button
            {
                Text = "Parse HTML",
                Dock = DockStyle.Top,
                BackColor = Color.DarkCyan,
                ForeColor = Color.White
            };
            parseButton.Click += ParseButton_Click;

            // Parse Output TextBox
            parseOutputTextBox = new RichTextBox
            {
                Dock = DockStyle.Right,
                Width = 300,
                ReadOnly = true,
                Font = new Font("Consolas", 10)
            };

            // Render Panel
            renderPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Form properties
            Text = "Local HTML Parser";
            Size = new Size(1200, 800);
            BackColor = Color.Cyan;

            // Add controls to form
            Controls.Add(renderPanel);
            Controls.Add(parseOutputTextBox);
            Controls.Add(parseButton);
            Controls.Add(htmlInputTextBox);
        }

        private void ParseButton_Click(object sender, EventArgs e)
        {
            try
            {
                string htmlContent = htmlInputTextBox.Text;

                // Parse using custom queue (refactored method)
                var parseResult = ParseHtmlWithCustomQueue(htmlContent);
                parseOutputTextBox.Text = parseResult;

                // Local rendering (no changes here)
                RenderHtmlLocally(htmlContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing HTML: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Refactored parsing method using CustomQueue
        private string ParseHtmlWithCustomQueue(string htmlContent)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();

            // Configure HAP to be less tolerant of errors
            doc.OptionFixNestedTags = false;  // Don't automatically fix mismatched tags
            doc.OptionAutoCloseOnEnd = false; // Don't automatically add closing tags

            try
            {
                doc.LoadHtml(htmlContent);

                // Check for parsing errors
                if (doc.ParseErrors != null && doc.ParseErrors.Any())
                {
                    StringBuilder errorBuilder = new StringBuilder();
                    errorBuilder.AppendLine("Lỗi phân tích HTML :");
                    foreach (var error in doc.ParseErrors)
                    {
                        errorBuilder.AppendLine($"  Dòng: {error.Line}, vị trí: {error.LinePosition}, Lí do: {error.Reason}");
                    }
                    MessageBox.Show(errorBuilder.ToString(), "Lỗi phân tích HTML", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "Error: Invalid HTML"; // Or handle the error as you see fit
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading HTML: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "Error: Invalid HTML"; // Or handle the error as you see fit
            }

            // If no errors, proceed with the parsing logic
            var queue = new CustomQueue<HtmlNode>();
            var outputBuilder = new StringBuilder();
            var depth = new Dictionary<HtmlNode, int>();

            queue.Enqueue(doc.DocumentNode);
            depth[doc.DocumentNode] = 0;

            while (!queue.IsEmpty())
            {
                var node = queue.Dequeue();
                int currentDepth = depth[node];

                outputBuilder.Append(' ', currentDepth * 2);

                if (node.NodeType == HtmlNodeType.Element)
                {
                    outputBuilder.AppendLine($"Element: {node.Name}");
                }
                else if (node.NodeType == HtmlNodeType.Text)
                {
                    string text = ((HtmlTextNode)node).Text.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        outputBuilder.AppendLine($"Text: {text}");
                    }
                }

                foreach (var child in node.ChildNodes)
                {
                    queue.Enqueue(child);
                    depth[child] = currentDepth + 1;
                }
            }

            return outputBuilder.ToString();
        }


        private void RenderHtmlLocally(string htmlContent)
        {
            // Create a temporary file for rendering
            string tempHtmlPath = Path.Combine(Path.GetTempPath(), "local_render.html");
            File.WriteAllText(tempHtmlPath, htmlContent);

            // Clear previous rendering
            renderPanel.Controls.Clear();

            // Create WebBrowser control for local rendering
            WebBrowser localBrowser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                ScriptErrorsSuppressed = true
            };

            localBrowser.Navigate(tempHtmlPath);
            renderPanel.Controls.Add(localBrowser);
        }
    }
}