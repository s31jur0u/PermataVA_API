using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace VA_API.Controllers.V1._0;

[Route("v1.0/transfer-va/[action]")]
[JwtAuthorize]
public class TransferVaController : ControllerBase
{
    private readonly IJwtTokenGeneratorService _jwtTokenGeneratorService;
    private readonly IConfiguration _config;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    public TransferVaController(IJwtTokenGeneratorService jwtTokenGeneratorService, IConfiguration config, ISqlConnectionFactory sqlConnectionFactory)
    {
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

        try
        {
            ok = true;
            // ok = VerifySignature(Request, body);
            if (ok)
            {
                if (ModelState.IsValid)
                {
                    header = RequestHeaderHelper.GetHeader(Request);
                    body = JsonConvert.SerializeObject(request);
                    JsonConvert.PopulateObject(body, vadata);



                    using (SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection())
                    {
                        SqlCommand cmd = new SqlCommand("SELECT top 1 name FROM  VW_PA_UPLOAD2 WHERE VA_CD= @VA_CD AND BIN_CD = @BIN_CD", sqlconn);

                        cmd.Parameters.AddWithValue("@VA_CD", request.virtualAccountNo);
                        cmd.Parameters.AddWithValue("@BIN_CD", request.partnerServiceId);

                        SqlDataReader reader = cmd.ExecuteReader();
                        bool gotRows = reader.HasRows;
                        reader.Close();
                        if (gotRows)
                        {
                            cmd = new SqlCommand("EXEC USPPA_GET_BILLVA @COMPANY_CODE,@CUSTOMER_NUMBER,@TRACE_NO ", sqlconn);
                            cmd.Parameters.AddWithValue("@COMPANY_CODE", request.partnerServiceId);
                            cmd.Parameters.AddWithValue("@CUSTOMER_NUMBER", request.virtualAccountNo);
                            cmd.Parameters.AddWithValue("@TRACE_NO", string.Empty);
                            cmd.ExecuteNonQuery();

                            cmd = new SqlCommand("SELECT SUM(TOTALAMOUNT) AS TOTALAMOUNT, MAX(CUSTOMERNAME) AS CUSTOMERNAME, MAX(PA_PERMATA_LOG_ID)  AS MAX_ID  FROM PA_PERMATA_LOG WHERE VACD = @VA_CD AND STATUS=0", sqlconn);
                            cmd.Parameters.AddWithValue("@VA_CD", request.virtualAccountNo);
                            reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                billtotalAmount = billtotalAmount + reader.GetDecimal(0);
                                vaName = reader.GetString(1);
                                maxId = reader.GetInt32(2);
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
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    failedResponse.responseMessage = string.Join(", ", errors);
                }
            }
            else
            {

                ok = false;
                failedResponse.responseCode = "4012400";
                failedResponse.responseMessage = "Unauhtorized Signature";
            }
        }
        catch (Exception ex)
        {
            ok = false;

        }
        return ok ? Ok(response) : BadRequest(failedResponse);
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
        failedResponse.responseCode = "4002401";
        failedResponse.responseMessage = "Failed";
        try
        {
            // ok = VerifySignature(Request, body);
            ok = true;
            if (ok)
            {
                if (ModelState.IsValid)
                {

                    header = RequestHeaderHelper.GetHeader(Request);

                    body = JsonConvert.SerializeObject(request);

                    JsonConvert.PopulateObject(JsonConvert.SerializeObject(request), vAPaymentBase);

                    if(Decimal.TryParse(vAPaymentBase.totalAmount.value, out decimal totalAmount) || Decimal.TryParse(vAPaymentBase.paidAmount.value, out decimal paidAmount))
                    {
                        failedResponse.responseCode ="4042413";
                        failedResponse.responseMessage = "Invalid Amount";
                    }
                    SqlCommand cmd = new();
                    // using SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection();
                    //  cmd = new SqlCommand("SELECT SUM(TOTALAMOUNT) AS TOTALAMOUNT, MAX(CUSTOMERNAME) AS CUSTOMERNAME, MAX(PA_PERMATA_LOG_ID)  AS MAX_ID  FROM PA_PERMATA_LOG WHERE VACD = @VA_CD AND STATUS=0", sqlconn);
                    // cmd.Parameters.AddWithValue("@VA_CD", request.virtualAccountNo);
                    // SqlDataReader reader = cmd.ExecuteReader();

                    bool gotRows = true;
                    // got_rows = reader.HasRows;
                    // reader.Close();
                    if (gotRows)
                    {

                        // cmd = new SqlCommand("EXEC usppa_pay_billva @COMPANY_CODE,@CUSTOMER_NUMBER,@CUSTOMER_NAME,@PAID_AMOUNT,@TOTAL_AMOUNT ", sqlconn);
                        // cmd.Parameters.AddWithValue("@COMPANY_CODE", request.partnerServiceId);
                        // cmd.Parameters.AddWithValue("@CUSTOMER_NUMBER", request.virtualAccountNo);
                        // cmd.Parameters.AddWithValue("@CUSTOMER_NAME", request.virtualAccountName);
                        // cmd.Parameters.AddWithValue("@PAID_AMOUNT", request.paidAmount.value);
                        // cmd.Parameters.AddWithValue("@TOTAL_AMOUNT", request.totalAmount.value);
                        // cmd.ExecuteReader();

                        response.responseCode = "2002400";
                        response.responseMessage = "Success";

                        response.virtualAccountData = vAPaymentBase;

                        return Ok(response);

                    }
                    else
                    { ok = false; }


                }
                else
                {
                    ok = false;
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    failedResponse.responseMessage = string.Join(", ", errors);
                }
            }
            else
            {
                ok = false;
                failedResponse.responseCode = "4012400";
                failedResponse.responseMessage = "Unauhtorized Signature";
            }

        }
        catch (System.Exception)
        {
            ok = false;
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
        bool verified = VerifySignature(Request, body);
        return Ok(new{Ok = verified});

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
    private bool VerifySignature(HttpRequest request, string requestBody)
    {
        bool ok = false;
        string clientId = UserHelper.GetClaimValue(User, "CLIENT_ID");
        HttpHeader headers = RequestHeaderHelper.GetHeader(request);
        string httpMethod = request.Method;
        string endpoint = "/v1.0/transfer-va/GetInquirySignature";
        var tokenHeaders = request.Headers["Authorization"].FirstOrDefault();
        string token = tokenHeaders.Split(' ').LastOrDefault();
        string hexbody = GetHexSha256(requestBody);
        string tosing = string.Concat(httpMethod, ":", endpoint, ":", token, ":", hexbody, ":", headers.xTimestamp);
        ok = SignatureVerifier.VerifyHmacSha512(tosing, headers.xSignature, clientId);
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
        string minified = Regex.Replace(input, @"\s+", " ");

        // Trim leading and trailing whitespace
        return minified.Trim();
    }
}