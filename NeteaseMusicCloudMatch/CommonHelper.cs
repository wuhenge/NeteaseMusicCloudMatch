using Gma.QrCodeNet.Encoding;
using Gma.QrCodeNet.Encoding.Windows.Render;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;

namespace NeteaseMusicCloudMatch
{
    public static class CommonHelper
    {

        #region 获取网页源码
        /// <summary>
        /// 获取网页源码
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public static string GetHtml(string url, string cookie = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(url))
                {
                    if (url.Contains("?"))
                    {
                        url = url + "&timestamp=" + GetTimestamp();
                    }
                    else
                    {
                        url = url + "?timestamp=" + GetTimestamp();
                    }
                }
                HttpHelper http = new HttpHelper();
                HttpItem item = new HttpItem()
                {
                    URL = url,
                    Method = "get",
                    Cookie = cookie,
                    ContentType = "application/x-www-form-urlencoded",
                    Referer = url,
                    ResultType = ResultType.String
                };
                HttpResult result = http.GetHtml(item);
                return result.Html;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
                return null;
            }
        }
        #endregion

        #region 生成时间戳
        public static string GetTimestamp()
        {
            Random rd = new Random();
            var rdNext = rd.Next(100000, 1000000);
            return (GetTimeSpan(true) + rdNext).ToString() + rdNext.ToString();
        }

        /// <summary>
        /// 生成时间戳
        /// </summary>
        /// <returns></returns>
        public static long GetTimeSpan(bool isMills = false)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64((DateTime.UtcNow.Ticks - startTime.Ticks) / (isMills ? 10000 : 10000000));
        }
        #endregion

        #region 生成二维码
        public static Bitmap QrCodeCreate(string content)
        {
            QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.Q);
            QrCode qrCode = qrEncoder.Encode(content);
            DrawingBrushRenderer dRenderer = new DrawingBrushRenderer(new FixedModuleSize(6, QuietZoneModules.Two));
            MemoryStream ms = new MemoryStream();
            dRenderer.WriteToStream(qrCode.Matrix, ImageFormatEnum.PNG, ms);
            Bitmap bmp = new Bitmap(ms);
            return bmp;
        }
        #endregion

        #region Base64转图片
        /// <summary>
        /// Base64转图片
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public static Bitmap Base64ToBitmap(string base64)
        {
            base64 = base64.Replace("data:image/png;base64,", "").Replace("data:image/jpg;base64,", "").Replace("data:image/jpeg;base64,", "").Replace("data:image/bmp;base64,", "");
            byte[] bytes = Convert.FromBase64String(base64);
            MemoryStream ms = new MemoryStream(bytes);
            Bitmap bmp = new Bitmap(ms);
            return bmp;
        }
        #endregion

        #region URL转图片
        /// <summary>
        /// URL转图片
        /// </summary>
        /// <param name="URL"></param>
        /// <returns></returns>
        public static Image GetImage(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Proxy = null;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    return Image.FromStream(stream);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        #endregion

        #region 文件大小转换
        public static string GetFileSize(float Len)
        {
            float temp = Len;
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (temp >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                temp = temp / 1024;
            }
            return string.Format("{0:0.##} {1}", temp, sizes[order]);
        }
        #endregion

        #region 时间戳转日期
        public static DateTime UnixTimestampToDateTime(string timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }
        #endregion

        #region 判断Json是否有效
        /// <summary>
        /// 验证JSON有效性
        /// </summary>
        /// <param name="strJson"></param>
        /// <returns></returns>
        public static bool CheckJson(string strJson)
        {
            try
            {
                if (string.IsNullOrEmpty(strJson))
                {
                    return false;
                }
                else if (strJson.StartsWith("{"))
                {
                    JObject j = JObject.Parse(strJson);
                    return true;
                }
                else if (strJson.StartsWith("["))
                {
                    JArray j = JArray.Parse(strJson);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion

        #region ini文件操作
        static string ConfigFilePath = Application.StartupPath + "\\Config.ini";
        /// <summary>
        /// 为INI文件中指定的节点取得字符串
        /// </summary>
        /// <param name="lpAppName">欲在其中查找关键字的节点名称</param>
        /// <param name="lpKeyName">欲获取的项名</param>
        /// <param name="lpDefault">指定的项没有找到时返回的默认值</param>
        /// <param name="lpReturnedString">指定一个字串缓冲区，长度至少为nSize</param>
        /// <param name="nSize">指定装载到lpReturnedString缓冲区的最大字符数量</param>
        /// <param name="lpFileName">INI文件完整路径</param>
        /// <returns>复制到lpReturnedString缓冲区的字节数量，其中不包括那些NULL中止字符</returns>
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);

        /// <summary>
        /// 修改INI文件中内容
        /// </summary>
        /// <param name="lpApplicationName">欲在其中写入的节点名称</param>
        /// <param name="lpKeyName">欲设置的项名</param>
        /// <param name="lpString">要写入的新字符串</param>
        /// <param name="lpFileName">INI文件完整路径</param>
        /// <returns>非零表示成功，零表示失败</returns>
        [DllImport("kernel32")]
        private static extern int WritePrivateProfileString(string lpApplicationName, string lpKeyName, string lpString, string lpFileName);

        /// <summary>
        /// 读取INI文件值
        /// </summary>
        /// <param name="section">节点名</param>
        /// <param name="key">键</param>
        /// <param name="def">未取到值时返回的默认值</param>
        /// <param name="filePath">INI文件完整路径</param>
        /// <returns>读取的值</returns>
        public static string Read(string section, string key, string def = null)
        {
            StringBuilder sb = new StringBuilder(1024);
            GetPrivateProfileString(section, key, def, sb, 1024, ConfigFilePath);
            if (string.IsNullOrWhiteSpace(sb.ToString()))
            {
                return "false";
            }
            return sb.ToString();
        }

        /// <summary>
        /// 写入INI文件值
        /// </summary>
        /// <param name="section">欲在其中写入的节点名称</param>
        /// <param name="key">欲设置的项名</param>
        /// <param name="value">要写入的新字符串</param>
        /// <param name="filePath">INI文件完整路径</param>
        /// <returns>非零表示成功，零表示失败</returns>
        public static int Write(string section, string key, string value)
        {
            return WritePrivateProfileString(section, key, value, ConfigFilePath);
        }
        #endregion

    }
}
