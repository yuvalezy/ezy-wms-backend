# Phase 5: Reports & Label System

## 5.1 Package Report Services

### 5.1.1 Mixed Box Goods Receipt Report

```csharp
public interface IPackageReportService
{
    Task<MixedBoxGoodsReceiptReport> GetMixedBoxGoodsReceiptReportAsync(MixedBoxGoodsReceiptReportRequest request);
    Task<PackageStockReport> GetPackageStockReportAsync(string packageBarcode);
    Task<PackageMovementReport> GetPackageMovementReportAsync(Guid packageId);
    Task<PackageInventoryReport> GetPackageInventoryReportAsync(PackageInventoryReportRequest request);
    Task<PackageUtilizationReport> GetPackageUtilizationReportAsync(PackageUtilizationReportRequest request);
}

public class PackageReportService : IPackageReportService
{
    private readonly ILWDbContext _context;
    private readonly ILogger<PackageReportService> _logger;
    private readonly IConfiguration _configuration;
    
    public PackageReportService(
        ILWDbContext context,
        ILogger<PackageReportService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task<MixedBoxGoodsReceiptReport> GetMixedBoxGoodsReceiptReportAsync(
        MixedBoxGoodsReceiptReportRequest request)
    {
        var query = _context.Packages
            .Where(p => p.CreatedAt >= request.FromDate && p.CreatedAt <= request.ToDate);
        
        if (!string.IsNullOrEmpty(request.WhsCode))
        {
            query = query.Where(p => p.WhsCode == request.WhsCode);
        }
        
        if (!string.IsNullOrEmpty(request.CreatedBy))
        {
            query = query.Where(p => p.CreatedBy == request.CreatedBy);
        }
        
        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }
        
        // Filter by packages created during goods receipt operations
        query = query.Where(p => p.CustomAttributes.Contains("\"SourceOperationType\":\"GoodsReceipt\""));
        
        var packages = await query
            .Include(p => p.Contents)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
        
        var report = new MixedBoxGoodsReceiptReport
        {
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = "System", // Could be passed as parameter
            Parameters = request,
            TotalPackages = packages.Count,
            TotalItems = packages.SelectMany(p => p.Contents).Count(),
            TotalQuantity = packages.SelectMany(p => p.Contents).Sum(c => c.Quantity),
            Packages = packages.Select(p => new MixedBoxReportItem
            {
                PackageId = p.Id,
                PackageBarcode = p.Barcode,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy,
                WhsCode = p.WhsCode,
                BinCode = p.BinCode,
                Status = p.Status.ToString(),
                TotalItems = p.Contents.Count,
                UniqueItems = p.Contents.Select(c => c.ItemCode).Distinct().Count(),
                TotalQuantity = p.Contents.Sum(c => c.Quantity),
                TotalWeight = await CalculatePackageWeightAsync(p.Contents),
                Contents = p.Contents.Select(c => new PackageContentReportItem
                {
                    ItemCode = c.ItemCode,
                    ItemName = GetItemName(c.ItemCode), // Would need item lookup
                    Quantity = c.Quantity,
                    UnitCode = c.UnitCode,
                    BatchNo = c.BatchNo,
                    SerialNo = c.SerialNo,
                    ExpiryDate = c.ExpiryDate
                }).ToList(),
                CustomAttributes = ParseCustomAttributes(p.CustomAttributes)
            }).ToList()
        };
        
        // Add summary statistics
        report.Summary = new MixedBoxReportSummary
        {
            AverageItemsPerPackage = report.TotalItems / (double)Math.Max(report.TotalPackages, 1),
            MostUsedWarehouse = packages.GroupBy(p => p.WhsCode)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key,
            PackagesByStatus = packages.GroupBy(p => p.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            PackagesByWarehouse = packages.GroupBy(p => p.WhsCode)
                .ToDictionary(g => g.Key, g => g.Count())
        };
        
        return report;
    }
    
    public async Task<PackageStockReport> GetPackageStockReportAsync(string packageBarcode)
    {
        var package = await _context.Packages
            .Include(p => p.Contents)
            .Include(p => p.LocationHistory)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Barcode == packageBarcode);
        
        if (package == null)
        {
            throw new NotFoundException($"Package {packageBarcode} not found");
        }
        
        var report = new PackageStockReport
        {
            PackageId = package.Id,
            PackageBarcode = package.Barcode,
            Status = package.Status.ToString(),
            Location = new LocationInfo
            {
                WhsCode = package.WhsCode,
                BinCode = package.BinCode,
                BinEntry = package.BinEntry
            },
            CreatedAt = package.CreatedAt,
            CreatedBy = package.CreatedBy,
            ClosedAt = package.ClosedAt,
            ClosedBy = package.ClosedBy,
            Notes = package.Notes,
            CustomAttributes = ParseCustomAttributes(package.CustomAttributes),
            Contents = package.Contents.Select(c => new PackageStockItem
            {
                ItemCode = c.ItemCode,
                ItemName = GetItemName(c.ItemCode),
                Quantity = c.Quantity,
                UnitCode = c.UnitCode,
                BatchNo = c.BatchNo,
                SerialNo = c.SerialNo,
                ExpiryDate = c.ExpiryDate,
                AddedAt = c.CreatedAt,
                AddedBy = c.CreatedBy
            }).ToList(),
            TotalItems = package.Contents.Count,
            UniqueItems = package.Contents.Select(c => c.ItemCode).Distinct().Count(),
            TotalQuantity = package.Contents.Sum(c => c.Quantity),
            LastMovement = package.LocationHistory.OrderByDescending(h => h.MovementDate).FirstOrDefault(),
            LastTransaction = package.Transactions.OrderByDescending(t => t.TransactionDate).FirstOrDefault()
        };
        
        return report;
    }
    
    public async Task<PackageMovementReport> GetPackageMovementReportAsync(Guid packageId)
    {
        var package = await _context.Packages
            .Include(p => p.LocationHistory)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == packageId);
        
        if (package == null)
        {
            throw new NotFoundException($"Package {packageId} not found");
        }
        
        var movements = package.LocationHistory.OrderBy(h => h.MovementDate).ToList();
        var transactions = package.Transactions.OrderBy(t => t.TransactionDate).ToList();
        
        var report = new PackageMovementReport
        {
            PackageId = package.Id,
            PackageBarcode = package.Barcode,
            Status = package.Status.ToString(),
            CreatedAt = package.CreatedAt,
            
            LocationMovements = movements.Select(m => new LocationMovementDto
            {
                Id = m.Id,
                MovementType = m.MovementType.ToString(),
                FromLocation = m.FromWhsCode != null ? $"{m.FromWhsCode}/{m.FromBinCode}" : null,
                ToLocation = $"{m.ToWhsCode}/{m.ToBinCode}",
                MovementDate = m.MovementDate,
                UserId = m.UserId,
                SourceOperation = m.SourceOperationType,
                Notes = m.Notes
            }).ToList(),
            
            ContentTransactions = transactions.Select(t => new ContentTransactionDto
            {
                Id = t.Id,
                TransactionType = t.TransactionType.ToString(),
                ItemCode = t.ItemCode,
                Quantity = t.Quantity,
                UnitCode = t.UnitCode,
                BatchNo = t.BatchNo,
                SerialNo = t.SerialNo,
                TransactionDate = t.TransactionDate,
                UserId = t.UserId,
                SourceOperation = t.SourceOperationType,
                Notes = t.Notes
            }).ToList(),
            
            Summary = new MovementSummaryDto
            {
                TotalLocationChanges = movements.Count,
                TotalContentTransactions = transactions.Count,
                TotalItemsAdded = transactions.Where(t => t.Quantity > 0).Sum(t => t.Quantity),
                TotalItemsRemoved = Math.Abs(transactions.Where(t => t.Quantity < 0).Sum(t => t.Quantity)),
                FirstMovement = movements.FirstOrDefault()?.MovementDate,
                LastMovement = movements.LastOrDefault()?.MovementDate,
                CurrentLocation = $"{package.WhsCode}/{package.BinCode}",
                DistinctUsers = movements.Select(m => m.UserId)
                    .Concat(transactions.Select(t => t.UserId))
                    .Distinct()
                    .ToList()
            }
        };
        
        return report;
    }
    
    public async Task<PackageInventoryReport> GetPackageInventoryReportAsync(PackageInventoryReportRequest request)
    {
        var query = _context.Packages
            .Include(p => p.Contents)
            .Where(p => p.Status == PackageStatus.Active);
        
        if (!string.IsNullOrEmpty(request.WhsCode))
        {
            query = query.Where(p => p.WhsCode == request.WhsCode);
        }
        
        if (request.BinEntry.HasValue)
        {
            query = query.Where(p => p.BinEntry == request.BinEntry.Value);
        }
        
        if (!string.IsNullOrEmpty(request.ItemCode))
        {
            query = query.Where(p => p.Contents.Any(c => c.ItemCode == request.ItemCode));
        }
        
        var packages = await query.ToListAsync();
        
        var inventoryItems = packages
            .SelectMany(p => p.Contents.Select(c => new
            {
                Package = p,
                Content = c
            }))
            .GroupBy(x => new { x.Content.ItemCode, x.Content.BatchNo, x.Package.WhsCode, x.Package.BinCode })
            .Select(g => new PackageInventoryItem
            {
                ItemCode = g.Key.ItemCode,
                ItemName = GetItemName(g.Key.ItemCode),
                BatchNo = g.Key.BatchNo,
                WhsCode = g.Key.WhsCode,
                BinCode = g.Key.BinCode,
                TotalQuantity = g.Sum(x => x.Content.Quantity),
                PackageCount = g.Count(),
                UnitCode = g.First().Content.UnitCode,
                ExpiryDate = g.Where(x => x.Content.ExpiryDate.HasValue)
                    .OrderBy(x => x.Content.ExpiryDate)
                    .FirstOrDefault()?.Content.ExpiryDate,
                Packages = g.Select(x => new PackageReference
                {
                    PackageId = x.Package.Id,
                    PackageBarcode = x.Package.Barcode,
                    Quantity = x.Content.Quantity
                }).ToList()
            })
            .OrderBy(i => i.ItemCode)
            .ThenBy(i => i.BatchNo)
            .ToList();
        
        return new PackageInventoryReport
        {
            GeneratedAt = DateTime.UtcNow,
            Parameters = request,
            TotalPackages = packages.Count,
            TotalItems = inventoryItems.Count,
            TotalQuantity = inventoryItems.Sum(i => i.TotalQuantity),
            Items = inventoryItems,
            Summary = new InventorySummaryDto
            {
                PackagesByWarehouse = packages.GroupBy(p => p.WhsCode)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ItemsByCategory = inventoryItems.GroupBy(i => GetItemCategory(i.ItemCode))
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageItemsPerPackage = packages.Any() ? 
                    packages.Average(p => p.Contents.Count) : 0
            }
        };
    }
    
    private async Task<decimal> CalculatePackageWeightAsync(IEnumerable<PackageContent> contents)
    {
        // Implementation would look up item weights and calculate total
        // For now, return 0 as this requires item master data integration
        return 0;
    }
    
    private string GetItemName(string itemCode)
    {
        // Implementation would look up item name from item master
        // For now, return the item code
        return itemCode;
    }
    
    private string GetItemCategory(string itemCode)
    {
        // Implementation would look up item category from item master
        // For now, return "General"
        return "General";
    }
    
    private Dictionary<string, object> ParseCustomAttributes(string customAttributesJson)
    {
        if (string.IsNullOrEmpty(customAttributesJson))
            return new Dictionary<string, object>();
            
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(customAttributesJson) 
                ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}
```

