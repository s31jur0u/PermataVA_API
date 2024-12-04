using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

public class VaData()
{
    public string inquiryStatus { get; set; }
    public string partnerServiceId { get; set; }
    public string customerNo { get; set; }
    public string virtualAccountNo { get; set; }

    public string virtualAccountName { get; set; }
    public string inquiryRequestId { get; set; }
    public VaTotalAmount totalAmount { get; set; }
    public AdditionalInfo additionalInfo { get; set; }
}

public class VaTotalAmount()
{
    [Required]
        public string value { get; set; }
    [Required]
    public string currency { get; set; }
}

public class AdditionalInfo()
{
    public string transactionId { get; set; }
}

public class AdditionalInfo2() :AdditionalInfo
{
    public string sourceAccountName{ get; set; }
}

public class VaPaymentBase()
{
    [Required]    
    public string partnerServiceId { get; set; }
    [Required]    
    public string customerNo { get; set; }
    [Required]    
    public string virtualAccountNo { get; set; }
    [Required]    
    public string paymentRequestId { get; set; }
    [JsonIgnore]
    public string virtualAccountName { get; set; }
    public VaTotalAmount paidAmount { get; set; }
    public VaTotalAmount totalAmount { get; set; }
}
