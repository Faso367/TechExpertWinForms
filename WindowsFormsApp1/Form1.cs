using Kodeks;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel _htmlPanel;
        private readonly TechexpertClient _client;
        private readonly List<DocListItem> _dataSource = new List<DocListItem>();

        private DocListInfo _docListInfo = null;

        public Form1()
        {
            InitializeComponent();

            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress("http://192.168.0.14:81/docs/api");
            _client = new TechexpertClient(binding, endpoint);

            _htmlPanel = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
            _htmlPanel.Dock = DockStyle.Fill;
            _htmlPanel.LinkClicked += htmlPanel_LinkClicked;
            Controls.Add(_htmlPanel);

            btnSearch.Click += SearchButton_Click;
            txtSearch.KeyDown += TextBoxSearch_KeyDown;
        }

        private async void htmlPanel_LinkClicked(object sender, TheArtOfDev.HtmlRenderer.Core.Entities.HtmlLinkClickedEventArgs e)
        {
            try
            {
                if (e.Link == "more")
                {
                    e.Handled = true;
                    if (e.Attributes.ContainsKey("part") && int.TryParse(e.Attributes["part"], out int part))
                    {
                        var (items, nextPart) = await LoadDataAsync(part);
                        _dataSource.AddRange(items);

                        BuildHtml(nextPart);
                    }
                }
                else if (e.Link == "default")
                {
                    e.Handled = true;
                }
                else if (e.Link == "kodeks")
                {
                    e.Handled = true;

                    var nd = e.Attributes.ContainsKey("nd") ? e.Attributes["nd"] : null;
                    var mark = e.Attributes.ContainsKey("mark") ? e.Attributes["mark"] : null;
                    await _client.RunKodeks(nd, mark);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message);
            }
        }

        private async void SearchButton_Click(object sender, EventArgs e)
        {
            try
            {
                _docListInfo = null;
                _dataSource.Clear();

                var (items, nextPart) = await LoadDataAsync();
                _dataSource.AddRange(items);

                BuildHtml(nextPart);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message);
            }
        }

        private void TextBoxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchButton_Click(btnSearch, EventArgs.Empty);
            }
        }

        private void BuildHtml(int? nextPart)
        {
            List<string> builder = new List<string>();

            foreach (var item in _dataSource)
            {
                builder.Add($"<p>{FormatedTitle(item.Name, item.Nd)}<p/>{FormatedText(item.Info)}<a href=\"default\" class=\"button\" nd=\"{item.Nd}\">По умолчанию</a>");
            }

            var scrollPosition = _htmlPanel.VerticalScroll.Value;

            _htmlPanel.Text = @$"
                <html>
                  <style>
                    body {{
                      margin: 10px;
                    }}
                    p {{
                      margin-top: 0px;
                      margin-bottom: 0px;
                    }}
                    span {{
                      display: block;
                      margin-left: 20px;
                    }}
                    a {{
                      text-decoration: none;
                    }}
                    .button {{
                      margin-top: 20px;
                      margin-left: 20px;
                      text-decoration: none;
                      background-color: #ffffff;
                      color: #333333;
                      padding: 5px 10px 5px 10px;
                      border: 1px solid gray;
                    }}
                  </style>
                  <body>
                    {string.Join("<hr/>", builder)}
                    {(nextPart != null ? $"<hr/><a href=\"more\" part=\"{nextPart}\">Показать еще</a>" : "")}
                  </body>
                </html>";

            if (_htmlPanel.VerticalScroll.Maximum > scrollPosition)
            {
                _htmlPanel.VerticalScroll.Value = scrollPosition;
                _htmlPanel.PerformLayout();
            }
        }

        private async Task<(DocListItem[], int?)> LoadDataAsync(int part = 0)
        {
            var docListInfo = await GetDocListInfoAsync(txtSearch.Text);
            var items = await _client.GetSearchListNAsync(docListInfo.Id, null, part, null, null, false);
            var nextPart = docListInfo.Parts > part + 1 ? (int?)(part + 1) : null;
            return (items.ArrayOfDocListItem, nextPart);
        }

        private string FormatedTitle(string text, int nd)
        {
            return Regex.Replace(text, @"^(.*?\(часть.*?\)|^[^(]*)(.*)", $"<a href=\"kodeks\" nd=\"{nd}\"><b>${{1}}</b></a>${{2}}");
        }

        private string FormatedText(string text)
        {
            string formatedText = Regex.Replace(text, @"^(.*)(<br>(.*)|$)", "<i style=\"color: gray\">${1}</i><br/>${2}");
            formatedText = Regex.Replace(formatedText, @"<a\shref=""([^""]*)""\snd=""(\d+)""\smark=""(\w+)"">", @"<a href=""kodeks"" nd=""${2}"" mark=""${3}"">");
            return formatedText;
        }

        private async Task<DocListInfo> GetDocListInfoAsync(string searchText)
        {
            if (_docListInfo == null)
            {
                var bparser = await _client.GetbparserAsync(searchText);
                _docListInfo = await _client.FuzzySearchAsync(searchText, null, null, "searchbynames", bparser);
            }
            return _docListInfo;
        }
    }
}