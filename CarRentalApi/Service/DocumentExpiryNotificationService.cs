using CarRentalApi.Data;
using CarRentalApi.Model;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

public class DocumentExpiryNotificationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DocumentExpiryNotificationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;

            // Calculate delay until the next 7:00 AM
            var nextRunTime = DateTime.Today.AddHours(7);
            if (now > nextRunTime)
            {
                nextRunTime = nextRunTime.AddDays(1);
            }
            var delay = nextRunTime - now;

            // Wait until the next 7:00 AM
            await Task.Delay(delay, stoppingToken);

            // Check for expiring documents and send emails
            await CheckAndSendExpiringDocumentEmails();
           /* var delay = TimeSpan.FromMinutes(0.2); // Run after 10 minutes
            await Task.Delay(delay, stoppingToken);

            // Check for expiring documents and send emails
            await CheckAndSendExpiringDocumentEmails();*/
        }
    }

    private async Task CheckAndSendExpiringDocumentEmails()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var today = DateTime.Now;
        var expiringDocuments = await dbContext.DriverDocuments
            .Where(doc => doc.ExpireDate.HasValue &&
                          doc.ExpireDate.Value.Date == today.AddDays(20).Date)
            .ToListAsync();
        var expiringDocumentsVehicle = await dbContext.Document
            .Where(doc => doc.ExpireDate.HasValue &&
                          doc.ExpireDate.Value.Date == today.AddDays(20).Date)
            .ToListAsync();

        foreach (var document in expiringDocuments)
        {
            var driver = await dbContext.Driver.FirstOrDefaultAsync(d => d.DriverId == document.DriverId);
            if (driver != null && !string.IsNullOrEmpty(driver.Email) && document.IsMailSent== false)
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("noreply@blueberry-travel.com"),
                    Subject = "Document Expiry Reminder",
                    Body = $"Dear Driver,\n\nYour document \"{document.Name}\" is set to expire on {document.ExpireDate:dd-MM-yyyy}. Please renew it at your earliest convenience.\n\nBest regards,\nCar Rental Team",
                    IsBodyHtml = false,
                };
                await SendEmailReminder(driver.Email, mailMessage);
                document.IsMailSent = true;
                dbContext.DriverDocuments.Update(document);
            }
        }
        
        foreach (var document in expiringDocumentsVehicle)
        {
            CustomDocumentData documentData = new CustomDocumentData();
            documentData.id = document.DocumentId;
            documentData.Name = document.Name;
            documentData.ExpireDate = document.ExpireDate;

            var vehicle = await dbContext.Vehicle.FirstOrDefaultAsync(v => v.VehicleId == document.VehicleId);

            if (vehicle != null && vehicle.CreatedBy != null && document.IsMailSent == false)
            {
                // Fetch the email using the CreatedBy ID
                var userEmail = await GetUserEmailAsync(vehicle.CreatedBy);

                if (!string.IsNullOrEmpty(userEmail))
                {
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress("noreply@blueberry-travel.com"),
                        Subject = "Document Expiry Reminder",
                        Body = $"Dear Manager,\n\nYour document \"{document.Name}\" with id {document.DocumentId} is set to expire on {document.ExpireDate:dd-MM-yyyy} of Vehicle number {vehicle.VehicleNumber}. Please renew it at your earliest convenience.\n\nBest regards,\nCar Rental Team",
                        IsBodyHtml = false,
                    };

                    await SendEmailReminder(userEmail,  mailMessage);
                    document.IsMailSent = true;
                    dbContext.Document.Update(document);
                }
            }
        }
        await dbContext.SaveChangesAsync();

    }
    // Helper Method to Call the API and Retrieve Email
    private async Task<string> GetUserEmailAsync(int? createdById)
    {
        using (var httpClient = new HttpClient())
        {
            try
            {
                string apiUrl = $"https://cbt-admin-api.blueberry-travel.com/api/Account/GetUserEmailusingGetById?Id={createdById}";
                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result?.Data?.Email;
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error fetching email for ID {createdById}: {ex.Message}");
            }
        }
        return null;
    }


    private async Task SendEmailReminder(string email,MailMessage mailMessage)
    {
        try
        {
            using var smtpClient = new SmtpClient("smtp.zeptomail.in") // Replace with your SMTP server
            {
                Port = 587,
                Credentials = new System.Net.NetworkCredential("emailapikey", "PHtE6r0FEOHuimZ9oBEB4v/rFMb3YI8u9ellLwNHtYkUCPADHE0Hqtwrljbi+h0vVaZKE/KawYw5ubmfsrmHIGrkNjpODWqyqK3sx/VYSPOZsbq6x00UsFocckPdXYDmd9Zr1ifXvt3fNA=="),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20000, // Optional: Set a timeout in milliseconds
            };

            smtpClient.SendCompleted += (s, e) =>
            {
                if (e.Error != null)
                {
                    Console.WriteLine($"Send Error: {e.Error.Message}");
                }
                else
                {
                    Console.WriteLine("Email sent successfully.");
                }
            };
            

            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
    }
}
public class CustomDocumentData
{
       public string Name { get; set; }
       public DateTime? ExpireDate { get; set; }
       public int id { get; set; }
     

}
public class ApiResponse
{
    public string Status { get; set; }
    public ApiResponseData Data { get; set; }
    public string Message { get; set; }
}

public class ApiResponseData
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
}
