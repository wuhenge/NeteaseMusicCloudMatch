using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace NeteaseMusicCloudMatch
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public static string unikey = string.Empty, wyCookie = string.Empty, userId = string.Empty;

        private void Form1_Load(object sender, EventArgs e)
        {
            string LoginCheck = CommonHelper.Read("NeteaseMusic", "LoginCheck");
            if (!string.IsNullOrWhiteSpace(LoginCheck))
            {
                checkBox1.Checked = Convert.ToBoolean(LoginCheck);
            }

            if (checkBox1.Checked)
            {
                wyCookie = CommonHelper.Read("NeteaseMusic", "Cookie");
                if (!string.IsNullOrEmpty(wyCookie))
                {
                    LoadUIDName();
                    LoadCloudInfo();
                    button2_Click(sender, null);
                    timer1.Enabled = false;
                }
                else
                {
                    LoadQrCodeImage();
                }
            }
            else
            {
                LoadQrCodeImage();
            }

            LoadDgvColumns();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            CommonHelper.Write("NeteaseMusic", "LoginCheck", checkBox1.Checked.ToString());
        }

        #region dataGridView1 加载标题
        private void LoadDgvColumns()
        {
            dataGridView1.RowHeadersVisible = false;
            DataGridViewTextBoxColumn colListId = new DataGridViewTextBoxColumn();
            colListId.Name = "colListId";
            //colListId.Width = 40;
            colListId.HeaderText = "#";
            colListId.ReadOnly = true;

            DataGridViewTextBoxColumn colSongId = new DataGridViewTextBoxColumn();
            colSongId.Name = "colSongId";
            //colSongId.Width = 80;
            colSongId.HeaderText = "ID";
            colSongId.ReadOnly = true;

            DataGridViewTextBoxColumn colFileName = new DataGridViewTextBoxColumn();
            colFileName.Name = "colFileName";
            //colFileName.Width = 200;
            colFileName.HeaderText = "文件名称";
            colFileName.ReadOnly = true;

            DataGridViewTextBoxColumn colFileSize = new DataGridViewTextBoxColumn();
            colFileSize.Name = "colFileSize";
            //colFileSize.Width = 68;
            colFileSize.HeaderText = "大小";
            colFileSize.ReadOnly = true;

            DataGridViewTextBoxColumn colAddTime = new DataGridViewTextBoxColumn();
            colAddTime.Name = "colAddTime";
            //colAddTime.Width = 130;
            colAddTime.HeaderText = "上传时间";
            colAddTime.ReadOnly = true;

            dataGridView1.Columns.AddRange(
                new DataGridViewColumn[] {
                                    colListId, colSongId, colFileName, colFileSize, colAddTime
                });

            dataGridView1.Columns[0].FillWeight = 5;
            dataGridView1.Columns[1].FillWeight = 15;
            dataGridView1.Columns[2].FillWeight = 30;
            dataGridView1.Columns[3].FillWeight = 10;
            dataGridView1.Columns[4].FillWeight = 20;
        }
        #endregion

        #region 加载二维码图片
        private void LoadQrCodeImage()
        {
            try
            {
                string apiUrl = "https://music.163.com/api/login/qrcode/unikey?type=1";
                string html = CommonHelper.GetHtml(apiUrl);
                if (CommonHelper.CheckJson(html))
                {
                    var json = JObject.Parse(html);
                    if (json["code"]?.ToString() == "200")
                    {
                        unikey = json["unikey"]?.ToString();
                        string QrCodeUrl = "https://music.163.com/login?codekey=" + unikey;
                        pictureBox1.Image = CommonHelper.QrCodeCreate(QrCodeUrl);
                    }
                    else
                    {
                        MessageBox.Show("生成二维码unikey出错", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 加载UID和Name
        private void LoadUIDName()
        {
            try
            {
                string apiUrl = "https://music.163.com/api/nuser/account/get";
                string html = CommonHelper.GetHtml(apiUrl, wyCookie);
                if (CommonHelper.CheckJson(html))
                {
                    var json = JObject.Parse(html);
                    if (json["code"]?.ToString() == "200")
                    {
                        userId = json["profile"]?["userId"]?.ToString();
                        string nickname = json["profile"]?["nickname"]?.ToString();
                        string avatarUrl = json["profile"]?["avatarUrl"]?.ToString();
                        label1.Text = "UID：" + userId + "，Name：" + nickname;
                        pictureBox1.Image = CommonHelper.GetImage(avatarUrl);
                    }
                }
                else
                {
                    MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 加载音乐网盘信息
        private void LoadCloudInfo()
        {
            try
            {
                string apiUrl = "https://music.163.com/api/v1/cloud/get?limit=0";
                string html = CommonHelper.GetHtml(apiUrl, wyCookie);
                if (CommonHelper.CheckJson(html))
                {
                    var json = JObject.Parse(html);
                    if (json["code"]?.ToString() == "200")
                    {
                        string size = json["size"]?.ToString();
                        string maxSize = json["maxSize"]?.ToString();
                        size = CommonHelper.GetFileSize(Convert.ToInt64(size));
                        maxSize = CommonHelper.GetFileSize(Convert.ToInt64(maxSize));
                        label2.Text = "音乐云盘容量：" + size + "  /  " + maxSize;
                    }
                }
                else
                {
                    MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 检测扫码状态 / 重新扫码登录
        private void timer1_Tick(object sender, EventArgs e)
        {
            string apiUrl = "https://music.163.com/api/login/qrcode/client/login?type=1&key=" + unikey;

            HttpHelper http = new HttpHelper();
            HttpItem item = new HttpItem()
            {
                URL = apiUrl,
                Method = "get",
                ContentType = "application/json;charset=UTF-8",
                Referer = apiUrl,
                ResultType = ResultType.String
            };
            HttpResult result = http.GetHtml(item);

            string html = result.Html;
            if (CommonHelper.CheckJson(html))
            {
                var json = JObject.Parse(html);
                string code = json["code"]?.ToString();
                string message = json["message"]?.ToString();
                if (code == "800")
                {
                    wyCookie = string.Empty;
                    LoadQrCodeImage();
                }
                else if (code == "803")
                {
                    wyCookie = result.Cookie.Replace(",", ";");
                    CommonHelper.Write("NeteaseMusic", "Cookie", wyCookie);

                    LoadUIDName();
                    LoadCloudInfo();
                    button2_Click(sender, null);

                    timer1.Enabled = false;
                }
                string messStr = code + ", " + message;
                Console.WriteLine(messStr);
                //MessageBox.Show(messStr);
            }
            else
            {
                MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("确定要重新扫码登录吗？", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                wyCookie = string.Empty;
                label1.Text = string.Empty;
                label2.Text = string.Empty;

                LoadQrCodeImage();

                timer1.Enabled = true;
            }
        }
        #endregion

        #region 读取音乐网盘内容
        private void button2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(wyCookie))
            {
                pageIndex = 1;
                Thread thread = new Thread(GetCloudData);
                thread.Start();
            }
            else
            {
                MessageBox.Show("还没登录呢", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        int pageIndex = 1;
        private void GetCloudData()
        {
            try
            {
                if (pageIndex <= 1)
                {
                    this.Invoke(new MethodInvoker(delegate ()
                    {
                        dataGridView1.Rows.Clear();
                    }));
                }
                int limit = 200;
                string apiUrl = "https://music.163.com/api/v1/cloud/get?limit=" + limit + "&offset=" + (pageIndex - 1) * limit;
                string html = CommonHelper.GetHtml(apiUrl, wyCookie);
                if (CommonHelper.CheckJson(html))
                {
                    var json = JObject.Parse(html);
                    if (json["code"]?.ToString() == "200")
                    {
                        if (json["count"]?.Value<int>() > 0)
                        {
                            var jarr = JArray.Parse(json["data"]?.ToString());
                            for (int i = 0; i < jarr.Count; i++)
                            {
                                var j = JObject.Parse(jarr[i].ToString());
                                string songId = j["songId"]?.ToString();
                                string fileName = j["fileName"]?.ToString();
                                string fileSize = j["fileSize"]?.ToString();
                                string addTime = j["addTime"]?.ToString();
                                int index = 0;
                                this.Invoke(new MethodInvoker(delegate ()
                                {
                                    index = dataGridView1.Rows.Add();
                                    dataGridView1.Rows[index].Cells[0].Value = dataGridView1.Rows.Count;
                                    dataGridView1.Rows[index].Cells[1].Value = songId;
                                    dataGridView1.Rows[index].Cells[2].Value = fileName;
                                    dataGridView1.Rows[index].Cells[3].Value = CommonHelper.GetFileSize(Convert.ToInt64(fileSize));
                                    dataGridView1.Rows[index].Cells[4].Value = CommonHelper.UnixTimestampToDateTime(addTime);
                                }));
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }

        private void ScrollReader(object sender, ScrollEventArgs e)
        {
            if (e.NewValue + dataGridView1.DisplayedRowCount(false) >= dataGridView1.RowCount)
            {
                pageIndex++;
                Thread thread = new Thread(GetCloudData);
                thread.Start();
            }
        }

        #endregion

        #region 音乐云盘搜索
        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                string searchValue = textBox3.Text.Trim();
                if (string.IsNullOrWhiteSpace(searchValue))
                {
                    MessageBox.Show("请先输入要搜索的内容", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                DataTable dataTable = DataGridViewToDataTable(dataGridView1);
                DataRow[] dataRows = dataTable.Select("文件名称 like '%" + searchValue + "%'");
                if (dataRows.Length > 0)
                {
                    foreach (var dataRow in dataRows)
                    {
                        dataGridView1.Rows.Clear();
                        int index = dataGridView1.Rows.Add();
                        dataGridView1.Rows[index].Cells[0].Value = dataRow[0].ToString();
                        dataGridView1.Rows[index].Cells[1].Value = dataRow[1].ToString();
                        dataGridView1.Rows[index].Cells[2].Value = dataRow[2].ToString();
                        dataGridView1.Rows[index].Cells[3].Value = dataRow[3].ToString();
                        dataGridView1.Rows[index].Cells[4].Value = dataRow[4].ToString();
                    }
                }
                else
                {
                    MessageBox.Show("未找到关于“" + searchValue + "”的内容", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region DataGridView转DataTable
        private DataTable DataGridViewToDataTable(DataGridView dgv)
        {
            DataTable dt = new DataTable();
            try
            {
                // 循环列标题名称，处理了隐藏的行不显示
                for (int count = 0; count < dgv.Columns.Count; count++)
                {
                    if (dgv.Columns[count].Visible == true)
                    {
                        dt.Columns.Add(dgv.Columns[count].HeaderText.ToString());
                    }
                }

                // 循环行，处理了隐藏的行不显示
                for (int count = 0; count < dgv.Rows.Count; count++)
                {
                    DataRow dr = dt.NewRow();
                    int curr = 0;
                    for (int countsub = 0; countsub < dgv.Columns.Count; countsub++)
                    {
                        if (dgv.Columns[countsub].Visible == true)
                        {
                            if (dgv.Rows[count].Cells[countsub].Value != null)
                            {
                                dr[curr] = dgv.Rows[count].Cells[countsub].Value.ToString();
                            }
                            else
                            {
                                dr[curr] = "";
                            }
                            curr++;
                        }
                    }
                    dt.Rows.Add(dr);
                }
                return dt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
                return dt;
            }
        }
        #endregion

        #region dataGridView1 选中赋值给sid
        private void dataGridView1_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.Rows.Count > 0)
                {
                    string sid = dataGridView1.SelectedRows[0].Cells[1].Value.ToString();
                    textBox1.Text = sid;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 判断云盘文件是否存在
        private bool CheckCloudFileStatus(string songId)
        {
            try
            {
                string apiUrl = "https://music.163.com/api/cloud/get/byids?songIds=[" + songId + "]";
                string html = CommonHelper.GetHtml(apiUrl, wyCookie);
                if (CommonHelper.CheckJson(html))
                {
                    var json = JObject.Parse(html);
                    if (json["code"]?.ToString() == "200")
                    {
                        var jarr = JArray.Parse(json["data"]?.ToString());
                        if (jarr.Count > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        #endregion

        #region 判断歌曲是否存在
        private int CheckSongStatus(string songId)
        {
            try
            {
                if (songId == "0")
                {
                    DialogResult result = MessageBox.Show("确定要取消匹配吗？", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    string apiUrl = "https://music.163.com/api/song/detail/?ids=[" + songId + "]";
                    string html = CommonHelper.GetHtml(apiUrl, wyCookie);
                    if (CommonHelper.CheckJson(html))
                    {
                        var json = JObject.Parse(html);
                        if (json["code"]?.ToString() == "200")
                        {
                            var jarr = JArray.Parse(json["songs"]?.ToString());
                            if (jarr.Count > 0)
                            {
                                return 1;
                            }
                            else
                            {
                                return 0;
                            }
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
                return -1;
            }
        }
        #endregion

        #region 匹配纠正
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                string uid = userId;
                string sid = textBox1.Text.Trim();
                string asid = textBox2.Text.Trim();

                if (string.IsNullOrEmpty(sid))
                {
                    MessageBox.Show("请选择云盘文件", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else if (string.IsNullOrEmpty(asid))
                {
                    MessageBox.Show("请输入歌曲ID", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else if (sid.Equals(asid))
                {
                    MessageBox.Show("已经匹配成功，无需再次匹配。", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    return;
                }
                if (CheckCloudFileStatus(sid))
                {
                    if (CheckSongStatus(asid) == 1)
                    {
                        string apiUrl = "https://music.163.com/api/cloud/user/song/match?userId=" + uid + "&songId=" + sid + "&adjustSongId=" + asid;
                        string html = CommonHelper.GetHtml(apiUrl, wyCookie);
                        Console.WriteLine(html);
                        if (CommonHelper.CheckJson(html))
                        {
                            var json = JObject.Parse(html);
                            if (json["code"]?.ToString() == "200")
                            {
                                MessageBox.Show("匹配纠正成功！");

                                button2_Click(sender, null);
                            }
                            else
                            {
                                MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else if (CheckSongStatus(asid) == 0)
                    {
                        MessageBox.Show("输入的歌曲ID不存在", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("云盘文件不存在", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 版权信息
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = "https://www.52pojie.cn/home.php?mod=space&uid=381706";
            try
            {
                //调用默认浏览器
                System.Diagnostics.Process.Start(url);

                ////从注册表中读取默认浏览器可执行文件路径  
                //RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"http\shell\open\command\");
                //string s = key.GetValue("").ToString();
                ////s就是你的默认浏览器，不过后面带了参数，把它截去，不过需要注意的是：不同的浏览器后面的参数不一样！  
                ////"D:\Program Files (x86)\Google\Chrome\Application\chrome.exe" -- "%1"  
                //System.Diagnostics.Process.Start(s.Substring(0, s.Length - 8), url);
            }
            catch (Exception)
            {
                //调用IE浏览器
                System.Diagnostics.Process.Start("iexplore.exe", url);
            }
        }
        #endregion

    }
}
