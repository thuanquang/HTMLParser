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
        private RichTextBox DomTextBox;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {

            // Tạo các phần tử WinForm
            components = new Container();

            // HTML Input TextBox
            htmlInputTextBox = new RichTextBox
            {
                Dock = DockStyle.Top,
                Height = 280,
                Font = new Font("Consolas", 10),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            // Nút Parse
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
                Dock = DockStyle.Left,
                Width = 600,
                ReadOnly = true,
                Font = new Font("Consolas", 10)
            };

            // DOM Text Box
            DomTextBox = new RichTextBox
            {
                Dock = DockStyle.Right,
                Width = 600,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Font = new Font("Consolas", 10)
            };

            // Form properties
            Text = "Local HTML Parser";
            Size = new Size(1200, 800);
            BackColor = Color.Cyan;


            Controls.Add(DomTextBox);
            Controls.Add(parseOutputTextBox);
            Controls.Add(parseButton);
            Controls.Add(htmlInputTextBox);
        }

        private void ParseButton_Click(object sender, EventArgs e)
        {
            try
            {
                string htmlContent = htmlInputTextBox.Text;

                // tạo một instance check lỗi
                HtmlErrorParser htmlErrorParser = new HtmlErrorParser();

                // check lỗi
                var errors = htmlErrorParser.DetectHtmlErrors(htmlContent);

                // nếu như có lỗi, hiện thông báo lỗi và ngưng tiến trình hiện tại
                if (errors.Any())
                {
                    // tạo thông báo lỗi
                    string errorMessage = "Critical HTML Errors Detected:\n\n" +
                        string.Join("\n", errors.Take(10)); // Show 10 lỗi đầu tiên

                    // show thông báo lỗi
                    DialogResult result = MessageBox.Show(
                        errorMessage,
                        "HTML Parsing Stopped",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    // dọn sạch output và dừng xử lí
                    parseOutputTextBox.Clear();
                    DomTextBox.Controls.Clear();
                    return; // thoát method, và dừng xử lí
                }

                // nếu không có lỗi nghiêm trọng, tiếp tục parse
                var parseResult = ParseHtmlWithCustomQueue(htmlContent);
                parseOutputTextBox.Text = parseResult;

                var parseResult2 = DomTree(htmlContent);
                DomTextBox.Text = parseResult2;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing HTML: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Phương thức phân tích cú pháp được cấu trúc bằng CustomQueue
        private string ParseHtmlWithCustomQueue(string htmlContent)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();

            // Cấu hình để không tự sửa lỗi HTML
            doc.OptionFixNestedTags = false;
            doc.OptionAutoCloseOnEnd = false;

            // Tải HTML và kiểm tra lỗi phân tích cú pháp
            doc.LoadHtml(htmlContent);

            // Khởi tạo hàng đợi và output
            var queue = new CustomQueue<HtmlNode>();
            var outputBuilder = new StringBuilder();
            // Khởi tạo hàng đợi với nút gốc
            queue.Enqueue(doc.DocumentNode);

            // Duyệt qua cây DOM
            while (!queue.IsEmpty())
            {
                var node = queue.Dequeue();


                // Hiển thị phần tử (Element)
                if (node.NodeType == HtmlNodeType.Element)
                {

                    outputBuilder.AppendLine($"Element: {node.Name}");

                    // Xử lý các TextNode ngay sau ElementNode
                    foreach (var childNode in node.ChildNodes)
                    {
                        if (childNode.NodeType == HtmlNodeType.Text)
                        {
                            string text = childNode.InnerText.Trim();
                            if (!string.IsNullOrEmpty(text))
                            {

                                outputBuilder.AppendLine($"Text: {text}");
                            }
                        }
                        else
                        {
                            queue.Enqueue(childNode);

                        }
                    }
                }
                else
                {
                    // Nếu là TextNode, in ra và không enqueue thêm
                    string text = node.InnerText.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {

                        outputBuilder.AppendLine($"{text}");
                    }
                }
            }

            return outputBuilder.ToString();
        }

        private string DomTree(string htmlContent)
        {
            DomTextBox.Controls.Clear();
            var doc = new HtmlAgilityPack.HtmlDocument();


            doc.OptionFixNestedTags = false;
            doc.OptionAutoCloseOnEnd = false;         
            doc.LoadHtml(htmlContent);
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
    }
}