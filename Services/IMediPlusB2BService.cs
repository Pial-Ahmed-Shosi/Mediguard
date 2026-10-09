using System;
using System.Threading.Tasks;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public interface IMediPlusB2BService
    {
        Task<(bool Success, string Message, Guid? OrderId)> CreateB2BOrderAsync(B2BOrderViewModel model);
    }
}