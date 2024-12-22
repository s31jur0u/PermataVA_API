using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace VA_API.Controllers.V1._0;

[Route("v1.0/transfer-va/[action]")]
[JwtAuthorize]
public class TransferVaController : ControllerBase
{
    private readonly IJwtTokenGeneratorService _jwtTokenGeneratorService;
    private readonly IConfiguration _config;
    private static ISqlConnectionFactory _sqlConnectionFactory;

    private JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly Serilog.Core.Logger _logger;

    public TransferVaController(IJwtTokenGeneratorService jwtTokenGeneratorService, IConfiguration config,
        ISqlConnectionFactory sqlConnectionFactory)
    {
        _logger = new LoggerConfiguration().WriteTo.File(config["LOG:PATH"], rollingInterval: RollingInterval.Day)
            .CreateLogger();
        _jwtTokenGeneratorService = jwtTokenGeneratorService;
        _config = config;
        _sqlConnectionFactory = sqlConnectionFactory;
    }


    [HttpPost("")]
    public IActionResult Inquiry([FromBody] VaInquiryRequest request)
    {
        bool ok = false;
        decimal billtotalAmount = 0;
        string vaName = string.Empty;
        int maxId = 0;

        HttpHeader header = new();

        ApiBaseResponse failedResponse = new();
        failedResponse.responseCode = "4002401";
        failedResponse.responseMessage = "Failed";
        string body = string.Empty;
        VaInquiryResponse response = new();
        VaData vadata = new();
        var settings = new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
        };
        header = RequestHeaderHelper.GetHeader(Request);
        body = JsonConvert.SerializeObject(request, settings);
        string headerstring = JsonConvert.SerializeObject(header, settings);