### 5.1.2 Package Report Controller

```csharp
[ApiController]
[Route("api/package/reports")]
[Authorize]
public class PackageReportController : ControllerBase
{
    private readonly IPackageReportService _reportService;
    private readonly ILogger<PackageReportController> _logger;
    
    public PackageReportController(
        IPackageReportService reportService,
        ILogger<PackageReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }
    
    [HttpGet("mixed-boxes")]
    public async Task<ActionResult<MixedBoxGoodsReceiptReport>> GetMixedBoxReport(
        [FromQuery] MixedBoxGoodsReceiptReportRequest request)
    {
        try
        {
            var report = await _reportService.GetMixedBoxGoodsReceiptReportAsync(request);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating mixed box report");
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("stock/{barcode}")]
    public async Task<ActionResult<PackageStockReport>> GetPackageStock(string barcode)
    {
        try
        {
            var report = await _reportService.GetPackageStockReportAsync(barcode);
            return Ok(report);
        }
        catch (NotFoundException)
        {
            return NotFound(new { error = $"Package {barcode} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating package stock report for {Barcode}", barcode);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("movements/{packageId}")]
    public async Task<ActionResult<PackageMovementReport>> GetPackageMovements(Guid packageId)
    {
        try
        {
            var report = await _reportService.GetPackageMovementReportAsync(packageId);
            return Ok(report);
        }
        catch (NotFoundException)
        {
            return NotFound(new { error = $"Package {packageId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating package movement report for {PackageId}", packageId);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("inventory")]
    public async Task<ActionResult<PackageInventoryReport>> GetPackageInventory(
        [FromQuery] PackageInventoryReportRequest request)
    {
        try
        {
            var report = await _reportService.GetPackageInventoryReportAsync(request);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating package inventory report");
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("export/mixed-boxes")]
    public async Task<IActionResult> ExportMixedBoxReport(
        [FromQuery] MixedBoxGoodsReceiptReportRequest request,
        [FromQuery] string format = "excel")
    {
        try
        {
            var report = await _reportService.GetMixedBoxGoodsReceiptReportAsync(request);
            
            switch (format.ToLower())
            {
                case "excel":
                    var excelData = GenerateExcelReport(report);
                    return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"MixedBoxReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                
                case "csv":
                    var csvData = GenerateCsvReport(report);
                    return File(Encoding.UTF8.GetBytes(csvData), "text/csv", 
                        $"MixedBoxReport_{DateTime.Now:yyyyMMddHHmmss}.csv");
                
                default:
                    return BadRequest(new { error = "Unsupported format. Use 'excel' or 'csv'" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting mixed box report");
            return BadRequest(new { error = ex.Message });
        }
    }
    
    private byte[] GenerateExcelReport(MixedBoxGoodsReceiptReport report)
    {
        // Implementation would use a library like EPPlus or ClosedXML
        // to generate Excel files
        throw new NotImplementedException("Excel export not implemented");
    }
    
    private string GenerateCsvReport(MixedBoxGoodsReceiptReport report)
    {
        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Package Barcode,Created Date,Created By,Warehouse,Bin,Status,Total Items,Total Quantity,Item Code,Item Quantity,Unit,Batch,Serial");
        
        // Data rows
        foreach (var package in report.Packages)
        {
            foreach (var content in package.Contents)
            {
                csv.AppendLine($"{package.PackageBarcode},{package.CreatedAt:yyyy-MM-dd HH:mm},{package.CreatedBy}," +
                             $"{package.WhsCode},{package.BinCode},{package.Status},{package.TotalItems}," +
                             $"{package.TotalQuantity},{content.ItemCode},{content.Quantity},{content.UnitCode}," +
                             $"{content.BatchNo},{content.SerialNo}");
            }
        }
        
        return csv.ToString();
    }
}
```

