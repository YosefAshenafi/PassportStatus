using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfDataExtractor.Models;
using PdfDataExtractor.Services;
using System.Text.Json;

namespace PdfDataExtractor.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly PdfProcessingService _pdfProcessingService;

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    public string? JsonResult { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IndexModel(ILogger<IndexModel> logger, PdfProcessingService pdfProcessingService)
    {
        _logger = logger;
        _pdfProcessingService = pdfProcessingService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (UploadedFile == null)
            {
                ErrorMessage = "Please select a file to upload";
                return Page();
            }

            var extractedData = await _pdfProcessingService.ProcessPdfAsync(UploadedFile);
            JsonResult = JsonSerializer.Serialize(extractedData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF");
            ErrorMessage = $"Error processing PDF: {ex.Message}";
            return Page();
        }
    }
}
