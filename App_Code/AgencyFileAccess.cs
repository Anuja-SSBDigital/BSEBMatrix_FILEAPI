using iText.Commons.Bouncycastle.Crypto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Services;

/// <summary>
/// Summary description for AgencyFileAccess
/// </summary>
[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
// [System.Web.Script.Services.ScriptService]
public class AgencyFileAccess : System.Web.Services.WebService
{
    FlureeCS fl = new FlureeCS();

    [WebMethod]
    public string AddSmartContractFileAccess()
    {

        string uploadAgency = HttpContext.Current.Request.Form["uploadAgency"];
        string viewerAgency = HttpContext.Current.Request.Form["viewerAgency"];
        string documentType = HttpContext.Current.Request.Form["documentType"];
        string SmartContractKey = HttpContext.Current.Request.Form["SmartContractKey"];

        if (string.IsNullOrEmpty(uploadAgency) || string.IsNullOrEmpty(viewerAgency) || string.IsNullOrEmpty(documentType))
        {
            return fl.ToJson(new { message = "Missing required fields." });
        }

        // 2️⃣ Validate Private Key
        string validKey = "BSEB#Matrix@SmartKey-7A3C1B8E92FD";
        if (SmartContractKey != validKey)
        {
            return fl.ToJson(new { message = "Invalid private key. Access denied." });
        }

        try
        {

            string resp = fl.checkAccessData(uploadAgency, viewerAgency, documentType);
            if (!resp.StartsWith("Error"))
            {
                DataTable existing = fl.Tabulate(resp);
                if (existing != null && existing.Rows.Count > 0)
                {
                    return fl.ToJson(new { message = "Record already exists." });
                }
            }
            string res = fl.InsertAgencyAccessFile(uploadAgency, viewerAgency, documentType);
            if (!res.StartsWith("Error"))
            {
                DataTable dtdata = fl.Tabulate("[" + res + "]");
                if (dtdata.Rows.Count > 0)
                {
                    if (dtdata.Rows[0]["status"].ToString() == "200")
                    {
                        return fl.ToJson(new { message = "Data Added Successfully" });

                    }
                    else
                    {
                        return fl.ToJson(new { message = "Details Not Added Successfully" });

                    }
                }
                else
                {
                    return fl.ToJson(new { message = "Details Not Added Successfully" });
                }
            }
            else
            {
                return fl.ToJson(new { message = "Details Not Added Successfully" });
            }
        }
        catch (Exception ex)
        {
            return fl.ToJson(new { message = "Error: " + ex.Message });
        }
    }

    }
