using iText.Commons.Bouncycastle.Crypto;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

    //[WebMethod]
    //public string AddSmartContractFileAccess()
    //{

    //    string uploadAgency = HttpContext.Current.Request.Form["uploadAgency"];
    //    string viewerAgency = HttpContext.Current.Request.Form["viewerAgency"];
    //    string documentType = HttpContext.Current.Request.Form["documentType"];
    //    string SmartContractKey = HttpContext.Current.Request.Form["SmartContractKey"];

    //    if (string.IsNullOrEmpty(uploadAgency) || string.IsNullOrEmpty(viewerAgency) || string.IsNullOrEmpty(documentType))
    //    {
    //        return fl.ToJson(new { message = "Missing required fields." });
    //    }

    //    // 2️⃣ Validate Private Key
    //    string validKey = "BSEB#Matrix@SmartKey-7A3C1B8E92FD";
    //    if (SmartContractKey != validKey)
    //    {
    //        return fl.ToJson(new { message = "Invalid private key. Access denied." });
    //    }

    //    try
    //    {

    //        string resp = fl.checkAccessData(uploadAgency, viewerAgency, documentType);
    //        if (!resp.StartsWith("Error"))
    //        {
    //            DataTable existing = fl.Tabulate(resp);
    //            if (existing != null && existing.Rows.Count > 0)
    //            {
    //                return fl.ToJson(new { message = "Record already exists." });
    //            }
    //        }
    //        string res = fl.InsertAgencyAccessFile(uploadAgency, viewerAgency, documentType);
    //        if (!res.StartsWith("Error"))
    //        {
    //            DataTable dtdata = fl.Tabulate("[" + res + "]");
    //            if (dtdata.Rows.Count > 0)
    //            {
    //                if (dtdata.Rows[0]["status"].ToString() == "200")
    //                {
    //                    return fl.ToJson(new { message = "Data Added Successfully" });

    //                }
    //                else
    //                {
    //                    return fl.ToJson(new { message = "Details Not Added Successfully" });

    //                }
    //            }
    //            else
    //            {
    //                return fl.ToJson(new { message = "Details Not Added Successfully" });
    //            }
    //        }
    //        else
    //        {
    //            return fl.ToJson(new { message = "Details Not Added Successfully" });
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        return fl.ToJson(new { message = "Error: " + ex.Message });
    //    }
    //}


    [WebMethod]
    public string AddSmartContractFileAccess()
    {
        try
        {
            HttpContext context = HttpContext.Current;

            string uploadAgency = context.Request.Form["uploadAgency"];
            string SmartContractKey = context.Request.Form["SmartContractKey"];
            string recordsJson = context.Request.Form["records"]; // JSON array: viewerAgency + documentType

            // 1️⃣ Missing fields check
            if (string.IsNullOrEmpty(uploadAgency) ||
                string.IsNullOrEmpty(SmartContractKey) ||
                string.IsNullOrEmpty(recordsJson))
            {
                return fl.ToJson(new
                {
                    
                    message = "Missing required fields. (uploadAgency / SmartContractKey / records)"
                });
            }

            // 2️⃣ Validate private key
            string validKey = "BSEB#Matrix@SmartKey-7A3C1B8E92FD";
            if (SmartContractKey != validKey)
            {
                return fl.ToJson(new
                {
                   
                    message = "Invalid SmartContractKey. Access denied."
                });
            }

            // 3️⃣ Parse JSON array
            JArray records;
            try
            {
                records = JArray.Parse(recordsJson);
            }
            catch
            {
                return fl.ToJson(new
                {
                    
                    message = "Invalid JSON format. Please pass valid JSON array."
                });
            }

            if (records.Count == 0)
            {
                return fl.ToJson(new
                {
                    
                    message = "Records array is empty."
                });
            }

            int successCount = 0, failCount = 0;

            foreach (JObject record in records)
            {
                string viewerAgency = (string)record["viewerAgency"];
                string documentType = (string)record["documentType"];

                // 4️⃣ Validation inside each record
                if (string.IsNullOrEmpty(viewerAgency) || string.IsNullOrEmpty(documentType))
                {
                    failCount++;
                    continue;
                }

                // 5️⃣ Check if record already exists
                string resp = fl.checkAccessData(uploadAgency, viewerAgency, documentType);

                if (!resp.StartsWith("Error"))
                {
                    DataTable existing = fl.Tabulate(resp);
                    if (existing != null && existing.Rows.Count > 0)
                    {
                        return fl.ToJson(new
                        {
                           
                            message = "Record already exists."
                        });
                    }
                }
                else if (resp == "Error : Unable to connect to the remote server")
                {

                    return fl.ToJson(new { message = "Unable to connect to the remote server" });
                }
                else
                {
                    return fl.ToJson(new { message = "Something Went Wrong." });

                }
                // 6️⃣ Insert Record
                string insertResp = fl.InsertAgencyAccessFile(uploadAgency, viewerAgency, documentType);

                // ❗ Specific server connectivity issue
                if (insertResp == "Error : Unable to connect to the remote server")
                {
                    return fl.ToJson(new
                    {
                       
                        message = "Unable to connect to the remote server"
                    });
                }

                // ❗ General error returned by insert function
                if (insertResp.StartsWith("Error"))
                {
                    failCount++;
                    continue;
                }

                // Process insert result
                DataTable dtdata = fl.Tabulate("[" + insertResp + "]");
                if (dtdata.Rows.Count > 0 && dtdata.Rows[0]["status"].ToString() == "200")
                    successCount++;
                else
                    failCount++;
            }

            // ================================
            // 🎉 3️⃣ FINAL SUCCESS RESPONSE
            // ================================

            if (successCount == 0)
            {
                return fl.ToJson(new
                {
                    message = "Data Not Added Successfully."
                });
            }
            return fl.ToJson(new
            {
                
                message = "Data Added Successfully.",
               
            });
        }
        catch (Exception ex)
        {
            // 8️⃣ Unexpected error
            return fl.ToJson(new
            {
  
                message = "Error: " + ex.Message

            });
        }
    }

    //[WebMethod]
    //public string AddSmartContractFileAccess()
    //{
    //    try
    //    {
    //        HttpContext context = HttpContext.Current;

    //        string uploadAgency = context.Request.Form["uploadAgency"];
    //        string SmartContractKey = context.Request.Form["SmartContractKey"];
    //        string recordsJson = context.Request.Form["records"]; // 👈 multiple data as JSON array

    //        if (string.IsNullOrEmpty(uploadAgency) || string.IsNullOrEmpty(SmartContractKey) || string.IsNullOrEmpty(recordsJson))
    //        {
    //            return fl.ToJson(new { message = "Missing required fields.", status = 400 });
    //        }

    //        // ✅ Validate private key
    //        string validKey = "BSEB#Matrix@SmartKey-7A3C1B8E92FD";
    //        if (SmartContractKey != validKey)
    //        {
    //            return fl.ToJson(new { message = "Invalid private key. Access denied." });
    //        }

    //        // ✅ Parse JSON array
    //        JArray records = JArray.Parse(recordsJson);
    //        int successCount = 0, failCount = 0;

    //        foreach (JObject record in records)
    //        {
    //            string viewerAgency = (string)record["viewerAgency"];
    //            string documentType = (string)record["documentType"];

    //            if (string.IsNullOrEmpty(viewerAgency) || string.IsNullOrEmpty(documentType))
    //            {
    //                failCount++;
    //                continue;
    //            }

    //            // 🧠 Check existing
    //            string resp = fl.checkAccessData(uploadAgency, viewerAgency, documentType);
    //            if (!resp.StartsWith("Error"))
    //            {
    //                DataTable existing = fl.Tabulate(resp);
    //                if (existing != null && existing.Rows.Count > 0)
    //                {
    //                    return fl.ToJson(new
    //                    {
    //                        message = "Record already exists.",
    //                        status = 409
    //                    });
    //                }
    //            }

    //            // 🧩 Insert
    //            string res = fl.InsertAgencyAccessFile(uploadAgency, viewerAgency, documentType);
    //            if (!res.StartsWith("Error"))
    //            {
    //                DataTable dtdata = fl.Tabulate("[" + res + "]");
    //                if (dtdata.Rows.Count > 0 && dtdata.Rows[0]["status"].ToString() == "200")
    //                    successCount++;
    //                else
    //                    failCount++;  
    //            }
    //            else
    //            {
    //                failCount++;
    //            }
    //        }

    //        return fl.ToJson(new
    //        {
    //            message = "Data Added Successfully"
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        return fl.ToJson(new { message = "Error: " + ex.Message });
    //    }
    //}



    public string ViewSmartContractFileAccess()
    {
        HttpContext context = HttpContext.Current;
        context.Response.ContentType = "application/json"; // Ensure JSON response

        string viewerAgency = HttpContext.Current.Request.Form["viewerAgency"];
        string SmartContractKey = HttpContext.Current.Request.Form["SmartContractKey"];

        // 1️⃣ Validate required fields
        if (string.IsNullOrEmpty(viewerAgency) || string.IsNullOrEmpty(SmartContractKey))
            return fl.ToJson(new { status = 400, message = "Missing required fields." });

        // 2️⃣ Validate private key
        string validKey = "BSEB#Matrix@SmartKey-7A3C1B8E92FD";
        if (SmartContractKey != validKey)
            return fl.ToJson(new { status = 401, message = "Invalid SmartContractKey. Access denied." });

        try
        {
            // 3️⃣ Fetch data using your function
            string resp = fl.checkAccessDataforAGS(viewerAgency);

            // ===============================
            // 🔥 ERROR HANDLING FOR RESPONSE
            // ===============================

            // ❗ Remote server connectivity issue
            if (resp == "Error : Unable to connect to the remote server")
            {
                return fl.ToJson(new
                {
                    
                    message = "Unable to connect to the remote server."
                });
            }

            // ❗ Other errors
            if (resp.StartsWith("Error"))
            {
                return fl.ToJson(new
                {
                   
                    message = "Error while fetching data: " + resp
                });
            }

            // 4️⃣ Convert to DataTable
            DataTable dt = fl.Tabulate(resp);

            if (dt == null || dt.Rows.Count == 0)
            {
                return fl.ToJson(new
                {
                   
                    message = "No records found for this agency."
                });
            }

            // 5️⃣ SUCCESS RESPONSE
            return fl.ToJson(new
            {
                
                
                data = dt
            });
        }
        catch (Exception ex)
        {
            // 6️⃣ Unexpected error
            return fl.ToJson(new
            {
                
                message = "Error: " + ex.Message
            });
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
                return fl.ToJson(new { message = resp });
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
