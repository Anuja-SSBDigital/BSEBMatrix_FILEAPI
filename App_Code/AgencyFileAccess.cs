using iText.Commons.Bouncycastle.Crypto;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Protocols.WSTrust;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Services;
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
    public string ViewSmartContractFileAccess()
    {
        HttpContext context = HttpContext.Current;
        context.Response.ContentType = "application/json"; // Force JSON output

        string viewerAgency = HttpContext.Current.Request.Form["viewerAgency"];
        string SmartContractKey = HttpContext.Current.Request.Form["SmartContractKey"];

        // Validate required fields
        if (string.IsNullOrEmpty(viewerAgency) || string.IsNullOrEmpty(SmartContractKey))
            return fl.ToJson(new { message = "Missing required fields." });

        // Validate private key
        string validKey = "BSEB#Matrix@SmartKey-7A3C1B8E92FD";
        if (SmartContractKey != validKey)
            return fl.ToJson(new { message = "Invalid private key. Access denied." });

        try
        {
            // use your same function (passing viewerAgency as needed)
            string resp = fl.checkAccessDataforAGS(viewerAgency);

            if (resp.StartsWith("Error"))
                return fl.ToJson(new { message = "Error while fetching data: " + resp });

            DataTable dt = fl.Tabulate(resp);
            if (dt == null || dt.Rows.Count == 0)
                return fl.ToJson(new { message = "No records found for this agency." });

            // Return result
            return fl.ToJson(new
            {
                data = dt
            });
        }
        catch (Exception ex)
        {
            return fl.ToJson(new { message = "Error: " + ex.Message });
        }
    }
    //[WebMethod]
    //public string ViewSmartContractFileAccess()
    //{
    //    HttpContext context = HttpContext.Current;
    //    context.Response.ContentType = "application/json"; // Force JSON output

    //    string viewerAgency = HttpContext.Current.Request.Form["viewerAgency"];
    //    string SmartContractKey = HttpContext.Current.Request.Form["SmartContractKey"];

    //    // Validate required fields
    //    if (string.IsNullOrEmpty(viewerAgency) || string.IsNullOrEmpty(SmartContractKey))
    //        return fl.ToJson(new { message = "Missing required fields." });

    //    // Validate private key
    //    string validKey = "BSEB#Matrix@SmartKey-7A3C1B8E92FD";
    //    if (SmartContractKey != validKey)
    //        return fl.ToJson(new { message = "Invalid private key. Access denied." });

    //    try
    //    {
    //        // use your same function (passing viewerAgency as needed)
    //        string resp = fl.checkAccessDataforAGS(viewerAgency);

    //        if (resp.StartsWith("Error"))
    //            return fl.ToJson(new { message = "Error while fetching data: " + resp });

    //        DataTable dt = fl.Tabulate(resp);
    //        if (dt == null || dt.Rows.Count == 0)
    //            return fl.ToJson(new { message = "No records found for this agency." });

    //        // Return result
    //        return fl.ToJson(new
    //        {
    //            message = "Data fetched successfully.",
    //            viewerAgency = viewerAgency,
    //            recordCount = dt.Rows.Count,
    //            data = dt
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        return fl.ToJson(new { message = "Error: " + ex.Message });
    //    }
    //}

    [WebMethod]
    public string InsertFileDetails()
    {
        HttpContext context = HttpContext.Current;

        string SmartContractKey = context.Request.Form["SmartContractKey"];
        string subdoctype = context.Request.Form["subdoctype"];
        string actualfilename = context.Request.Form["actualfilename"];
        string filename = context.Request.Form["filename"];
        string filehash = context.Request.Form["filehash"];
        string agencyname = context.Request.Form["agencyname"];

        // Validate required fields
        if (string.IsNullOrEmpty(SmartContractKey) || string.IsNullOrEmpty(subdoctype) ||
     string.IsNullOrEmpty(actualfilename) ||
     string.IsNullOrEmpty(filename) ||
     string.IsNullOrEmpty(filehash) ||
     string.IsNullOrEmpty(agencyname))
        {
            return fl.ToJson(new { message = "Missing required fields. Please provide all parameters" });
        }
        // Validate private key
        string validKey = "BSEB#Matrix@SmartKey-7A3C1B8E92FD";
        if (SmartContractKey != validKey)
        {
            return fl.ToJson(new { message = "Invalid SmartContractKey. Access denied." });
        }

        try
        {
            // Prepare file record
            //    var fileData = new List<object>
            //{
            //    new Dictionary<string, object>
            //    {
            //        { "_id", "filedetails" },
            //        { "filedetails/subdoctype", subdoctype },
            //        { "filedetails/actualfilename", actualfilename },
            //        { "filedetails/filename", filename },
            //        { "filedetails/filehash", filehash },
            //        { "filedetails/agencyname", agencyname },
            //        { "filedetails/status", status },
            //        { "filedetails/createddate", DateTime.UtcNow.ToString("o") }
            //    }
            //};

            //    // Convert to JSON for Fluree insertion
            //    string flureeData = Newtonsoft.Json.JsonConvert.SerializeObject(fileData);

            // Insert into FlureeDB (custom method)

            //string checkResp = fl.CheckFileHashExists(filehash);
            //if (!checkResp.StartsWith("Error"))
            //{
            //    DataTable dtExisting = fl.Tabulate(checkResp);
            //    if (dtExisting != null && dtExisting.Rows.Count > 0)
            //    {
            //        return fl.ToJson(new { message = "Duplicate filehash found. Record already exists." });
            //    }
            //}

            string resp = fl.InsertTofiledetails(subdoctype, actualfilename, filename, filehash, agencyname);

            if (!resp.StartsWith("Error"))
            {
                DataTable dtdata = fl.Tabulate("[" + resp + "]");
                if (dtdata.Rows.Count > 0)
                {
                    if (dtdata.Rows[0]["status"].ToString() == "200")
                    {
                        return fl.ToJson(new { message = "File details inserted successfully" });

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

    [WebMethod]
    public string GenerateFileHash()
    {
        HttpContext context = HttpContext.Current;

        string SmartContractKey = context.Request.Form["SmartContractKey"];
        HttpPostedFile uploadedFile = context.Request.Files["file"];

        // Validate fields
        if (string.IsNullOrEmpty(SmartContractKey) || uploadedFile == null)
        {
            return fl.ToJson(new { message = "Missing SmartContractKey or file." });
        }

        // Validate private key
        string validKey = "BSEB#Matrix@SmartKey-7A3C1B8E92FD";
        if (SmartContractKey != validKey)
        {
            return fl.ToJson(new { message = "Invalid SmartContractKey. Access denied." });
        }

        try
        {
            // ✅ Compute SHA256 hash
            string hashValue;
            using (Stream fileStream = uploadedFile.InputStream)
            {
                hashValue = fl.SHA256CheckSum(fileStream);
            }

            // ✅ Return response (no DB insert)
            return fl.ToJson(new
            {
                message = "File hash generated successfully.",
                filename = uploadedFile.FileName,
                sha256 = hashValue
            });
        }
        catch (Exception ex)
        {
            return fl.ToJson(new { message = "Error: " + ex.Message });
        }
    }


    //[WebMethod]
    //public string UpdateSmartContractFileAccess()
    //{
    //    string uploadAgency = HttpContext.Current.Request.Form["uploadAgency"];
    //    string viewerAgency = HttpContext.Current.Request.Form["viewerAgency"];
    //    string documentType = HttpContext.Current.Request.Form["documentType"];
    //    string newDocumentType = HttpContext.Current.Request.Form["newDocumentType"]; // new value
    //    string SmartContractKey = HttpContext.Current.Request.Form["SmartContractKey"];

    //    if (string.IsNullOrEmpty(uploadAgency) || string.IsNullOrEmpty(viewerAgency) || string.IsNullOrEmpty(documentType) || string.IsNullOrEmpty(newDocumentType))
    //        return fl.ToJson(new { message = "Missing required fields." });

    //    string validKey = "BSEB#Matrix@SmartKey-7A3C1B8E92FD";
    //    if (SmartContractKey != validKey)
    //        return fl.ToJson(new { message = "Invalid private key. Access denied." });

    //    try
    //    {
    //        // Check if record exists
    //        string resp = fl.checkAccessData(uploadAgency, viewerAgency, documentType);
    //        DataTable existing = fl.Tabulate(resp);
    //        if (existing == null || existing.Rows.Count == 0)
    //            return fl.ToJson(new { message = "Record not found to update." });

    //        // Update record
    //        string updateResp = fl.UpdateAgencyAccessFile(uploadAgency, viewerAgency, documentType, newDocumentType);
    //        if (!updateResp.StartsWith("Error"))
    //        {
    //            return fl.ToJson(new { message = "Data Updated Successfully" });
    //        }
    //        else
    //        {
    //            return fl.ToJson(new { message = "Update failed." });
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        return fl.ToJson(new { message = "Error: " + ex.Message });
    //    }
    //}

}
