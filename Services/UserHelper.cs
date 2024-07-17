using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
public class UserHelper{

    public static string GetClaimValue(ClaimsPrincipal claims, string claim) {

        string value = string.Empty;
value = claims.Claims.Where(c => c.Type == claim).Select(c => c.Value).FirstOrDefault();
        
        return value;

    }
}