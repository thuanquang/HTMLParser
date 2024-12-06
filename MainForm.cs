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

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void ParseButton_Click(object sender, EventArgs e)
        {
            try
            {
                string htmlContent = htmlInputTextBox.Text;

                // Parse using custom queue
                var parseResult = ParseHtmlWithQueue(htmlContent);
                parseOutputTextBox.Text = parseResult;

                // Local rendering
                RenderHtmlLocally(htmlContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing HTML: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ParseHtmlWithQueue(string htmlContent)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent);

            var elementQueue = new CustomQueue<HtmlNode>();
            var outputBuilder = new StringBuilder();

            // Initial enqueue of root document node
            elementQueue.Enqueue(doc.DocumentNode);

            while (elementQueue.Size > 0)
            {
                var currentNode = elementQueue.Dequeue();

                // Process current node
                if (!string.IsNullOrWhiteSpace(currentNode.Name))
                {
                    outputBuilder.AppendLine($"Element: {currentNode.Name}");

                    // Add attributes
                    foreach (var attr in currentNode.Attributes)
                    {
                        outputBuilder.AppendLine($"  Attribute: {attr.Name} = {attr.Value}");
                    }
                }

                // Enqueue child nodes
                foreach (var childNode in currentNode.ChildNodes)
                {
                    if (childNode.NodeType == HtmlNodeType.Element)
                    {
                        elementQueue.Enqueue(childNode);
                    }
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