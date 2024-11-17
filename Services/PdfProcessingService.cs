using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using PdfDataExtractor.Models;
using System.Text.Json;

namespace PdfDataExtractor.Services;

public class PdfProcessingService
{
    public async Task<string> ProcessPdfAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file was uploaded");

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a PDF");

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var extractedData = new List<ExtractedData>();

        using (var pdfReader = new PdfReader(memoryStream))
        using (var pdfDocument = new PdfDocument(pdfReader))
        {
            var strategy = new SimpleTextExtractionStrategy();
            string text = "";

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                text += PdfTextExtractor.GetTextFromPage(page, strategy);
            }

            // Here you would implement your specific parsing logic
            // This is a simple example - you'd need to adjust based on your PDF structure
            extractedData = ParseText(text);
        }

        // Convert to JSON and write to file
        string jsonString = System.Text.Json.JsonSerializer.Serialize(extractedData, 
            new JsonSerializerOptions { WriteIndented = true });
        
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "extracted_names.json");
        await File.WriteAllTextAsync(filePath, jsonString);

        return filePath; // Return the path where the file was saved
    }

    private List<ExtractedData> ParseText(string text)
    {
        var dataList = new List<ExtractedData>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                       .Select(l => l.Trim())
                       .ToList();
        
        // Find the start of the table (after the headers)
        int tableStart = lines.FindIndex(l => l.Contains("REQUEST_No.") || l.Contains("NAME"));
        
        if (tableStart >= 0)
        {
            // Process all rows after the header
            for (int i = tableStart + 1; i < lines.Count; i++)
            {
                var dataLine = lines[i];
                var columns = dataLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (columns.Length >= 4)
                {
                    // Skip the index number if present
                    int startIndex = char.IsDigit(columns[0][0]) ? 1 : 0;
                    
                    dataList.Add(new ExtractedData
                    {
                        FirstName = columns[startIndex].Trim(),
                        FatherName = columns[startIndex + 1].Trim(),
                        GrandFatherName = columns[startIndex + 2].Trim(),
                        ID = columns[startIndex + 3].Trim()
                    });
                }
            }
        }

        return dataList;
    }
} 