## 5.2 Label Generation System

### 5.2.1 Package Label Service

```csharp
public interface IPackageLabelService
{
    Task<byte[]> GenerateLabelAsync(Guid packageId);
    Task<LabelTemplate> GetLabelTemplateAsync();
    Task<bool> PrintLabelAsync(Guid packageId);
    Task<LabelPreviewDto> GenerateLabelPreviewAsync(Guid packageId);
    Task<byte[]> GenerateMultipleLabelsAsync(IEnumerable<Guid> packageIds);
}

public class PackageLabelService : IPackageLabelService
{
    private readonly IPackageService _packageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PackageLabelService> _logger;
    private readonly IPrintService _printService;
    
    public PackageLabelService(
        IPackageService packageService,
        IConfiguration configuration,
        ILogger<PackageLabelService> logger,
        IPrintService printService)
    {
        _packageService = packageService;
        _configuration = configuration;
        _logger = logger;
        _printService = printService;
    }
    
    public async Task<byte[]> GenerateLabelAsync(Guid packageId)
    {
        var package = await _packageService.GetPackageAsync(packageId);
        if (package == null)
        {
            throw new NotFoundException($"Package {packageId} not found");
        }
        
        var template = await GetLabelTemplateAsync();
        var contents = await _packageService.GetPackageContentsAsync(packageId);
        
        var labelData = new PackageLabelData
        {
            PackageBarcode = package.Barcode,
            CreatedDate = package.CreatedAt,
            CreatedBy = package.CreatedBy,
            Location = $"{package.WhsCode}{(string.IsNullOrEmpty(package.BinCode) ? "" : "/" + package.BinCode)}",
            Status = package.Status.ToString(),
            ContentsSummary = GenerateContentsSummary(contents, template.MaxContentLines),
            CustomFields = GetCustomFieldValues(package, template.CustomFields),
            TotalItems = contents.Count(),
            TotalQuantity = contents.Sum(c => c.Quantity)
        };
        
        return await GenerateLabelImageAsync(labelData, template);
    }
    
    public async Task<LabelTemplate> GetLabelTemplateAsync()
    {
        var config = _configuration.GetSection("Package:Label");
        
        return new LabelTemplate
        {
            Width = config.GetValue<int>("Width", 400),
            Height = config.GetValue<int>("Height", 600),
            BarcodeType = config.GetValue<string>("BarcodeType", "QR"),
            ShowBarcode = config.GetValue<bool>("ShowBarcode", true),
            BarcodeWidth = config.GetValue<int>("BarcodeWidth", 150),
            BarcodeHeight = config.GetValue<int>("BarcodeHeight", 150),
            Columns = config.GetValue<int>("Columns", 2),
            MaxContentLines = config.GetValue<int>("MaxContentLines", 10),
            CustomFields = config.GetSection("CustomFields").Get<List<LabelCustomField>>() ?? new List<LabelCustomField>(),
            FontSize = config.GetValue<int>("FontSize", 10),
            HeaderFontSize = config.GetValue<int>("HeaderFontSize", 12),
            IncludeCreatedDate = config.GetValue<bool>("IncludeCreatedDate", true),
            IncludeLocation = config.GetValue<bool>("IncludeLocation", true),
            IncludeStatus = config.GetValue<bool>("IncludeStatus", true),
            IncludeContents = config.GetValue<bool>("IncludeContents", true),
            LogoPath = config.GetValue<string>("LogoPath"),
            BorderWidth = config.GetValue<int>("BorderWidth", 2),
            Margin = config.GetValue<int>("Margin", 10)
        };
    }
    
    public async Task<bool> PrintLabelAsync(Guid packageId)
    {
        try
        {
            var labelData = await GenerateLabelAsync(packageId);
            var package = await _packageService.GetPackageAsync(packageId);
            
            var printRequest = new PrintRequest
            {
                Data = labelData,
                PrinterName = GetConfiguredPrinter(),
                JobName = $"Package_Label_{package.Barcode}",
                Copies = 1
            };
            
            var success = await _printService.PrintAsync(printRequest);
            
            if (success)
            {
                _logger.LogInformation("Label printed successfully for package {PackageBarcode}", package.Barcode);
            }
            else
            {
                _logger.LogWarning("Failed to print label for package {PackageBarcode}", package.Barcode);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing label for package {PackageId}", packageId);
            return false;
        }
    }
    
    private async Task<byte[]> GenerateLabelImageAsync(PackageLabelData data, LabelTemplate template)
    {
        using var bitmap = new SKBitmap(template.Width, template.Height);
        using var canvas = new SKCanvas(bitmap);
        
        // Background
        canvas.Clear(SKColors.White);
        
        // Draw border
        if (template.BorderWidth > 0)
        {
            var borderPaint = new SKPaint
            {
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = template.BorderWidth
            };
            
            canvas.DrawRect(new SKRect(0, 0, template.Width, template.Height), borderPaint);
        }
        
        var yPosition = template.Margin;
        var contentWidth = template.Width - (template.Margin * 2);
        
        // Draw logo if configured
        if (!string.IsNullOrEmpty(template.LogoPath) && File.Exists(template.LogoPath))
        {
            var logo = SKBitmap.Decode(template.LogoPath);
            var logoHeight = 40;
            var logoWidth = (int)(logo.Width * (logoHeight / (double)logo.Height));
            
            canvas.DrawBitmap(logo, new SKRect(template.Margin, yPosition, template.Margin + logoWidth, yPosition + logoHeight));
            yPosition += logoHeight + 10;
        }
        
        // Header text paint
        var headerPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = template.HeaderFontSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        
        // Regular text paint
        var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = template.FontSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };
        
        // Package header
        canvas.DrawText("PACKAGE LABEL", template.Margin, yPosition + template.HeaderFontSize, headerPaint);
        yPosition += template.HeaderFontSize + 15;
        
        // Barcode
        if (template.ShowBarcode)
        {
            var barcodeImage = GenerateBarcodeImage(data.PackageBarcode, template.BarcodeType, 
                template.BarcodeWidth, template.BarcodeHeight);
            
            var barcodeX = (template.Width - template.BarcodeWidth) / 2;
            canvas.DrawBitmap(barcodeImage, new SKRect(barcodeX, yPosition, 
                barcodeX + template.BarcodeWidth, yPosition + template.BarcodeHeight));
            yPosition += template.BarcodeHeight + 10;
        }
        
        // Barcode text (centered)
        var barcodeTextWidth = textPaint.MeasureText(data.PackageBarcode);
        var barcodeTextX = (template.Width - barcodeTextWidth) / 2;
        canvas.DrawText(data.PackageBarcode, barcodeTextX, yPosition + template.FontSize, textPaint);
        yPosition += template.FontSize + 15;
        
        // Package information
        if (template.IncludeCreatedDate)
        {
            canvas.DrawText($"Created: {data.CreatedDate:yyyy-MM-dd HH:mm}", template.Margin, yPosition + template.FontSize, textPaint);
            yPosition += template.FontSize + 5;
        }
        
        if (!string.IsNullOrEmpty(data.CreatedBy))
        {
            canvas.DrawText($"By: {data.CreatedBy}", template.Margin, yPosition + template.FontSize, textPaint);
            yPosition += template.FontSize + 5;
        }
        
        if (template.IncludeLocation)
        {
            canvas.DrawText($"Location: {data.Location}", template.Margin, yPosition + template.FontSize, textPaint);
            yPosition += template.FontSize + 5;
        }
        
        if (template.IncludeStatus)
        {
            canvas.DrawText($"Status: {data.Status}", template.Margin, yPosition + template.FontSize, textPaint);
            yPosition += template.FontSize + 5;
        }
        
        // Summary information
        canvas.DrawText($"Items: {data.TotalItems}", template.Margin, yPosition + template.FontSize, textPaint);
        canvas.DrawText($"Qty: {data.TotalQuantity:N2}", template.Margin + 120, yPosition + template.FontSize, textPaint);
        yPosition += template.FontSize + 10;
        
        // Contents (if enabled and space available)
        if (template.IncludeContents && data.ContentsSummary.Any() && yPosition < template.Height - 100)
        {
            canvas.DrawText("CONTENTS:", template.Margin, yPosition + template.FontSize, headerPaint);
            yPosition += template.FontSize + 5;
            
            DrawContentsInColumns(canvas, data.ContentsSummary, template, yPosition, contentWidth);
        }
        
        // Custom fields
        if (data.CustomFields.Any())
        {
            var customFieldsY = template.Height - 60; // Position near bottom
            foreach (var field in data.CustomFields.Where(f => !string.IsNullOrEmpty(f.Value)))
            {
                canvas.DrawText($"{field.DisplayName}: {field.Value}", template.Margin, customFieldsY, textPaint);
                customFieldsY += template.FontSize + 3;
                
                if (customFieldsY > template.Height - template.Margin) break;
            }
        }
        
        // Convert to byte array
        using var image = SKImage.FromBitmap(bitmap);
        using var encodedData = image.Encode(SKEncodedImageFormat.Png, 100);
        return encodedData.ToArray();
    }
    
    private void DrawContentsInColumns(SKCanvas canvas, List<ContentSummaryItem> contents, 
        LabelTemplate template, int startY, int contentWidth)
    {
        var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = template.FontSize - 1,
            IsAntialias = true
        };
        
        var columnWidth = contentWidth / template.Columns;
        var currentColumn = 0;
        var currentY = startY;
        var lineHeight = template.FontSize + 2;
        
        foreach (var item in contents.Take(template.MaxContentLines))
        {
            var x = template.Margin + (currentColumn * columnWidth);
            var text = $"{item.ItemCode}: {item.Quantity:N1}";
            
            // Truncate if text is too long
            if (textPaint.MeasureText(text) > columnWidth - 5)
            {
                while (textPaint.MeasureText(text + "...") > columnWidth - 5 && text.Length > 10)
                {
                    text = text.Substring(0, text.Length - 1);
                }
                text += "...";
            }
            
            canvas.DrawText(text, x, currentY + template.FontSize, textPaint);
            
            currentColumn++;
            if (currentColumn >= template.Columns)
            {
                currentColumn = 0;
                currentY += lineHeight;
            }
        }
    }
    
    private SKBitmap GenerateBarcodeImage(string data, string barcodeType, int width, int height)
    {
        // Implementation would use a barcode library like ZXing.Net
        // For now, return a placeholder
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        
        canvas.Clear(SKColors.White);
        
        // Draw placeholder barcode pattern
        var paint = new SKPaint { Color = SKColors.Black };
        for (int i = 0; i < width; i += 4)
        {
            if (i % 8 < 4)
            {
                canvas.DrawLine(i, 0, i, height, paint);
            }
        }
        
        return bitmap;
    }
    
    private List<ContentSummaryItem> GenerateContentsSummary(IEnumerable<PackageContent> contents, int maxLines)
    {
        return contents
            .GroupBy(c => c.ItemCode)
            .Select(g => new ContentSummaryItem
            {
                ItemCode = g.Key,
                Quantity = g.Sum(c => c.Quantity),
                UnitCode = g.First().UnitCode
            })
            .OrderBy(i => i.ItemCode)
            .Take(maxLines)
            .ToList();
    }
    
    private List<CustomFieldValue> GetCustomFieldValues(Package package, List<LabelCustomField> customFields)
    {
        var attributes = ParseCustomAttributes(package.CustomAttributes);
        
        return customFields
            .Where(f => f.Enabled)
            .OrderBy(f => f.Order)
            .Select(f => new CustomFieldValue
            {
                Name = f.Name,
                DisplayName = f.DisplayName,
                Value = GetCustomFieldValue(f.Name, package, attributes)
            })
            .ToList();
    }
    
    private string GetCustomFieldValue(string fieldName, Package package, Dictionary<string, object> attributes)
    {
        return fieldName.ToLower() switch
        {
            "createdby" => package.CreatedBy,
            "createdat" => package.CreatedAt.ToString("yyyy-MM-dd"),
            "status" => package.Status.ToString(),
            "notes" => package.Notes,
            _ => attributes.ContainsKey(fieldName) ? attributes[fieldName]?.ToString() : ""
        };
    }
    
    private Dictionary<string, object> ParseCustomAttributes(string customAttributesJson)
    {
        if (string.IsNullOrEmpty(customAttributesJson))
            return new Dictionary<string, object>();
            
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(customAttributesJson) 
                ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
    
    private string GetConfiguredPrinter()
    {
        return _configuration.GetValue<string>("Package:Label:PrinterName", "Default");
    }
}
```

