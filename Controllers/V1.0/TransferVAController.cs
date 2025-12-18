using System.Data;
using System.Diagnostics;
using System.Net;
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

[Route("openapi/v1.0/transfer-va/[action]")]
[JwtAuthorize]
public class TransferVaController : ControllerBase
{
    private readonly IJwtTokenGeneratorService _jwtTokenGeneratorService;
    private readonly IConfiguration _config;
    private static ISqlConnectionFactory _sqlConnectionFactory;
    private readonly string _channelId;
    private readonly string _partnerId;

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
        _channelId = _config["CHANNEL_ID"];
        _partnerId = _config["PARTNER_ID"];
    }


    [HttpPost("")]
    public IActionResult Inquiry([FromBody] VaInquiryRequest request)
    {
        bool ok = false;
        decimal billtotalAmount = 0;
        string vaName = string.Empty;
        string billNumber = string.Empty;
        string billDescription = string.Empty;
        int maxId = 0;

        HttpStatusCode statusCode = HttpStatusCode.BadRequest;

        HttpHeader header = new();
        string body = string.Empty;
        VaInquiryResponse response = new();
        VaData vadata = new();
        vadata.freeTexts = new List<object>();
        vadata.billDetails = new List<object>();
        List<BillDetail> listbilldetails = new();

        var settings = new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
        };
        header = RequestHeaderHelper.GetHeader(Request);
        body = JsonConvert.SerializeObject(request, settings);
        string headerstring = JsonConvert.SerializeObject(header, settings);

        _logger.Information(headerstring);
        _logger.Information(body);
        body = JsonConvert.SerializeObject(request);
        JsonConvert.PopulateObject(body, vadata);
        vadata.inquiryStatus = "01";
        // vadata.inquiryReason = new()
        // {
        //     english = "Virtual Account Not Found",
        //     indonesia = "Virtual Account Tidak Ditemukan"
        // };
        vadata.subCompany = "00000";
        vadata.billDetails = new List<object>();
        vadata.virtualAccountName = "";
        vadata.totalAmount = new VaAmountBase
        {
            value = "",
            currency = ""
        };
        response.additionalInfo = new { };
        ApiBaseResponse failedApiBaseResponse = new();
        bool inconsistent = false;
        string status_inconsistent = "";
        VaLanguage reason_inconsistent = new();
        if (CheckExternalId(header.xExternalId,vadata.inquiryRequestId, "inquiry",out status_inconsistent,out reason_inconsistent,out inconsistent, out failedApiBaseResponse))
            // if(true)
        {
            try
            {

                string trimmedVaAcc = String.Empty;
                ok = true;
                bool need_verify = false;
                bool.TryParse(_config["VERIFY_SIGNATURE:INQUIRY"], out need_verify);

                var sr = new StreamReader(Request.Body);
                sr.BaseStream.Seek(0, SeekOrigin.Begin);
                var rawMessage = sr.ReadToEnd();


                if (header.channelId != _channelId || header.xPartnerId != _partnerId)
                {
                    failedApiBaseResponse.responseCode = "4012400";
                    failedApiBaseResponse.responseMessage = "Unauthorized. [Unknown client]";
                    statusCode = HttpStatusCode.Unauthorized;
                    throw new Exception("unauthorized");
                }


                if (need_verify)
                    ok = VerifySignature(Request, rawMessage, "inquiry");
                if (ok)
                {
                    if (ModelState.IsValid)
                    {
                        trimmedVaAcc = request.virtualAccountNo.Trim();
                        if (_config["EXPIRED_VA"] == trimmedVaAcc)
                        {
                            response.responseCode = "4042419";
                            response.responseMessage = "Invalid Bill/Virtual Account";

                            statusCode = HttpStatusCode.NotFound;
                            throw new Exception("Invalid Bill/Virtual Account");

                        }
                        else
                        {

                            if (CheckVAExists(trimmedVaAcc))
                            {
                                SqlCommand cmd = new();
                                SqlDataReader reader;
                                if (CheckGotBill(trimmedVaAcc))
                                {
                                    using (SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection())
                                    {
                                        cmd = new SqlCommand(
                                            "EXEC USPPA_GET_BILLVA @COMPANY_CODE,@CUSTOMER_NUMBER,@TRACE_NO ",
                                            sqlconn);
                                        cmd.Parameters.AddWithValue("@COMPANY_CODE", request.partnerServiceId.Trim());
                                        cmd.Parameters.AddWithValue("@CUSTOMER_NUMBER", trimmedVaAcc);
                                        cmd.Parameters.AddWithValue("@TRACE_NO", request.inquiryRequestId);
                                        cmd.ExecuteNonQuery();

                                        cmd = new SqlCommand(
                                            "SELECT  TOTALAMOUNT, CUSTOMERNAME, PA_PERMATA_LOG_ID AS MAX_ID ,BillNumber, BillDescription  FROM PA_PERMATA_LOG WHERE VACD = @VA_CD AND STATUS=0",
                                            sqlconn);
                                        cmd.Parameters.AddWithValue("@VA_CD", trimmedVaAcc);
                                        reader = cmd.ExecuteReader();
                                        bool gotRows = reader.HasRows;
                                        if (gotRows)
                                        {
                                            while (reader.Read())
                                            {
                                                billtotalAmount = billtotalAmount + reader.GetDecimal(0);
                                                vaName = reader.GetString(1);
                                                maxId = reader.GetInt32(2);
                                                billNumber = reader.GetString(3);
                                                billDescription = reader.GetString(4);
                                            }
                                        }
                                        else
                                        {
                                            reader.Close();
                                            cmd = new SqlCommand(
                                                "SELECT top 1 VACD  FROM PA_PERMATA_LOG WHERE VACD = @VA_CD",
                                                sqlconn);
                                            cmd.Parameters.AddWithValue("@VA_CD", trimmedVaAcc);

                                            SqlDataReader reader2 = cmd.ExecuteReader();
                                            bool gotRows2 = false;
                                            gotRows2 = reader2.HasRows;

                                            reader2.Close();

                                            if (gotRows2)
                                            {
                                                response.responseCode = "4042414";
                                                response.responseMessage = "Paid Bill";
                                                vadata.inquiryReason = new VaLanguage
                                                {
                                                    english = "Bill Has Been Paid",
                                                    indonesia = "Tagihan Sudah Terbayar"
                                                };
                                            }
                                            statusCode = HttpStatusCode.NotFound;

                                            throw new Exception("Bill Has Been Paid");

                                        }

                                    }
                                }
                                else
                                {
                                    response.responseCode = "4042414";
                                    response.responseMessage = "Paid Bill";
                                    vadata.inquiryReason = new VaLanguage
                                    {
                                        english = "Bill Has Been Paid",
                                        indonesia = "Tagihan Sudah Terbayar"
                                    };
                                    statusCode = HttpStatusCode.NotFound;

                                    throw new Exception("Bill Has Been Paid");
                                }
                            }
                            else
                            {
                                response.responseCode = "4042412";
                                response.responseMessage = "Invalid Bill/Virtual Account [Not Found]";
                                vadata.inquiryStatus = "01";
                                vadata.inquiryReason = new()
                                {
                                    english = "Virtual Account Not Found",
                                    indonesia = "Virtual Account Tidak Ditemukan"
                                };

                                statusCode = HttpStatusCode.NotFound;

                                throw new Exception("No Record Found");
                            }
                        }

                        VaAmountBase amountBase = new();
                        AdditionalInfo additionalInfo = new();
                        amountBase.currency = "IDR";
                        amountBase.value = billtotalAmount.ToString("#0.00");
                        additionalInfo.transactionId = maxId.ToString();

                        vadata.totalAmount = amountBase;

                        response.responseCode = "2002400";
                        response.responseMessage = "Successful";
                        vadata.inquiryStatus = "00";
                        vadata.inquiryReason = new()
                        {
                            english = "Success",
                            indonesia = "Sukses"
                        };
                        vadata.subCompany = "00000";
                        BillDetail billDetail = new()
                        {
                            billNo = billNumber,
                            billDescription = new()
                            {
                                english = billDescription,
                                indonesia = billDescription
                            },
                            billSubCompany = "00000",
                            billAmount = amountBase,
                            additionalInfo = new { }
                        };
                        // listbilldetails.Add(billDetail);
                        vadata.virtualAccountName = vaName;
                        vadata.billDetails = listbilldetails;

                    }
                    else
                    {
                        ok = false;
                        failedApiBaseResponse = GetModelInvalidError(ModelState, "inquiry");
                        response.responseCode = failedApiBaseResponse.responseCode;
                        response.responseMessage = failedApiBaseResponse.responseMessage;
                    }
                }
                else
                {

                    ok = false;
                    failedApiBaseResponse.responseCode = "4012400";
                    failedApiBaseResponse.responseMessage = "Unauthorized. [Signature]";
                    statusCode = HttpStatusCode.Unauthorized;

                }
            }
            catch (Exception ex)
            {
                ok = false;

            }
        }
        else
        {
            response.responseCode = failedApiBaseResponse.responseCode;
            response.responseMessage = failedApiBaseResponse.responseMessage;
 vadata.inquiryReason = new VaLanguage
            {
                english = "Cannot use the same X-EXTERNAL-ID",
                indonesia = "Tidak bisa menggunakan X-EXTERNAL-ID yang sama"
            };
            statusCode = HttpStatusCode.Conflict;
            if (inconsistent)
            {
                vadata.inquiryStatus = status_inconsistent;
                vadata.inquiryReason = reason_inconsistent;
                statusCode = HttpStatusCode.NotFound;
            }
            
           
        }

        response.virtualAccountData = vadata;
        if (ok)
        {
            UpdateVaApiLog(header.xExternalId, vadata.inquiryRequestId, "inquiry", vadata.inquiryStatus,vadata.inquiryReason);
        }
        return ok
            ? Ok(response)
            : statusCode switch
            {
                HttpStatusCode.Unauthorized => Unauthorized(failedApiBaseResponse),
                HttpStatusCode.NotFound => NotFound(response),
                HttpStatusCode.Conflict => Conflict(response),
                _ => BadRequest(response)
            };
        ;
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
            .Select(s => s.Key)
            .ToList();

        var formatErrors = errors
            .Where(e => e.Value.Any(msg => msg.Contains("Invalid")))
            .Select(s => s.Key)
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
        VaDataPayment vaDataPayment = new();
        ApiBaseResponse failedApiBaseResponse = new();
        failedApiBaseResponse.responseCode = "4002501";
        failedApiBaseResponse.responseMessage = "Failed";
        header = RequestHeaderHelper.GetHeader(base.Request);
        body = JsonConvert.SerializeObject(request, jsonSerializerSettings);
        _logger.Information("payment");
        HttpStatusCode statusCode = HttpStatusCode.BadRequest;
        
        var sr = new StreamReader(Request.Body);
        sr.BaseStream.Seek(0, SeekOrigin.Begin);
        var rawMessage = sr.ReadToEnd();
        
        JsonConvert.PopulateObject(body, vaDataPayment);
        response.additionalInfo = new() { };
        // vaDataPayment.billDetails = new List<object>(){};
        vaDataPayment.billDetails = new List<object>();
        bool inconsistent = false;
        string status_inconsistent = "";
        VaLanguage reason_inconsistent = new();
        if (CheckExternalId(header.xExternalId,vaDataPayment.paymentRequestId, "payment",out status_inconsistent,out reason_inconsistent,out inconsistent, out failedApiBaseResponse))
