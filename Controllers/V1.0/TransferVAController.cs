using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;


[Route("v1.0/transfer-va")]
 [JwtAuthorize]
public class TransferVAController : ControllerBase
{
    private readonly IJwtTokenGeneratorService _jwtTokenGeneratorService;
    private readonly IConfiguration _config;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    public TransferVAController(IJwtTokenGeneratorService jwtTokenGeneratorService, IConfiguration config, ISqlConnectionFactory sqlConnectionFactory)
    {
        _jwtTokenGeneratorService = jwtTokenGeneratorService;
        _config = config;
        _sqlConnectionFactory = sqlConnectionFactory;
    }
    [HttpPost("[action]")]
    public IActionResult Inquiry([FromBody] VAInquiryRequest request)
    {
        bool ok = false;
        decimal billtotalAmount = 0;
        string va_name = string.Empty;
        int max_id = 0;

        HTTPHeader header = new();

        APIBaseResponse failed_response = new();
        failed_response.responseCode = "4002401";
        failed_response.responseMessage = "Failed";
        string body = string.Empty;
        VAInquiryResponse response = new();
        VAData vadata = new();

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
                        bool got_rows = reader.HasRows;
                        reader.Close();
                        if (got_rows)
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
                                va_name = reader.GetString(1);
                                max_id = reader.GetInt32(2);
                            }
                        }
                        else
                        {
                            failed_response.responseCode = "4042412";
                            failed_response.responseMessage = "Bill Not Found";
                            throw new Exception("No Record Found");
                        }
                    }


                    VATotalAmount totalAmount = new();
                    AdditionalInfo additionalInfo = new();
                    totalAmount.currency = "IDR";
                    totalAmount.value = billtotalAmount.ToString("#0.00");
                    additionalInfo.transactionId = max_id.ToString();

                    vadata.totalAmount = totalAmount;
                    vadata.additionalInfo = additionalInfo;
                    response.responseCode = "2002400";
                    response.responseMessage = "Success";
                    vadata.inquiryStatus = "00";
                    vadata.virtualAccountName = va_name;

                    response.virtualAccountData = vadata;

                }
                else
                {

                    ok = false;
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => e.ErrorMessage)
                                     .ToList();
                    failed_response.responseMessage = string.Join(", ", errors);
                }
            }
            else
            {

                ok = false;
                failed_response.responseCode = "4012400";
                failed_response.responseMessage = "Unauhtorized Signature";
            }
        }
        catch (Exception ex)
        {
            ok = false;

        }
        return ok ? Ok(response) : BadRequest(failed_response);
    }

    [HttpPost("[action]")]

    public IActionResult Payment([FromBody] VAPaymentRequest request)
    {
        bool ok = false;
        HTTPHeader header = new();
        string body = string.Empty;
        VAPaymentResponse response = new();
        VAPaymentBase vAPaymentBase = new();
        APIBaseResponse failed_response = new();
        failed_response.responseCode = "4002401";
        failed_response.responseMessage = "Failed";
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
                        failed_response.responseCode ="4042413";
                        failed_response.responseMessage = "Invalid Amount";
                    }
                    SqlCommand cmd = new();
                    // using SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection();
                    //  cmd = new SqlCommand("SELECT SUM(TOTALAMOUNT) AS TOTALAMOUNT, MAX(CUSTOMERNAME) AS CUSTOMERNAME, MAX(PA_PERMATA_LOG_ID)  AS MAX_ID  FROM PA_PERMATA_LOG WHERE VACD = @VA_CD AND STATUS=0", sqlconn);
                    // cmd.Parameters.AddWithValue("@VA_CD", request.virtualAccountNo);
                    // SqlDataReader reader = cmd.ExecuteReader();

                    bool got_rows = true;
                    // got_rows = reader.HasRows;
                    // reader.Close();
                    if (got_rows)
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
                    failed_response.responseMessage = string.Join(", ", errors);
                }
            }
            else
            {
                ok = false;
                failed_response.responseCode = "4012400";
                failed_response.responseMessage = "Unauhtorized Signature";
            }

        }
        catch (System.Exception)
        {
            ok = false;
        }


        return ok ? Ok(response) : Ok(failed_response);
    }

    [HttpPost("[action]")]
    [AllowAnonymous]
    public IActionResult GetPaymentSignature(VAPaymentRequest request)
    {
        HTTPHeader header = new();
        header = RequestHeaderHelper.GetHeader(Request);
        string body = JsonConvert.SerializeObject(request);
        string signature = CreateSignature(Request, body);
        return Ok(signature);

    }

    [HttpPost("[action]")]
    public IActionResult GetInquirySignature(VAInquiryRequest request)
    {
        HTTPHeader header = new();
        header = RequestHeaderHelper.GetHeader(Request);
        string body = JsonConvert.SerializeObject(request);
        string signature = CreateSignature(Request, body);
        return Ok(signature);

    }

        [HttpPost("[action]")]
    [AllowAnonymous]
    public IActionResult VerifyInquirySignature(VAInquiryRequest request)
    {

        string body = JsonConvert.SerializeObject(request);
        bool verified = VerifySignature(Request, body);
        return Ok(new{Ok = verified});

    }
    private string CreateSignature(HttpRequest request, string request_body)
    {
        string signature = "";
        string client_id = UserHelper.GetClaimValue(User, "CLIENT_ID");
        HTTPHeader headers = RequestHeaderHelper.GetHeader(request);
        string http_method = request.Method;
        string endpoint = request.Path;
        var token_headers = request.Headers["Authorization"].FirstOrDefault();
        string token = token_headers.Split(' ').LastOrDefault();
        string hexbody = GetHexSHA256(request_body);
        string tosing = string.Concat(http_method, ":", endpoint, ":", token, ":", hexbody, ":", headers.X_TIMESTAMP);
        signature = SignatureVerifier.CreateHmacSHA512(tosing, client_id);
        return signature;
    }
    private bool VerifySignature(HttpRequest request, string request_body)
    {
        bool ok = false;
        string client_id = UserHelper.GetClaimValue(User, "CLIENT_ID");
        HTTPHeader headers = RequestHeaderHelper.GetHeader(request);
        string http_method = request.Method;
        string endpoint = "/v1.0/transfer-va/GetInquirySignature";
        var token_headers = request.Headers["Authorization"].FirstOrDefault();
        string token = token_headers.Split(' ').LastOrDefault();
        string hexbody = GetHexSHA256(request_body);
        string tosing = string.Concat(http_method, ":", endpoint, ":", token, ":", hexbody, ":", headers.X_TIMESTAMP);
        ok = SignatureVerifier.VerifyHmacSHA512(tosing, headers.X_SIGNATURE, client_id);
        return ok;
    }

    private string GetHexSHA256(string input)
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
