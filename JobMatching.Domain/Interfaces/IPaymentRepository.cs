using System.Collections.Generic;
using System.Threading.Tasks;
using JobMatching.Domain.Entities;

namespace JobMatching.Domain.Interfaces;
public interface IPaymentRepository
{
    Task<List<PaymentRecord>> GetPaymentsByUserAsync(string userId);
    Task<PaymentRecord?> GetPaymentByTransactionId(string transactionId);
    Task<bool> SavePaymentAsync(PaymentRecord payment);
    Task<bool> UpdatePaymentStatusAsync(string transactionId, string status);
    Task<bool> PaymentExistsAsync(string transactionId);
    Task<PaymentRecord?> GetLatestPaymentByUserId(string userId);
    Task<List<PaymentRecord>> GetAllPaymentsAsync();
    
}
