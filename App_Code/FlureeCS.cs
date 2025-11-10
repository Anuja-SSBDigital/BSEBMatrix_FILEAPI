using log4net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Linq;
using Org.BouncyCastle.Ocsp;
using Microsoft.ReportingServices.ReportProcessing.ReportObjectModel;
using System.IdentityModel.Protocols.WSTrust;
using System.Configuration;
using MySql.Data.MySqlClient;
using System.IdentityModel.Metadata;
using AjaxControlToolkit.HtmlEditor.ToolbarButtons;
using System.Xml.Linq;
using System.Web.Security;
using System.Security.Policy;
using Org.BouncyCastle.Crypto.Tls;
using System.Activities.Expressions;
using ZXing.PDF417.Internal;
using System.Web.Script.Serialization;
using System.Globalization;
using Org.BouncyCastle.Bcpg.OpenPgp;
using iText.StyledXmlParser.Jsoup.Nodes;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

public class FlureeCS
{

    HttpWebRequest request;
    public readonly ILog log = LogManager.GetLogger
          (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public static string servertranurl = "http://localhost:8091/fdb/ssb/bsebmatrix/transact";
    public static string serverqryurl = "http://localhost:8091/fdb/ssb/bsebmatrix/query";
    public static string servermulqryurl = "http://localhost:8091/fdb/ssb/nhdc/multi-query";



    //public static string servertranurl = "http://15.207.210.199:8093/fdb/ssb/nhdc/transact";
    //public static string serverqryurl = "http://15.207.210.199:8093/fdb/ssb/nhdc/query";
    //public static string servermulqryurl = "http://15.207.210.199:8093/fdb/ssb/nhdc/multi-query";
    public static DataTable TempTable()
    {
        DataTable dt = new DataTable();

        DataRow dr = dt.NewRow();

        DataColumn col = new DataColumn();
        col.ColumnName = "Name";
        col.DataType = typeof(string);
        dt.Columns.Add(col);

        col = new DataColumn();
        col.ColumnName = "Value";
        col.DataType = typeof(string);
        dt.Columns.Add(col);

        col = new DataColumn();
        col.ColumnName = "Key";
        col.DataType = typeof(int);
        dt.Columns.Add(col);

        return dt;
    }

    public static DataTable GetStatusTable()
    {
        DataTable dt = TempTable();

        DataRow dr = dt.NewRow();
        dr["Name"] = "Active";
        dr["Key"] = 0;
        dr["Value"] = "Active";
        dt.Rows.Add(dr);
        dt.AcceptChanges();

        dr = dt.NewRow();
        dr["Name"] = "Assigned";
        dr["Key"] = 1;
        dr["Value"] = "Assigned";
        dt.Rows.Add(dr);
        dt.AcceptChanges();

        dr = dt.NewRow();
        dr["Name"] = "In Process";
        dr["Key"] = 2;
        dr["Value"] = "In Process";
        dt.Rows.Add(dr);
        dt.AcceptChanges();

        dr = dt.NewRow();
        dr["Name"] = "Send To IO";
        dr["Key"] = 3;
        dr["Value"] = "Send To IO";
        dt.Rows.Add(dr);
        dt.AcceptChanges();

        dr = dt.NewRow();
        dr["Name"] = "Received By IO";
        dr["Key"] = 4;
        dr["Value"] = "Received By IO";
        dt.Rows.Add(dr);
        dt.AcceptChanges();

        return dt;
    }
    public static DataTable dtStatus = GetStatusTable();
    #region Other
    private Random _random = new Random();

    public string EncryptString(string str)
    {
        MD5 md5Hash = MD5.Create();
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(str));
        // Create a new Stringbuilder to collect the bytes  
        // and create a string.  
        StringBuilder sBuilder = new StringBuilder();
        // Loop through each byte of the hashed data  
        // and format each one as a hexadecimal string.  
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }

