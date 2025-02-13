using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobMatching.Domain.Entities;
using JobMatching.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<PaymentRecord>> GetPaymentsByUserAsync(string userId)
    {
        return await _context.PaymentRecords.Where(p => p.UserId == userId).ToListAsync();
    }

    public async Task<bool> SavePaymentAsync(PaymentRecord payment)
    {
        _context.PaymentRecords.Add(payment);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdatePaymentStatusAsync(string transactionId, string status)
    {
        var payment = await _context.PaymentRecords.FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        if (payment == null) return false;

        payment.Status = status;
        return await _context.SaveChangesAsync() > 0;
    }

    // Check if Payment Already Exists (Prevents Duplicate Entries)
    public async Task<bool> PaymentExistsAsync(string transactionId)
    {
        return await _context.PaymentRecords.AnyAsync(p => p.TransactionId == transactionId);
    }

    // 🔥 Get Latest Payment for a User
    public async Task<PaymentRecord?> GetLatestPaymentByUserId(string userId)
    {
        return await _context.PaymentRecords
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.PaymentDate)
            .FirstOrDefaultAsync();
    }

    public async Task<PaymentRecord?> GetPaymentByTransactionId(string transactionId)
    {
        return await _context.PaymentRecords.FirstOrDefaultAsync(p => p.TransactionId == transactionId);
    }

    public async Task<List<PaymentRecord>> GetAllPaymentsAsync()
    {
        return await _context.PaymentRecords.OrderByDescending(p => p.PaymentDate).ToListAsync();
    }
}
