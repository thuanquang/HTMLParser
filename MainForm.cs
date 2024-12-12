using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

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

                // Create an instance of HtmlErrorParser
                HtmlErrorParser htmlErrorParser = new HtmlErrorParser();

                // Perform comprehensive error checking first
                var errors = htmlErrorParser.DetectHtmlErrors(htmlContent);

                // If any critical errors are found, show error dialog and stop processing
                if (errors.Any())
                {
                    // Create a detailed error message
                    string errorMessage = "Critical HTML Errors Detected:\n\n" +
                        string.Join("\n", errors.Take(10)); // Show first 10 errors

                    // Show error dialog
                    DialogResult result = MessageBox.Show(
                        errorMessage,
                        "HTML Parsing Stopped",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    // Clear output and stop further processing
                    parseOutputTextBox.Clear();
                    renderPanel.Controls.Clear();
                    return; // Exit the method, effectively stopping further processing
                }

                // If no critical errors, proceed with parsing
                var parseResult = ParseHtmlWithCustomQueue(htmlContent);
                parseOutputTextBox.Text = parseResult;

                // Local rendering
                RenderHtmlLocally(htmlContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing HTML: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Refactored parsing method using CustomQueue
        private string ParseHtmlWithCustomQueue(string htmlContent)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();

            // Cấu hình để không tự sửa lỗi HTML
            doc.OptionFixNestedTags = false;
            doc.OptionAutoCloseOnEnd = false;

            // Tải HTML và kiểm tra lỗi phân tích cú pháp
            doc.LoadHtml(htmlContent);

            if (doc.ParseErrors != null && doc.ParseErrors.Any())
            {
                StringBuilder errorBuilder = new StringBuilder();
                errorBuilder.AppendLine("HTML Parsing Errors:");
                foreach (var error in doc.ParseErrors)
                {
                    errorBuilder.AppendLine($"  Line: {error.Line}, Position: {error.LinePosition}, Reason: {error.Reason}");
                }
                MessageBox.Show(errorBuilder.ToString(), "HTML Parsing Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "Error: Invalid HTML";
            }

            // Khởi tạo hàng đợi và output
            var queue = new CustomQueue<HtmlNode>();
            var outputBuilder = new StringBuilder();
            var depth = new Dictionary<HtmlNode, int>();

            // Khởi tạo hàng đợi với nút gốc
            queue.Enqueue(doc.DocumentNode);
            depth[doc.DocumentNode] = 0;

            // Duyệt qua cây DOM
            while (!queue.IsEmpty())
            {
                var node = queue.Dequeue();
                int currentDepth = depth[node];

                // Hiển thị phần tử (Element)
                if (node.NodeType == HtmlNodeType.Element)
                {
                    outputBuilder.Append(' ', currentDepth * 2); // Thụt lề theo độ sâu
                    outputBuilder.AppendLine($"Element: {node.Name}");

                    // Xử lý các TextNode ngay sau ElementNode
                    foreach (var childNode in node.ChildNodes)
                    {
                        if (childNode.NodeType == HtmlNodeType.Text)
                        {
                            string text = childNode.InnerText.Trim();
                            if (!string.IsNullOrEmpty(text))
                            {
                                outputBuilder.Append(' ', (currentDepth + 1) * 2); // Thụt lề cho TextNode
                                outputBuilder.AppendLine($"Text: {text}");
                            }
                        }
                        else
                        {
                            queue.Enqueue(childNode);
                            depth[childNode] = currentDepth + 1;
                        }
                    }
                }
                else
                {
                    // Nếu là TextNode, in ra và không enqueue thêm
                    string text = node.InnerText.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        outputBuilder.Append(' ', currentDepth * 2); // Thụt lề cho TextNode
                        outputBuilder.AppendLine($"Text: {text}");
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