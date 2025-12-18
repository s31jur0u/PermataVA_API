using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

public class B2BRequest()
{
    public string grantType { get; set; }
    public object additionalInfo { get; set; }
}
public class VaInquiryRequest()
{
    [Required]
    [Length(8,8, ErrorMessage = "Invalid Format")]

    public string partnerServiceId { get; set; }
    [Required]
    [Length(12,18, ErrorMessage = "Invalid Format")]

    public string customerNo { get; set; }
    [Required]
    [Length(12,26, ErrorMessage = "Invalid Format")]

    public string virtualAccountNo { get; set; }
    public int channelCode { get; set; }
    [Required]
    public string inquiryRequestId { get; set; }
    
    public string trxDateInit { get; set; }
    public string language { get; set; }
    public VaAmountBase? amount  { get; set; }
    public  string? hashedSourceAccountNo { get; set; } 
    public  string? sourceBankCode { get; set; } 
    public object additionalInfo { get; set; } 
    public  string? passApp { get; set; } 
}


public class VaPaymentRequest() 
{
    [Required]    
    [Length(8,8, ErrorMessage = "Invalid Format")]

    public string partnerServiceId { get; set; }
    [Required]    
    [Length(12,18, ErrorMessage = "Invalid Format")]

    public string customerNo { get; set; }
    [Required]    
    [Length(12,26, ErrorMessage = "Invalid Format")]
    public string virtualAccountNo { get; set; }
    public string? virtualAccountName { get; set; }
    public string? virtualAccountEmail { get; set; }
    public string? virtualAccountPhone { get; set; }
    public string? trxId { get; set; }
    [Required]    
    public string paymentRequestId { get; set; }
    public int? channelCode { get; set; }
    public string? hashedSourceAccountNo { get; set; }
    public string? sourceBankCode { get; set; }
    public VaAmountBase paidAmount { get; set; }
    public VaAmountBase? cumulaivePaymentAmount { get; set; }
    public string? paidBills { get; set; }
  public VaAmountBase totalAmount  { get; set; }
    public string? trxDateTime { get; set; }
    public string referenceNo { get; set; }
    public string? journalNum { get; set; }
    public string? paymentType { get; set; }
    public string flagAdvise { get; set; }
    public string subCompany { get; set; }
    public List<PaymentRequestBillDetail> billDetails { get; set; }
    public List<VaLanguage> freeTexts { get; set; }
    public object additionalInfo { get; set; }
}

// public class PaidAmount
// {
//     public string value { get; set; }
//     public string currency { get; set; }
// }
//
// public class TotalAmount
// {
//     public string value { get; set; }
//     public string currency { get; set; }
// }
//
// public class VaPaymentRequest
// {
//     public string partnerServiceId { get; set; }
//     public string customerNo { get; set; }
//     public string virtualAccountNo { get; set; }
//     public string virtualAccountName { get; set; }
//     public string virtualAccountEmail { get; set; }
//     public string virtualAccountPhone { get; set; }
//     public string trxId { get; set; }
//     public string paymentRequestId { get; set; }
//     public int channelCode { get; set; }
//     public string hashedSourceAccountNo { get; set; }
//     public string sourceBankCode { get; set; }
//     public PaidAmount paidAmount { get; set; }
//     public object cumulativePaymentAmount { get; set; } // Can be null
//     public string paidBills { get; set; }
//     public TotalAmount totalAmount { get; set; }
//     public DateTime trxDateTime { get; set; }
//     public string referenceNo { get; set; }
//     public string journalNum { get; set; }
//     public string paymentType { get; set; }
//     public string flagAdvise { get; set; }
//     public string subCompany { get; set; }
//     public List<object> billDetails { get; set; } // Can contain null values
//     public List<object> freeTexts { get; set; } // Empty array
//     public Dictionary<string, object> additionalInfo { get; set; } // Empty object
// }

public class PaymentRequestBillDetail() : BillDetail
{
    public string billReferenceNo { get; set; }
}