        return sBuilder.ToString();
    }

    public string GenerateRandomNo()
    {
        return _random.Next(0, 999999).ToString("D6");
    }

    public string GetIP()
    {
        string strHostName = "";
        strHostName = System.Net.Dns.GetHostName();

        IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);

        IPAddress[] addr = ipEntry.AddressList;

        return addr[addr.Length - 1].ToString();

    }

    public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);

    public long ConvertToTimestamp(DateTime value)
    {
        TimeSpan elapsedTime = value - Epoch;
        return (long)elapsedTime.TotalMilliseconds;
    }

    public double ConvertDateTimeToTimestamp(DateTime value)
    {
        TimeSpan epoch = (value - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
        //return the total seconds (which is a UNIX timestamp)
        return (double)epoch.TotalMilliseconds;
    }

    public string sendTransaction(string DatatoPost, string url)
    {
        log.Info("Web request called");
        log.Info("query :" + DatatoPost + "");
        request = (HttpWebRequest)WebRequest.Create(url);
        ////request.Timeout = 360000;
        ////request.ReadWriteTimeout = 360000;
        ////request.KeepAlive = true;
        //if (token != "")
        //    request.Headers.Add("Authorization", token);
        request.Method = "POST";
        try
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(DatatoPost);
            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;
            log.Info("GetRequestStream function called");
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);

            dataStream.Close();

            WebResponse response = request.GetResponse();
            log.Info("Request executed");
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            return responseFromServer;

        }
        catch (Exception ex)
        {
            log.Error("************** Error : " + ex.Message, ex);

            return "Error : " + ex.Message;
        }
    }

    public DataTable Tabulate(string json)
    {
        var trgArray = new JArray();
        try
        {
            var jsonLinq = JArray.Parse(json);
            //var jsonLinq = JArray.Parse(json);

            // Find the first array using Linq
            foreach (JObject row in jsonLinq.Children<JObject>())
            {
                var cleanRow = new JObject();

                //var srcArray = jsonLinq.Descendants().Where(d => d is JProperty);
                foreach (JToken column in row.Children<JToken>())
                {
                    if (column.First is JArray)
                    {
                        try
                        {
                            JProperty jt = (JProperty)column;

                            JArray res = JArray.Parse("[" + jarray((JArray)column.First) + "]");
                            foreach (JObject jares in res.First.Children<JObject>())
                                foreach (JToken colpck in jares.Children<JToken>())
                                {
                                    if (colpck.First is JValue)
                                    {
                                        try
                                        {
                                            foreach (JValue colv in colpck)
                                            {
                                                // Only include JValue types
                                                if (colv.Parent is JProperty)
                                                {
                                                    JProperty jtv = (JProperty)colv.Parent;
                                                    //if (col.Value is JValue)
                                                    //{
                                                    cleanRow.Add(jtv.Name, jtv.Value);
                                                    //}
                                                }
                                            }
                                        }
                                        catch (Exception ex) { }
                                    }
                                    //trgArray.Merge(jares);
                                }
                        }
                        catch (Exception ex) { }

                    }
                    else if (column.First is JValue)
                    {
                        try
                        {
                            foreach (JValue col in column)
                            {
                                // Only include JValue types
                                if (col.Parent is JProperty)
                                {
                                    JProperty jt = (JProperty)col.Parent;
                                    //if (col.Value is JValue)
                                    //{
                                    cleanRow.Add(jt.Name, jt.Value);
                                    //}
                                }
                            }
                        }
                        catch (Exception ex) { }
                    }
                    else
                    {
                        try
                        {
                            foreach (JToken t in column.Children<JToken>())
                            {
                                foreach (JProperty v in t)
                                {
                                    string prop = v.Name;
                                    //JProperty p = (JProperty)v.Parent;
                                    if (cleanRow.ContainsKey(v.Name))
                                    {
                                        JProperty colp = (JProperty)column;
                                        prop = colp.Name + "_" + prop;
                                    }
                                    cleanRow.Add(prop, v.Value);
                                }
                            }
                        }
                        catch (Exception ex) { }

                    }

                }
                trgArray.Add(cleanRow);
            }

        }
        catch (Exception ex) { }
        return JsonConvert.DeserializeObject<DataTable>(trgArray.ToString());
    }

    public JArray jarray(JArray ja)
    {
        JObject cleanRows = new JObject();
        JArray jarow = new JArray();
        foreach (JToken column in ja.Children<JToken>())
        {
            JObject cleanRow = new JObject();
            if (column.Children().Count() > 0)
            {
                if (column.First is JArray)
                {
                    cleanRow.Add(jarray((JArray)column.First));
                }
                else if (column.First is JValue)
                {
                    foreach (JValue col in column)
                    {
                        // Only include JValue types
                        if (col.Parent is JProperty)
                        {
                            JProperty jt = (JProperty)col.Parent;
                            //if (col.Value is JValue)
                            //{
                            cleanRow.Add(jt.Name, jt.Value);
                            //}
                        }
                    }
                }
                else
                {
                    try
                    {
                        foreach (JToken t in column.Children<JToken>())
                        {

                            foreach (JToken col in t.Children<JToken>())
                            {
                                // Only include JValue types
                                try
                                {
                                    if (col is JValue)
                                    {
                                        //foreach (JValue col in column)
                                        //{
                                        //    // Only include JValue types
                                        if (col.Parent is JProperty)
                                        {
                                            JProperty jt = (JProperty)col.Parent;
                                            //if (col.Value is JValue)
                                            //{
                                            cleanRow.Add(jt.Name, jt.Value);
                                            //}
                                        }
                                        //}
                                    }
                                    else
                                    {
                                        foreach (JProperty jt in col.Children<JProperty>())
                                        {
                                            if (jt.First is JValue)
                                                cleanRow.Add(jt.Name, jt.Value);
                                            else
                                            {
                                                foreach (JObject jb in jt.Children<JObject>())
                                                {
                                                    foreach (JProperty jt1 in jb.Children<JProperty>())
                                                        cleanRow.Add(jt1.Name, jt1.Value);
                                                }
                                            }

                                        }
                                    }
                                }
                                catch (Exception ex) { }
                            }
                        }
                    }
                    catch (Exception ex) { }
                }
            }
            else
            {
                JProperty jt = (JProperty)column.Parent;
                //if (col.Value is JValue)
                //{
                cleanRow.Add(jt.Name, jt.Value);
            }
            jarow.Add(cleanRow);
        }
        //cleanRows.Add(jarow);
        return jarow;
    }

    public string SHA256CheckSum(Stream fs)
    {
        using (SHA256 SHA256 = SHA256Managed.Create())
        {
            //using (FileStream fileStream = File.OpenRead(filePath))
            byte[] bt = SHA256.ComputeHash(fs);
            string str = "";
            foreach (byte b in bt)
                str += b.ToString("x2");
            return str;
        }
    }

    #endregion

    #region Anuja
    public string ToJson(object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }



    public string InsertAgencyAccessFile(string uploadAgency, string viewerAgency, string documentType)
    {
        try
        {
            string res = "[{\"_id\":\"AgencyFileAccess\","
                       + "\"agencyid\":\"" + Guid.NewGuid() + "\","
                       + "\"uploadAgency\":\"" + uploadAgency + "\","
                       + "\"viewerAgency\":\"" + viewerAgency + "\","
                       + "\"documentType\":\"" + documentType + "\","
                       + "\"IsActive\":\"1\","

                       + "\"createddate\":" + ConvertToTimestamp(DateTime.Now) + ""

                       + "}]";
            string resp = sendTransaction(res, servertranurl);
            return resp;
        }
        catch (Exception ex)
        {
            log.Error("An unexpected error occurred.", ex);
            return "Error: " + ex.Message;
        }
    }


    public string checkAccessDataforAGS(string viewerAgency)
    {
        try
        {
            string res = "{"
            + "\"select\":{\"?user\":["
            + "\"uploadAgency\","
            + "\"viewerAgency\""

            + "]}," // selecting fields
            + "\"where\":["
            + "[\"?user\",\"AgencyFileAccess/viewerAgency\",\"" + viewerAgency + "\"]"
            + "]"
            + "}";
            string resp = sendTransaction(res, serverqryurl);
            return resp;
        }
        catch (Exception ex)
        {
            log.Error("An error occurred.", ex);
            return "Error: " + ex.Message;
        }
    }

    public string CheckFileHashExists(string filehash)
    {
        try
        {
            string res = "{"
                       + "\"select\":{\"?file\":["
                       + "\"filehash\","
                       + "\"filename\","
                       + "\"agencyname\""
                       + "]},"
                       + "\"where\":[[\"?file\",\"filedetails/filehash\",\"" + filehash + "\"]]"
                       + "}";

            string resp = sendTransaction(res, serverqryurl);
            return resp;
        }
        catch (Exception ex)
        {
            log.Error("An error occurred while checking file hash.", ex);
            return "Error: " + ex.Message;
        }
    }

    public string checkAccessData(string uploadAgency, string viewerAgency, string documentType)

    {
        try
        {
            string res = "{"
              + "\"select\":{\"?user\":["
               + "\"uploadAgency\","
                + "\"viewerAgency\""

              + "]},"
              + "\"where\":[[\"?user\",\"AgencyFileAccess/agencyid\",\"?name\"]";
            if (uploadAgency != "")
            {
                res += ",[\"?user\",\"AgencyFileAccess/uploadAgency\",\"" + uploadAgency
                     + "\"]";
            }
            if (uploadAgency != "")
            {
                res += ",[\"?user\",\"AgencyFileAccess/viewerAgency\",\"" + viewerAgency
                     + "\"]";
            }
            if (documentType != "")
                res += ",[\"?user\",\"AgencyFileAccess/documentType\",\"" + documentType
                        + "\"]]}";


            string resp = sendTransaction(res, serverqryurl);
            return resp;
        }
        catch (Exception ex)
        {
            log.Error("An error occurred.", ex);
            return "Error: " + ex.Message;
        }
    }

    public string InsertTofiledetails(string subdoctype, string actualfilename, string filename, string filehash, string agencyname)
    {
        try
        {
            // Create JSON body for Fluree transaction
            string status = "Active";
            string res = "[{\"_id\":\"filedetails\","
                       + "\"fileid\":\"" + Guid.NewGuid() + "\","
                       + "\"subdoctype\":\"" + subdoctype + "\","
                       + "\"actualfilename\":\"" + actualfilename + "\","
                       + "\"filename\":\"" + filename + "\","
                       + "\"filehash\":\"" + filehash + "\","
                       + "\"agencyname\":\"" + agencyname + "\","
                       + "\"status\":\"" + status + "\","
                       + "\"createddate\":" + ConvertToTimestamp(DateTime.Now)
                       + "}]";

            // Send to Fluree transaction URL
            string resp = sendTransaction(res, servertranurl);
            return resp;
        }
        catch (Exception ex)
        {
            log.Error("An unexpected error occurred.", ex);
            return "Error: " + ex.Message;
        }
    }

    #endregion

}

