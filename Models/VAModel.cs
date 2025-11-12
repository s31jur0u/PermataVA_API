using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

public class VaData()
{
    public string inquiryStatus { get; set; }
    public VaLanguage inquiryReason { get; set; }
    public string partnerServiceId { get; set; }
    public string customerNo { get; set; }
    public string virtualAccountNo { get; set; }

    public string virtualAccountName { get; set; }
    public string inquiryRequestId { get; set; }
    public VaAmountBase totalAmount { get; set; }
    public object additionalInfo { get; set; }
    public string subCompany { get; set; }
    public object freeTexts { get; set; }
    public object billDetails { get; set; }
}

public class VaAmountBase()
{
    [Required]
        public string value { get; set; }
    [Required]
    public string currency { get; set; }
}

public class VaLanguage()
{
    public string english { get; set; }
    public string indonesia { get; set; }
}
public class BillDetail()
{
    public string? billCode { get; set; }
    public string billNo { get; set; }
    public string? billName { get; set; }
    public string billShortName { get; set; }
    public VaLanguage billDescription { get; set; }
    public string billSubCompany { get; set; }
    public VaAmountBase billAmount { get; set; }
    public object additionalInfo { get; set; }
}


public class PaymentBillDetail() 
{
    public string billNo {get; set;}
    public VaLanguage billDescription {get; set;}
    public string subCompany {get; set;}
    public VaAmountBase billAmount { get; set; }
    public object additionalInfo { get; set; }
    
    public string billerReferenceId { get; set; }
    public string status { get; set; }
    public VaLanguage reason { get; set; }
    public object freeTexts { get; set; }
    
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
    public VaAmountBase paidAmount { get; set; }
    public VaAmountBase amount { get; set; }
}
