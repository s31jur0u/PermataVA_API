public class APIBaseResponse(){
    public string responseCode { get; set; }
    public string responseMessage { get; set;}
    
}

public class AccessTokenResponse :APIBaseResponse{
    public string accessToken { get; set; }
    public string tokenType { get; set; }
    public string expiresIn { get; set; }
}

public class VAInquiryResponse: APIBaseResponse{

    public VAData virtualAccountData { get; set; }
}

public class VAPaymentResponse : APIBaseResponse
{
    public VAPaymentBase virtualAccountData { get; set; }
}