        _logger.Information(headerstring);
        _logger.Information(body);
        
        
        if(CheckExternalId(header.xExternalId, "inquiry",out failedResponse))
        {
            try
            {
                ok = true;
                bool need_verify = false;
                bool.TryParse(_config["VERIFY_SIGNATURE:INQUIRY"], out need_verify);
                if (need_verify)
                    ok = VerifySignature(Request, body, "inquiry");
                if (ok)
                {
                    if (ModelState.IsValid)
                    {
                        header = RequestHeaderHelper.GetHeader(Request);
                        body = JsonConvert.SerializeObject(request);
                        JsonConvert.PopulateObject(body, vadata);

                        if (_config["EXPIRED_VA"] == request.virtualAccountNo)
                        {
                            failedResponse.responseCode = "4042419";
                            failedResponse.responseMessage = "Invalid Bill/Virtual Account";
                            throw new Exception("Invalid Bill/Virtual Account");

                        }
                        else
                        {

                            if (CheckVAExists(request.virtualAccountNo))
                            {
                                SqlCommand cmd = new();
                                SqlDataReader reader;
                                if (CheckGotBill(request.virtualAccountNo))
                                {
                                    using (SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection())
                                    {
                                        cmd = new SqlCommand(
                                            "EXEC USPPA_GET_BILLVA @COMPANY_CODE,@CUSTOMER_NUMBER,@TRACE_NO ",
                                            sqlconn);
                                        cmd.Parameters.AddWithValue("@COMPANY_CODE", request.partnerServiceId.Trim());
                                        cmd.Parameters.AddWithValue("@CUSTOMER_NUMBER", request.virtualAccountNo);
                                        cmd.Parameters.AddWithValue("@TRACE_NO", request.inquiryRequestId);
                                        cmd.ExecuteNonQuery();

                                        cmd = new SqlCommand(
                                            "SELECT SUM(TOTALAMOUNT) AS TOTALAMOUNT, MAX(CUSTOMERNAME) AS CUSTOMERNAME, MAX(PA_PERMATA_LOG_ID)  AS MAX_ID  FROM PA_PERMATA_LOG WHERE VACD = @VA_CD AND STATUS=0",
                                            sqlconn);
                                        cmd.Parameters.AddWithValue("@VA_CD", request.virtualAccountNo);
                                        reader = cmd.ExecuteReader();
                                        if (reader.HasRows)
                                        {
                                            while (reader.Read())
                                            {
                                                billtotalAmount = billtotalAmount + reader.GetDecimal(0);
                                                vaName = reader.GetString(1);
                                                maxId = reader.GetInt32(2);
                                            }
                                        }
                                        else
                                        {
                                            cmd = new SqlCommand(
                                                "SELECT top 1 VACD  FROM PA_PERMATA_LOG WHERE VACD = @VA_CD",
                                                sqlconn);
                                            cmd.Parameters.AddWithValue("@VA_CD", request.virtualAccountNo);

                                            SqlDataReader reader2 = cmd.ExecuteReader();
                                            bool gotRows2 = false;
                                            gotRows2 = reader2.HasRows;

                                            reader2.Close();

                                            if (gotRows2)
                                            {
                                                failedResponse.responseCode = "4042414";
                                                failedResponse.responseMessage = "Bill Has Been Paid";
                                            }

                                            throw new Exception("Bill Has Been Paid");

                                        }

                                    }
                                }
                                else
                                {
                                    failedResponse.responseCode = "4042414";
                                    failedResponse.responseMessage = "Bill Has Been Paid";
                                    throw new Exception("Bill Has Been Paid");
                                }
                            }
                            else
                            {
                                failedResponse.responseCode = "4042412";
                                failedResponse.responseMessage = "Bill Not Found";


                                throw new Exception("No Record Found");
                            }
                        }

                        VaTotalAmount totalAmount = new();
                        AdditionalInfo additionalInfo = new();
                        totalAmount.currency = "IDR";
                        totalAmount.value = billtotalAmount.ToString("#0.00");
                        additionalInfo.transactionId = maxId.ToString();

                        vadata.totalAmount = totalAmount;
                        vadata.additionalInfo = additionalInfo;
                        response.responseCode = "2002400";
                        response.responseMessage = "Success";
                        vadata.inquiryStatus = "00";
                        vadata.virtualAccountName = vaName;

                        response.virtualAccountData = vadata;

                    }
                    else
                    {
                        ok = false;
                        failedResponse = GetModelInvalidError(ModelState, "inquiry");
                    }
                }
                else
                {

                    ok = false;
                    failedResponse.responseCode = "4012400";
                    failedResponse.responseMessage = "Unauthorized Signature";
                }
            }
            catch (Exception ex)
            {
                ok = false;

            }
        }
        return ok ? Ok(response) : BadRequest(failedResponse);
    }

    private ApiBaseResponse GetModelInvalidError(ModelStateDictionary ModelState, string apiType)
    {
        var errors = new Dictionary<string, List<string>>();
ApiBaseResponse failedResponse = new();
        foreach (var state in ModelState)
        {
            var key = state.Key;
            var stateErrors = state.Value.Errors;

            foreach (var error in stateErrors)
            {
                if (!errors.ContainsKey(key))
                {
                    errors[key] = new List<string>();
                }

                errors[key].Add(error.ErrorMessage);
            }
        }

        var requiredErrors = errors
            .Where(e => e.Value.Any(msg => msg.Contains("required")))
            .Select(s=> s.Key)
            .ToList();

        var formatErrors = errors
            .Where(e => e.Value.Any(msg => msg.Contains("Invalid")))
            .Select(s=> s.Key)
            .ToList();

        string errorResponseCode = "400XX02";
        string errorResponseMessage = "Failed";
        if (requiredErrors.Any())
        {
            errorResponseCode = "400XX02";
            errorResponseMessage = string.Format("Missing Mandatory Field {{{0}}}", string.Join(", ", requiredErrors));
        }
        else
        {
            errorResponseCode = "400XX01";
            errorResponseMessage = string.Format("Invalid Field Format {{{0}}}", string.Join(", ", formatErrors));

        }

        string apierrorcode = string.Empty;    
        apierrorcode = apiType switch
        {
            "inquiry" => "24",
            "payment" => "25",
            _ => apierrorcode
        };
        
        
        
        failedResponse.responseCode = errorResponseCode.Replace("XX", apierrorcode);
        failedResponse.responseMessage = errorResponseMessage;


        return failedResponse;
    }

    [HttpPost("")]
    public IActionResult Payment([FromBody] VaPaymentRequest request)
    {
        bool ok = false;
        HttpHeader header = new();
        string body = string.Empty;
        VaPaymentResponse response = new();
        VaPaymentBase vAPaymentBase = new();
        ApiBaseResponse failedResponse = new();
        failedResponse.responseCode = "4002501";
        failedResponse.responseMessage = "Failed";
        header = RequestHeaderHelper.GetHeader(base.Request);
        body = JsonConvert.SerializeObject(request, jsonSerializerSettings);
        _logger.Information("payment");

        if(CheckExternalId(header.xExternalId,"payment", out failedResponse))

        {
            try
            {
                ok = true;
                bool need_verify = false;
                bool.TryParse(_config["VERIFY_SIGNATURE:PAYMENT"], out need_verify);
                if (need_verify)
                    ok = VerifySignature(Request, body, "payment");
                if (ok)
                {
                    if (ModelState.IsValid)
                    {

                        header = RequestHeaderHelper.GetHeader(Request);


                        JsonConvert.PopulateObject(JsonConvert.SerializeObject(request), vAPaymentBase);


                        decimal totalAmount = 0;
                        decimal paidAmount = 0;
                        Decimal.TryParse(vAPaymentBase.totalAmount.value, out totalAmount);
                        Decimal.TryParse(vAPaymentBase.paidAmount.value, out paidAmount);


                        if (CheckVAExists(request.virtualAccountNo))
                        {

                            SqlCommand cmd = new();
                            using SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection();
                            cmd = new SqlCommand(
                                "SELECT SUM(TOTALAMOUNT) AS TOTALAMOUNT, MAX(CUSTOMERNAME) AS CUSTOMERNAME, MAX(PA_PERMATA_LOG_ID)  AS MAX_ID  FROM PA_PERMATA_LOG WHERE VACD = @VA_CD AND STATUS=0",
                                sqlconn);
                            cmd.Parameters.AddWithValue("@VA_CD", request.virtualAccountNo);
                            SqlDataReader reader = cmd.ExecuteReader();
                            string customername = string.Empty;
                            bool gotRows = true;
                            decimal billAmount = 0;
                            gotRows = reader.HasRows;
                            while (reader.Read())
                            {
                                billAmount = reader.GetDecimal(0);
                                customername = reader.GetString(1);

                            }

                            reader.Close();
                            if (gotRows)
                            {

                                if (paidAmount != billAmount)
                                {
                                    failedResponse.responseCode = "4042513";
                                    failedResponse.responseMessage = "Invalid Amount";
                                    ok = false;

                                }
                                else
                                {
                                    try
                                    {

                                        cmd = new SqlCommand(
                                            "EXEC usppa_pay_billva @COMPANY_CODE,@CUSTOMER_NUMBER,@CUSTOMER_NAME,@PAID_AMOUNT,@TOTAL_AMOUNT ",
                                            sqlconn);
                                        cmd.Parameters.AddWithValue("@COMPANY_CODE", request.partnerServiceId.Trim());
                                        cmd.Parameters.AddWithValue("@CUSTOMER_NUMBER", request.virtualAccountNo);
                                        cmd.Parameters.AddWithValue("@CUSTOMER_NAME", customername);
                                        cmd.Parameters.AddWithValue("@PAID_AMOUNT", request.paidAmount.value);
                                        cmd.Parameters.AddWithValue("@TOTAL_AMOUNT", request.totalAmount.value);
                                        SqlDataReader sp_reader = cmd.ExecuteReader();

                                        string errorcode = string.Empty;
                                        while (sp_reader.Read())
                                        {
                                            errorcode = sp_reader.GetString(0);
                                        }

                                        if (errorcode == "00")
                                        {
                                            response.responseCode = "2002500";
                                            response.responseMessage = "Success";

                                            response.virtualAccountData = vAPaymentBase;
                                        }

                                        ok = true;
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                        ok = false;
                                    }
                                }

                            }
                            else
                            {

                                ok = false;
                            }
                        }
                        else
                        {
                            ok = false;
                            failedResponse.responseCode = "4042512";
                            failedResponse.responseMessage = "Bill Not Found";
                        }

                    }
                    else
                    {
                        ok = false;
                        failedResponse = GetModelInvalidError(ModelState,"payment");

                    }
                }
                else
                {
                    ok = false;
                    failedResponse.responseCode = "4012500";
                    failedResponse.responseMessage = "Unauthorized Signature";
                }

            }
            catch (Exception ex)
            {
                _logger.Information(ex.Message);
                _logger.Information(ex.InnerException?.Message ?? ex.Message);

                ok = false;
            }
            
        }

        return ok ? Ok(response) : Ok(failedResponse);
    }

    [HttpPost("")]
    [AllowAnonymous]
    public IActionResult GetPaymentSignature(VaPaymentRequest request)
    {
        HttpHeader header = new();
        header = RequestHeaderHelper.GetHeader(Request);
        string body = JsonConvert.SerializeObject(request);
        string signature = CreateSignature(Request, body);
        return Ok(signature);

    }

    [HttpPost("")]
    public IActionResult GetInquirySignature(VaInquiryRequest request)
    {
        HttpHeader header = new();
        header = RequestHeaderHelper.GetHeader(Request);
        string body = JsonConvert.SerializeObject(request);
        string signature = CreateSignature(Request, body);
        return Ok(signature);

    }

    [HttpPost("")]
    [AllowAnonymous]
    public IActionResult VerifyInquirySignature(VaInquiryRequest request)
    {

        string body = JsonConvert.SerializeObject(request);
        bool verified = VerifySignature(Request, body, "inquiry");
        return Ok(new { Ok = verified });

    }

    private string CreateSignature(HttpRequest request, string requestBody)
    {
        string signature = "";
        string clientId = UserHelper.GetClaimValue(User, "CLIENT_ID");
        HttpHeader headers = RequestHeaderHelper.GetHeader(request);
        string httpMethod = request.Method;
        string endpoint = request.Path;
        var tokenHeaders = request.Headers["Authorization"].FirstOrDefault();
        string token = tokenHeaders.Split(' ').LastOrDefault();
        string hexbody = GetHexSha256(requestBody);
        string tosing = string.Concat(httpMethod, ":", endpoint, ":", token, ":", hexbody, ":", headers.xTimestamp);
        signature = SignatureVerifier.CreateHmacSha512(tosing, clientId);
        return signature;
    }

    private bool VerifySignature(HttpRequest request, string requestBody, string verifytype)
    {
        bool ok = false;
        string clientId = UserHelper.GetClaimValue(User, "CLIENT_ID");
        HttpHeader headers = RequestHeaderHelper.GetHeader(request);
        string httpMethod = request.Method;

        // //string endpoint = "https://vah2h.southcity.co.id:4580" + (verifytype.ToLower() == "inquiry"
        string endpoint = (verifytype.ToLower() == "inquiry"
            ? "/v1.0/transfer-va/inquiry"
            : "/v1.0/transfer-va/payment");
        var tokenHeaders = request.Headers["Authorization"].FirstOrDefault();
        string token = tokenHeaders.Split(' ').LastOrDefault();
        string hexbody = GetHexSha256(requestBody);
        string tosing = string.Concat(httpMethod, ":", endpoint, ":", token, ":", hexbody, ":", headers.xTimestamp);
        ok = SignatureVerifier.VerifyHmacSha512(tosing, headers.xSignature, _config["CLIENT_SECRET"]);
        return ok;
    }

    private string GetHexSha256(string input)
    {
        input = MinifyString(input);
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // ComputeHash - returns byte array
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString().ToLower();
        }
    }

    private string MinifyString(string input)
    {
        // Remove all types of unnecessary whitespace
        // string minified = Regex.Replace(input, @"\s+", " ");
        //
        // // Trim leading and trailing whitespace
        // return minified.Trim();

        JToken parsedJson = JToken.Parse(input);
        return parsedJson.ToString(Formatting.None); // Minified JSON
    }

    private bool CheckVAExists(string vaNo)
    {
        bool ok = false;

        using (SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection())
        {



            SqlCommand cmd =
                new SqlCommand(
                    "SELECT top 1 * FROM  vwpa_konsumen_va WHERE VA_NO= @VA_CD ",
                    sqlconn);

            cmd.Parameters.AddWithValue("@VA_CD", vaNo);

            SqlDataReader reader = cmd.ExecuteReader();
            ok = reader.HasRows;
            reader.Close();
        }

        return ok;
    }
    
    private bool CheckGotBill(string vaNo)
    {
        bool ok = false;

        try
        {
            using (SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection())
            {
                SqlCommand cmd =
                    new SqlCommand(
                        "SELECT top 1 name FROM  VW_PA_UPLOAD2 WHERE VA_CD= @VA_CD",
                        sqlconn);

                cmd.Parameters.AddWithValue("@VA_CD", vaNo);

                SqlDataReader reader = cmd.ExecuteReader();
                ok = reader.HasRows;
                reader.Close();
            }
        }
        catch (Exception e)
        {
           
        }

        return ok;
    }

    private bool CheckExternalId(string externalid, string actiontype, out ApiBaseResponse conflict_resp)
    {
        bool isvalid = false;

        using SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection();
        SqlCommand sqlCommand = new SqlCommand("Select * From ExternalIdLogs Where ExternalId = @ExternalId", sqlconn);
        sqlconn.Open();
        sqlCommand.Parameters.AddWithValue("@ExternalId", externalid);

        SqlDataReader reader = sqlCommand.ExecuteReader();
        if (!reader.HasRows)
        {
            sqlCommand =
                new SqlCommand("insert into ExternalIdLogs (ExternalId,IsDuplicate) values (@ExternalId,false)",
                    sqlconn);
            sqlCommand.Parameters.AddWithValue("@ExternalId", externalid);

            sqlCommand.ExecuteNonQuery();
            isvalid = true;
        }

        sqlconn.Close();



        conflict_resp = new()
        {
            responseCode = "409XX00",
            responseMessage = "Conflict"
        };
        
        string actioncode  = actiontype switch
        {
            "inquiry" => "24",
            "payment" => "25",
            _ => "00"
        };

        conflict_resp.responseCode.Replace("XX", actioncode);
        return isvalid;

    }
}