### 5.2.2 Label Controller

```csharp
[ApiController]
[Route("api/package/labels")]
[Authorize]
public class PackageLabelController : ControllerBase
{
    private readonly IPackageLabelService _labelService;
    private readonly ILogger<PackageLabelController> _logger;
    
    public PackageLabelController(
        IPackageLabelService labelService,
        ILogger<PackageLabelController> logger)
    {
        _labelService = labelService;
        _logger = logger;
    }
    
    [HttpGet("{packageId}")]
    public async Task<IActionResult> GenerateLabel(Guid packageId)
    {
        try
        {
            var labelData = await _labelService.GenerateLabelAsync(packageId);
            return File(labelData, "image/png", $"package_label_{packageId}.png");
        }
        catch (NotFoundException)
        {
            return NotFound(new { error = $"Package {packageId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating label for package {PackageId}", packageId);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("{packageId}/print")]
    public async Task<ActionResult> PrintLabel(Guid packageId)
    {
        try
        {
            var success = await _labelService.PrintLabelAsync(packageId);
            if (success)
            {
                return Ok(new { message = "Label printed successfully" });
            }
            else
            {
                return BadRequest(new { error = "Failed to print label" });
            }
        }
        catch (NotFoundException)
        {
            return NotFound(new { error = $"Package {packageId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing label for package {PackageId}", packageId);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("{packageId}/preview")]
    public async Task<ActionResult<LabelPreviewDto>> GetLabelPreview(Guid packageId)
    {
        try
        {
            var preview = await _labelService.GenerateLabelPreviewAsync(packageId);
            return Ok(preview);
        }
        catch (NotFoundException)
        {
            return NotFound(new { error = $"Package {packageId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating label preview for package {PackageId}", packageId);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("batch")]
    public async Task<IActionResult> GenerateMultipleLabels([FromBody] BatchLabelRequest request)
    {
        try
        {
            var labelData = await _labelService.GenerateMultipleLabelsAsync(request.PackageIds);
            return File(labelData, "application/pdf", $"package_labels_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating batch labels");
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("template")]
    public async Task<ActionResult<LabelTemplate>> GetLabelTemplate()
    {
        try
        {
            var template = await _labelService.GetLabelTemplateAsync();
            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting label template");
            return BadRequest(new { error = ex.Message });
        }
    }
}
```

