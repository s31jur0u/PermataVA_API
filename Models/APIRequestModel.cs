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
    [Length(12,16, ErrorMessage = "Invalid Format")]

    public string customerNo { get; set; }
    [Required]
    [Length(12,16, ErrorMessage = "Invalid Format")]

    public string virtualAccountNo { get; set; }
    // public int channelCode { get; set; }
    [Required]
    public string inquiryRequestId { get; set; }
}


public class VaPaymentRequest() 
{
    [Required]    
    [Length(8,8, ErrorMessage = "Invalid Format")]

    public string partnerServiceId { get; set; }
    [Required]    
    [Length(12,16, ErrorMessage = "Invalid Format")]

    public string customerNo { get; set; }
    [Required]    
    [Length(12,16, ErrorMessage = "Invalid Format")]
    public string virtualAccountNo { get; set; }
    public string? virtualAccountName { get; set; }
    [Required]    
    public string paymentRequestId { get; set; }
    public int? channelCode { get; set; }
    public string? hashedSourceAccountNo { get; set; }
    public VaTotalAmount paidAmount { get; set; }
    public VaTotalAmount totalAmount { get; set; }
  
    public string? trxDateTime { get; set; }
    public AdditionalInfo2? additionalInfo { get; set; }
}
