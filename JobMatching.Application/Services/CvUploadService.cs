using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class CvUploadService
{
    private readonly string _uploadPath;
    private readonly string[] _allowedExtensions;

    public CvUploadService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _uploadPath = Path.Combine(environment.WebRootPath, configuration["FileStorage:UploadPath"]);
        _allowedExtensions = configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>();

        if (!Directory.Exists(_uploadPath)) Directory.CreateDirectory(_uploadPath);
    }

    public async Task<string?> UploadCvAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        if (!_allowedExtensions.Contains(extension)) return null;

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return fileName;
    }
}