## 5.3 Data Models

### 5.3.1 Report Models

```csharp
public class MixedBoxGoodsReceiptReportRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string WhsCode { get; set; }
    public string CreatedBy { get; set; }
    public PackageStatus? Status { get; set; }
}

public class MixedBoxGoodsReceiptReport
{
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; }
    public MixedBoxGoodsReceiptReportRequest Parameters { get; set; }
    public int TotalPackages { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalQuantity { get; set; }
    public List<MixedBoxReportItem> Packages { get; set; } = new();
    public MixedBoxReportSummary Summary { get; set; }
}

public class MixedBoxReportItem
{
    public Guid PackageId { get; set; }
    public string PackageBarcode { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string WhsCode { get; set; }
    public string BinCode { get; set; }
    public string Status { get; set; }
    public int TotalItems { get; set; }
    public int UniqueItems { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalWeight { get; set; }
    public List<PackageContentReportItem> Contents { get; set; } = new();
    public Dictionary<string, object> CustomAttributes { get; set; } = new();
}

public class PackageStockReport
{
    public Guid PackageId { get; set; }
    public string PackageBarcode { get; set; }
    public string Status { get; set; }
    public LocationInfo Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string ClosedBy { get; set; }
    public string Notes { get; set; }
    public Dictionary<string, object> CustomAttributes { get; set; } = new();
    public List<PackageStockItem> Contents { get; set; } = new();
    public int TotalItems { get; set; }
    public int UniqueItems { get; set; }
    public decimal TotalQuantity { get; set; }
    public PackageLocationHistory LastMovement { get; set; }
    public PackageTransaction LastTransaction { get; set; }
}
```

