namespace VA_API.Services;
public class LicenseValidationMiddleware
{
    private readonly RequestDelegate _next;

    public LicenseValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Logika untuk memeriksa lisensi
        bool isLicenseValid = CheckLicense();
        if (!isLicenseValid)
        {
            // Jika lisensi tidak valid, kirimkan respons kesalahan
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Service Unavailable");
            return;
        }

        // Jika valid, teruskan request
        await _next(context);
    }

    private bool CheckLicense()
    {
        // Implementasi logika validasi lisensi berdasarkan tanggal
        DateTime licenseExpiryDate = GetLicenseExpiryDate();
        return DateTime.Now.Date <= licenseExpiryDate;
    }

    private DateTime GetLicenseExpiryDate()
    {
        // Contoh membaca tanggal kedaluwarsa dari environment variable
      
        // Jika tidak valid, anggap lisensi telah kedaluwarsa
        return new DateTime(year:2025,month:2,day:28);
    }
}
