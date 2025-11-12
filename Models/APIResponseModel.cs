public class ApiBaseResponse(){
    public string responseCode { get; set; }
    public string responseMessage { get; set;}
    
}

public class AccessTokenResponse :ApiBaseResponse{
    public string accessToken { get; set; }
    public string tokenType { get; set; }
    public string expiresIn { get; set; }
}

public class VaInquiryResponse: ApiBaseResponse{

    public VaData virtualAccountData { get; set; }
}

public class VaPaymentResponse : ApiBaseResponse
{
    public VaDataPayment virtualAccountData { get; set; }
}

public class VaDataPayment
{
    public VaLanguage paymentFlagReason { get; set; }
    public string partnerServiceId { get; set; }
    public string customerNo { get; set; }
    public string virtualAccountNo{ get; set; }
    public string virtualAccountName { get; set; }
    public string paymentRequestId { get; set; }
    public VaAmountBase paidAmount { get; set; }
    public VaAmountBase totalAmount { get; set; }
public string trxDateTime {get; set;}
public string referenceNo { get; set; }
public string paymentFlagStatus { get; set; }
public object billDetails { get; set; }
public List<VaLanguage> freeTexts { get; set; }
public object additionalInfo { get; set; }

}