### 5.3.2 Label Models

```csharp
public class LabelTemplate
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string BarcodeType { get; set; }
    public bool ShowBarcode { get; set; }
    public int BarcodeWidth { get; set; }
    public int BarcodeHeight { get; set; }
    public int Columns { get; set; }
    public int MaxContentLines { get; set; }
    public List<LabelCustomField> CustomFields { get; set; } = new();
    public int FontSize { get; set; }
    public int HeaderFontSize { get; set; }
    public bool IncludeCreatedDate { get; set; }
    public bool IncludeLocation { get; set; }
    public bool IncludeStatus { get; set; }
    public bool IncludeContents { get; set; }
    public string LogoPath { get; set; }
    public int BorderWidth { get; set; }
    public int Margin { get; set; }
}

public class LabelCustomField
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public bool Enabled { get; set; }
    public int Order { get; set; }
}

public class PackageLabelData
{
    public string PackageBarcode { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public string Location { get; set; }
    public string Status { get; set; }
    public List<ContentSummaryItem> ContentsSummary { get; set; } = new();
    public List<CustomFieldValue> CustomFields { get; set; } = new();
    public int TotalItems { get; set; }
    public decimal TotalQuantity { get; set; }
}

public class ContentSummaryItem
{
    public string ItemCode { get; set; }
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; }
}

public class CustomFieldValue
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Value { get; set; }
}

public class BatchLabelRequest
{
    public List<Guid> PackageIds { get; set; } = new();
}
```

## Implementation Notes

### Timeline: Week 10-11
- Comprehensive package reporting system
- Configurable label generation with multiple formats
- Export capabilities (Excel, CSV, PDF)
- Print integration with existing printer infrastructure

### Key Features
- **Multiple report types**: Mixed box, stock, movement, inventory reports
- **Flexible label design**: Configurable templates with custom fields
- **Export options**: Multiple formats for data analysis
- **Print integration**: Direct printing with job tracking
- **Batch operations**: Multiple labels and reports
- **Performance optimized**: Efficient queries for large datasets

### Report Types
1. **Mixed Box Goods Receipt Report**: Track packages created during receiving
2. **Package Stock Report**: Current contents and status of specific package
3. **Package Movement Report**: Complete audit trail of package history
4. **Package Inventory Report**: Current inventory grouped by packages

### Label Features
- QR code/barcode generation with configurable format
- Customer logo integration
- Custom fields from package attributes
- Responsive layout with columns
- Border and margin controls
- Font size and style options

### Next Steps
- Phase 6: Configuration system and final integration
- Advanced label templates and design options
- Integration with external reporting tools