//     ApiBaseResponse failedApiBaseResponse = new();
          //  if(true)
        {
            try
            {
                string trimmedVaNo = String.Empty;
                ok = true;

              


                if (header.channelId != _channelId || header.xPartnerId != _partnerId)
                {
                    failedApiBaseResponse.responseCode = "4012500";
                    failedApiBaseResponse.responseMessage = "Unauthorized. [Unknown client]";
                    statusCode = HttpStatusCode.Unauthorized;
                    throw new Exception("unauthorized");
                }



                bool need_verify = false;
                bool.TryParse(_config["VERIFY_SIGNATURE:PAYMENT"], out need_verify);
                if (need_verify)
                    ok = VerifySignature(Request, rawMessage, "payment");
                if (ok)
                {
                    if (ModelState.IsValid)
                    {

                        header = RequestHeaderHelper.GetHeader(Request);
                        
                        trimmedVaNo = request.virtualAccountNo.Trim();

                        decimal totalAmount = 0;
                        decimal paidAmount = 0;
                        Decimal.TryParse(request.paidAmount.value, out totalAmount);
                        Decimal.TryParse(request.paidAmount.value, out paidAmount);


                        if (CheckVAExists(trimmedVaNo))
                        {

                            SqlCommand cmd = new();
                            using SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection();
                            cmd = new SqlCommand(
                                "SELECT TOTALAMOUNT,  CUSTOMERNAME, PA_PERMATA_LOG_ID  AS MAX_ID FROM PA_PERMATA_LOG WHERE VACD = @VA_CD AND STATUS=0",
                                sqlconn);
                            cmd.Parameters.AddWithValue("@VA_CD", trimmedVaNo);
                         using   SqlDataReader reader = cmd.ExecuteReader();
                            string customername = string.Empty;
                            bool gotRows = true;
                            decimal billAmount = 0m;
                            try
                            {
                                gotRows = reader.HasRows;
                                while (reader.Read())
                                {
                                    billAmount = reader.GetDecimal(0);
                                    customername = reader.GetString(1);

                                }

                                reader.Close();
                            }
                            catch (Exception e)
                            {

                            }
                            finally
                            {
                                reader.Close();
                            }
                            cmd = new SqlCommand("SELECT 1 FROM PA_PERMATA_LOG WHERE PAYMENTREQUESTID = @PAYMENT_REQUEST_ID", sqlconn);
                            cmd.Parameters.AddWithValue("@PAYMENT_REQUEST_ID", request.paymentRequestId);
                            bool haspaid = cmd.ExecuteScalar()  != null;
                               
                            
                            if (!haspaid && gotRows)
                            {

                                if (!billAmount.Equals(paidAmount))
                                {
                                    response.responseCode = "4042513";
                                    response.responseMessage = "Invalid Amount";
                                    ok = false;
                                    statusCode = HttpStatusCode.NotFound;

                                    vaDataPayment.paymentFlagStatus = "01";
                                    vaDataPayment.paymentFlagReason = new()
                                    {
                                        english = "Invalid Amount",
                                        indonesia = "Invalid Amount"
                                    };
                                }
                                else
                                {
                                    //[FD] Comment for testing
                                    try
                                    {
                                        cmd = new SqlCommand(
                                            "EXEC usppa_pay_billva @COMPANY_CODE,@CUSTOMER_NUMBER,@CUSTOMER_NAME,@PAID_AMOUNT,@TOTAL_AMOUNT,@PAYMENT_REQUEST_ID ",
                                            sqlconn);
                                        cmd.Parameters.AddWithValue("@COMPANY_CODE", request.partnerServiceId.Trim());
                                        cmd.Parameters.AddWithValue("@CUSTOMER_NUMBER", trimmedVaNo);
                                        cmd.Parameters.AddWithValue("@CUSTOMER_NAME", customername);
                                        cmd.Parameters.AddWithValue("@PAID_AMOUNT", request.paidAmount.value);
                                        cmd.Parameters.AddWithValue("@TOTAL_AMOUNT", request.paidAmount.value);
                                        cmd.Parameters.AddWithValue("@PAYMENT_REQUEST_ID", request.paymentRequestId);
                                        SqlDataReader sp_reader = cmd.ExecuteReader();

                                        string errorcode = string.Empty;
                                        while (sp_reader.Read())
                                        {
                                            errorcode = sp_reader.GetString(0);
                                        }

                                        _logger.Information(
                                            $"external id: {header.xExternalId} - errorcode: {errorcode}");
                                        if (errorcode == "00")
                                        {
                                            response.responseCode = "2002500";
                                            response.responseMessage = "Successful";
                                            vaDataPayment.virtualAccountName = customername;
                                            vaDataPayment.paymentFlagReason = new()
                                            {
                                                english = "Success",
                                                indonesia = "Sukses"
                                            };
                                            vaDataPayment.totalAmount = vaDataPayment.paidAmount;
                                            vaDataPayment.referenceNo = request.referenceNo;
                                            vaDataPayment.paymentFlagStatus = "00";
                                            // PaymentBillDetail paymentBillDetail = new()
                                            // {
                                            //     billNo = request.billDetails.FirstOrDefault()?.billNo,
                                            //     billDescription = request.billDetails.FirstOrDefault()?.billDescription,
                                            //     subCompany = "00000",
                                            //     billAmount = request.billDetails.FirstOrDefault()?.billAmount,
                                            //     additionalInfo = new() { },
                                            //     billerReferenceId =
                                            //         request.billDetails.FirstOrDefault()?.billReferenceNo,
                                            //     status = "00",
                                            //     reason = new()
                                            //     {
                                            //         english = "Success",
                                            //         indonesia = "Sukses"
                                            //     },
                                            //     freeTexts = null
                                            // };
                                            response.virtualAccountData = vaDataPayment;
                                            ok = true;
                                        }

                                    }
                                    catch (Exception e)
                                    {

                                        _logger.Information(
                                            $"external id: {header.xExternalId} - stacktrace: {e.StackTrace}");

                                        Console.WriteLine(e);
                                        ok = false;
                                    }
                                }

                            }
                            else
                            {
                                response.responseCode = "4042514";
                                response.responseMessage = "Paid Bill";
                                vaDataPayment.paymentFlagStatus = "01";
                                vaDataPayment.paidAmount = new ()
                                {
                                    value = "0.00",
                                    currency = "IDR"
                                };
  
                                vaDataPayment.totalAmount = new ()
                            
                                {
                                    value = "0.00",
                                    currency = "IDR"
                                };
                                vaDataPayment.paymentFlagReason = new VaLanguage
                                {
                                    english = "Bill Has Been Paid",
                                    indonesia = "Tagihan Sudah Terbayar"
                                };
                                statusCode = HttpStatusCode.NotFound;

                                throw new Exception("Bill Has Been Paid");
                            }
                        }
                        else
                        {
                            ok = false;

                            response.responseCode = "4042512";
                            response.responseMessage = "Invalid Bill/Virtual Account [Not Found]";
                            vaDataPayment.paymentFlagStatus = "01";
                            vaDataPayment.paymentFlagReason = new()
                            {
                                english = "Virtual Account Not Found",
                                indonesia = "Virtual Account Tidak Ditemukan"
                            };
                            vaDataPayment.virtualAccountName = "";
                            
                            vaDataPayment.paidAmount = new ()
                            {
                                value = "",
                                currency = ""
                            };
  
                            vaDataPayment.totalAmount = new ()
                            {
                                value = "",
                                currency = ""
                            };

                            statusCode = HttpStatusCode.NotFound;

                            throw new Exception("No Record Found");
                        }

                    }
                    else
                    {
                        ok = false;
                        failedApiBaseResponse = GetModelInvalidError(ModelState, "payment");

                        response.responseCode = failedApiBaseResponse.responseCode;
                        response.responseMessage = failedApiBaseResponse.responseMessage;
                        vaDataPayment.paymentFlagStatus = "01";
                        vaDataPayment.paymentFlagReason = new()
                        {
                            english = response.responseMessage,
                            indonesia = response.responseMessage
                        };

                        statusCode = HttpStatusCode.BadRequest;


                    }
                }
                else
                {
                    ok = false;
                    failedApiBaseResponse.responseCode = "4012500";
                    failedApiBaseResponse.responseMessage = "Unauthorized. [Signature]";
                    statusCode = HttpStatusCode.Unauthorized;

                }

            }
            catch (Exception ex)
            {
                _logger.Information(ex.Message);
                _logger.Information(ex.InnerException?.Message ?? ex.Message);

                ok = false;
            }

        }
        else
        {
            response.responseCode = failedApiBaseResponse.responseCode;
            response.responseMessage = failedApiBaseResponse.responseMessage;
            vaDataPayment.paymentFlagReason = new VaLanguage
            {
                english = "Cannot use the same X-EXTERNAL-ID",
                indonesia = "Tidak bisa menggunakan X-EXTERNAL-ID yang sama"
            };
            vaDataPayment.paymentFlagStatus = "01";
            vaDataPayment.virtualAccountName = "";
            vaDataPayment.paidAmount = new()
            {
                value = "",
                currency = ""
            };
            vaDataPayment.totalAmount = new()
            {
                value = "",
                currency = ""
            };
            statusCode = HttpStatusCode.Conflict;

            if (inconsistent)
            {
                vaDataPayment.paymentFlagStatus = status_inconsistent;
                vaDataPayment.paymentFlagReason = reason_inconsistent;
                statusCode = HttpStatusCode.NotFound;
            }
            
        }

        response.virtualAccountData = vaDataPayment;

        if (ok)
        {
            UpdateVaApiLog(header.xExternalId, vaDataPayment.paymentRequestId, "payment", vaDataPayment.paymentFlagStatus,vaDataPayment.paymentFlagReason);
        }

        return ok
            ? Ok(response)
            : statusCode switch
            {
                HttpStatusCode.Unauthorized => Unauthorized(failedApiBaseResponse),
                HttpStatusCode.NotFound => NotFound(response),
                HttpStatusCode.Conflict => Conflict(response),
                _ => BadRequest(response)
            };
        
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
            ? "/openapi/v1.0/transfer-va/inquiry"
            : "/openapi/v1.0/transfer-va/payment");
        var tokenHeaders = request.Headers["Authorization"].FirstOrDefault();
        string token = tokenHeaders.Split(' ').LastOrDefault();
        string hexbody = GetHexSha256(requestBody);
        string tosing = string.Concat(httpMethod, ":", endpoint, ":", token, ":", hexbody, ":", headers.xTimestamp);
        Console.WriteLine(tosing);
        ok = SignatureVerifier.VerifyHmacSha512(tosing, headers.xSignature, _config["CLIENT_SECRET"]);
        return ok;
    }

    private string GetHexSha256(string input)
    {
        input = MinifyString(input);
        Console.WriteLine(input);
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


        var settings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None // This is key!
        };

        // JToken parsedJson = JToken.Parse(input,settings);
        var obj = JsonConvert.DeserializeObject<JToken>(input, settings);

        return obj.ToString(Formatting.None); // Minified JSON
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

    private bool CheckExternalId(string externalid, string requestid, string actiontype,out string status_fromdb, out VaLanguage reason, out bool inconsistent, out ApiBaseResponse conflict_resp)
    {
        bool isvalid = false;
        inconsistent = false;
        status_fromdb = "";
        reason = new();

        using SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection();
        SqlCommand sqlCommand = new SqlCommand("Select requestid,status,response From va_api_logs Where externalid = @externalid and api_name = @api_name ", sqlconn);
        if (sqlconn.State != ConnectionState.Open)
            sqlconn.Open();
        sqlCommand.Parameters.AddWithValue("@externalid", externalid);
        sqlCommand.Parameters.AddWithValue("@api_name", actiontype);

        SqlDataReader reader = sqlCommand.ExecuteReader();
        bool gotdata = true;
        gotdata = reader.HasRows;


        string requestid_fromdb;
        string reason_fromdb;
        if (gotdata && actiontype.Equals("payment", StringComparison.InvariantCultureIgnoreCase))
        {
            while (reader.Read())
            {
                requestid_fromdb = reader.GetString(0);
                status_fromdb = reader.GetString(1);
                reason_fromdb = reader.GetString(2);
            
                reason = JsonConvert.DeserializeObject<VaLanguage>(reason_fromdb) ?? new VaLanguage();

                if (requestid_fromdb == requestid)
                {
                    inconsistent = true;
                }
            }
           
            
        }
        reader.Close();

        if (!gotdata)
        {
            sqlCommand =
                new SqlCommand("insert into va_api_logs (api_name,externalid,requestid) values (@api_name,@externalid,@requestid)",
                    sqlconn);
            sqlCommand.Parameters.AddWithValue("@externalid", externalid);
            sqlCommand.Parameters.AddWithValue("@requestid", requestid);
            sqlCommand.Parameters.AddWithValue("@api_name", actiontype);
            sqlCommand.ExecuteNonQuery();
            isvalid = true;
        }

        sqlconn.Close();



        conflict_resp = new()
        {
            responseCode = "409XX00",
            responseMessage = "Conflict"
        };

        string actioncode = actiontype switch
        {
            "inquiry" => "24",
            "payment" => "25",
            _ => "00"
        };

        if (inconsistent)
        {
            conflict_resp.responseCode = "404XX18";
            conflict_resp.responseMessage = "Inconsistent Request";
        }

        conflict_resp.responseCode = conflict_resp.responseCode.Replace("XX", actioncode);
        return isvalid;

    }

    private void UpdateVaApiLog(string externalid, string requestid, string actiontype, string status,VaLanguage reason)
    {

        string reason_string = JsonConvert.SerializeObject(reason);
        
        using SqlConnection sqlconn = _sqlConnectionFactory.GetOpenConnection();
        SqlCommand sqlCommand =
            new SqlCommand("update va_api_logs set status = @status, response = @response where externalid = @externalid and api_name = @api_name  and requestid = @requestid", sqlconn);
        sqlCommand.Parameters.AddWithValue("@externalid", externalid);
        sqlCommand.Parameters.AddWithValue("@requestid", requestid);
        sqlCommand.Parameters.AddWithValue("@api_name", actiontype);
        sqlCommand.Parameters.AddWithValue("@status", status);
        sqlCommand.Parameters.AddWithValue("@response", reason_string);
        sqlCommand.ExecuteNonQuery();
        
        sqlconn.Close();
    }
    
}