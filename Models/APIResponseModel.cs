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
    public VaPaymentBase virtualAccountData { get; set; }
}