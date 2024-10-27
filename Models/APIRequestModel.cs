using System.ComponentModel.DataAnnotations;

public class B2BRequest()
{
    public string grantType { get; set; }
    public object additionalInfo { get; set; }
}
public class VaInquiryRequest()
{
    [Required]
    public string partnerServiceId { get; set; }
    [Required]
    public string customerNo { get; set; }
    [Required]
    public string virtualAccountNo { get; set; }
    // public int channelCode { get; set; }
    [Required]
    public string inquiryRequestId { get; set; }
}


public class VaPaymentRequest() : VaPaymentBase
{
    public int channelCode { get; set; }
    public string hashedSourceAccountNo { get; set; }
    public string trxDateTime { get; set; }
    public AdditionalInfo2 additionalInfo { get; set; }
}
