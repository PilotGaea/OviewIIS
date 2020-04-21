using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using PilotGaea.TMPEngine;

namespace OviewWeb
{
    /// <summary>
    /// Oview 的摘要描述
    /// </summary>
    public class Oview : IHttpHandler
    {
        static CServer m_Server = null;

        public Oview()
        {
            if (m_Server == null)
            {
                m_Server = new CServer();
                Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + @"C:\Program Files\PilotGaea\TileMap");
                if (m_Server.Init(8080, @"C:\ProgramData\PilotGaea\PGMaps\地圖伺服器#01\Map.TMPX", @"C:\Program Files\PilotGaea\TileMap\plugins"))
                {
                    m_Server.Start();
                }
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            string url = HttpUtility.UrlDecode(context.Request.QueryString.ToString());

            if (url == "")
            {
                context.Response.Write("PilotGaea O'view Map Server(" + (IntPtr.Size * 8).ToString() + " bits) " + (m_Server.IsStart() ? "Start" : "Stop"));
                if (!m_Server.IsStart())
                {
                    context.Response.Write(m_Server.GetLastError());
                }
            }
            else
            {
                //轉給mapserver
                //判斷是GET還是POST
                bool bPOST = context.Request.InputStream.Length > 0;
                byte[] postBuffer = null;
                if (bPOST)//取得POST資料
                {
                    postBuffer = new byte[context.Request.InputStream.Length];
                    context.Request.InputStream.Read(postBuffer, 0, postBuffer.Length);
                }
                System.Net.HttpWebRequest req = System.Net.WebRequest.Create(url) as System.Net.HttpWebRequest;
                req.Method = bPOST ? "POST" : "GET";
                if (bPOST)
                {
                    req.ContentLength = postBuffer.Length;
                    req.ContentType = "application/json";
                }
                req.UserAgent = "PilotGaea Proxy Server";

                //複製檔頭的cookie
                System.Collections.Specialized.NameValueCollection headers = context.Request.Headers;
                for (int i = 0; i < headers.Count; i++)
                {
                    //context.Response.Write(headers.GetKey(i) + ":" + headers.Get(i) + "<BR>");
                    if (headers.GetKey(i) == "Cookie")
                    {
                        req.Headers.Set("Cookie", headers.Get(i));
                        break;
                    }
                }

                byte[] retBuffer = null;
                try
                {
                    if (bPOST)
                    {
                        //上傳
                        System.IO.Stream stream_req = req.GetRequestStream();
                        stream_req.Write(postBuffer, 0, postBuffer.Length);
                        stream_req.Flush();
                        stream_req.Close();
                    }

                    System.Net.HttpWebResponse res = req.GetResponse() as System.Net.HttpWebResponse;
                    System.IO.Stream stream_res = res.GetResponseStream();
                    int len = 1024 * 1024;
                    byte[] tmpBuffer = new byte[len];//一次1MB
                    int readlen = 0;
                    System.IO.MemoryStream ms = new System.IO.MemoryStream();
                    do
                    {
                        readlen = stream_res.Read(tmpBuffer, 0, len);
                        ms.Write(tmpBuffer, 0, readlen);
                    }
                    while (readlen > 0);
                    retBuffer = ms.ToArray();

                    //複製回應的檔頭
                    for (int i = 0; i < res.Headers.Count; i++)
                    {
                        string[] s = res.Headers.GetValues(i);
                        string k = res.Headers.Keys[i];
                        string v = "";
                        for (int j = 0; j < s.Length; j++)
                        {
                            if (j > 0) v += ";";
                            v += s[j];
                        }
                        if (k == "Content-Type")
                        {
                            context.Response.ContentType = v;
                        }
                        else if (k == "Set-Cookie")
                        {
                            context.Response.SetCookie(new HttpCookie(v));
                        }
                        else if (k == "Last-Modified")
                        {
                            context.Response.Headers.Set(k, v);
                        }
                        else if (k == "Content-Encoding")
                        {
                            context.Response.Headers.Set(k, v);
                        }
                    }
                    //寫入回應的本文
                    context.Response.BinaryWrite(retBuffer);
                }
                catch (System.Net.WebException ex)
                {
                    if (ex.Response != null)
                    {
                        context.Response.StatusCode = (int)(((System.Net.HttpWebResponse)ex.Response).StatusCode);
                        context.Response.StatusDescription = ((System.Net.HttpWebResponse)ex.Response).StatusDescription;
                    }
                    else
                    {
                        //通常是伺服器沒開
                        context.Response.StatusCode = 500;
                        context.Response.StatusDescription = "Internal Server error:" + ex.Message;
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = "File not found:" + ex.Message;
                }
            }
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}