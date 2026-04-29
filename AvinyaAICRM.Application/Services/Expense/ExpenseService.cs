using AvinyaAICRM.Application.DTOs.Expense;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Expenses;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Expense;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace AvinyaAICRM.Application.Services.Expense
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _repository;
        private readonly IConfiguration _configuration;

        public ExpenseService(IExpenseRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }

        public async Task<ResponseModel> GetFilteredAsync(
            string? search,
            int page,
            int pageSize,
            string tenantId,
            string? status,
            DateTime? from,
            DateTime? to)
        {
            var result = await _repository.GetFilteredAsync(
                search,
                page,
                pageSize,
                tenantId,
                status,
                from,
                to);

            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> GetByIdAsync(Guid id)
        {
            var expense = await _repository.GetByIdAsync(id);
            if (expense == null)
                return CommonHelper.BadRequestResponseMessage("Expense not found");

            return CommonHelper.GetResponseMessage(expense);
        }

        public async Task<ResponseModel> CreateAsync(CreateExpenseDto dto, string tenantId, Guid userId)
        {
            string? receiptPath = null;

            if (dto.ReceiptFile != null && dto.ReceiptFile.Length > 0)
            {
                var fileResult = await SaveReceiptFileAsync(dto.ReceiptFile, tenantId);
                if (fileResult.IsError)
                    return CommonHelper.BadRequestResponseMessage(fileResult.ErrorMessage!);

                receiptPath = fileResult.Path;
            }

            var expense = new AvinyaAICRM.Domain.Entities.Expenses.Expense
            {
                ExpenseId = Guid.NewGuid(),
                TenantId = Guid.Parse(tenantId),
                ExpenseDate = dto.ExpenseDate,
                CategoryId = dto.CategoryId,
                Amount = dto.Amount,
                PaymentMode = dto.PaymentMode,
                Description = dto.Description,
                ReceiptPath = receiptPath,
                Status = dto.Status ?? "Unpaid",
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };

            try
            {
                var success = await _repository.CreateAsync(expense);
                if (!success)
                    return CommonHelper.BadRequestResponseMessage("Failed to create expense");

                return CommonHelper.GetResponseMessage(expense);
            }
            catch (Exception ex)
            {
                return CommonHelper.BadRequestResponseMessage("Failed to create expense: " + ex.Message + (ex.InnerException != null ? " -> " + ex.InnerException.Message : ""));
            }
        }

        public async Task<ResponseModel> UpdateAsync(UpdateExpenseDto dto, Guid userId)
        {
            var expense = await _repository.GetByIdAsync(dto.ExpenseId);
            if (expense == null)
                return CommonHelper.BadRequestResponseMessage("Expense not found");

            // New file uploaded — replace old one
            if (dto.ReceiptFile != null && dto.ReceiptFile.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(expense.ReceiptPath))
                    DeleteReceiptFile(expense.ReceiptPath);

                var fileResult = await SaveReceiptFileAsync(dto.ReceiptFile, expense.TenantId.ToString());
                if (fileResult.IsError)
                    return CommonHelper.BadRequestResponseMessage(fileResult.ErrorMessage!);

                expense.ReceiptPath = fileResult.Path;
            }
            else if (dto.ReceiptFile == null)
            {
                // User explicitly removed the file
                if (!string.IsNullOrWhiteSpace(expense.ReceiptPath))
                    DeleteReceiptFile(expense.ReceiptPath);

                expense.ReceiptPath = null;
            }
            // else: no change to receipt — keep existing path

            expense.ExpenseDate = dto.ExpenseDate;
            expense.CategoryId = dto.CategoryId;
            expense.Amount = dto.Amount;
            expense.PaymentMode = dto.PaymentMode;
            expense.Description = dto.Description;
            expense.ModifiedBy = userId;
            expense.ModifiedDate = DateTime.Now;

            var success = await _repository.UpdateAsync(expense);
            if (!success)
                return CommonHelper.BadRequestResponseMessage("Failed to update expense");

            return CommonHelper.GetResponseMessage(expense);
        }

        public async Task<ResponseModel> DeleteAsync(Guid id)
        {
            var expense = await _repository.GetByIdAsync(id);
            if (expense == null)
                return CommonHelper.BadRequestResponseMessage("Expense not found");

            if (!string.IsNullOrWhiteSpace(expense.ReceiptPath))
                DeleteReceiptFile(expense.ReceiptPath);

            expense.IsDeleted = true;

            var success = await _repository.UpdateAsync(expense);
            if (!success)
                return CommonHelper.BadRequestResponseMessage("Failed to delete expense");

            return CommonHelper.SuccessResponseMessage("Expense deleted successfully", null);
        }

        // ── Private Helpers ────────────────────────────────────────────────────────

        private async Task<FileUploadResult> SaveReceiptFileAsync(IFormFile file, string tenantId)
        {
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "application/pdf" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return FileUploadResult.Fail("Only JPG, PNG, or PDF files are allowed");

            const long maxBytes = 5 * 1024 * 1024;
            if (file.Length > maxBytes)
                return FileUploadResult.Fail("File size must be under 5 MB");

            // Read base path from appsettings.json → "FileStorage:UploadsBasePath"
            // Falls back to {CurrentDirectory}/wwwroot if not configured
            var basePath = _configuration["FileStorage:UploadsBasePath"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var uploadsFolder = Path.Combine(basePath, "uploads", "expenses", tenantId);

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            return FileUploadResult.Ok($"/uploads/expenses/{tenantId}/{fileName}");
        }

        private void DeleteReceiptFile(string relativePath)
        {
            try
            {
                var basePath = _configuration["FileStorage:UploadsBasePath"]
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var fullPath = Path.Combine(basePath, relativePath.TrimStart('/'));

                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch
            {
                // Swallow — file deletion failure should not block main operation
            }
        }

        private class FileUploadResult
        {
            public bool IsError { get; private set; }
            public string? ErrorMessage { get; private set; }
            public string? Path { get; private set; }

            public static FileUploadResult Ok(string path) => new() { Path = path };
            public static FileUploadResult Fail(string msg) => new() { IsError = true, ErrorMessage = msg };
        }
    }
}
