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
    public VaAmountBase cumulaivePaymentAmount { get; set; }
    public string? paidBills { get; set; }
  public VaAmountBase totalAmount  { get; set; }
    public string? trxDateTime { get; set; }
    public string referenceNo { get; set; }
    public string journalNum { get; set; }
    public string paymentType { get; set; }
    public string flagAdvise { get; set; }
    public string subCompany { get; set; }
    public string subCompanyCode { get; set; }
    public List<BillDetail> billDetails { get; set; }
    public List<VaLanguage> freeTexts { get; set; }
    public object additionalInfo { get; set; }
}
