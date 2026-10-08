using System;

namespace MediGuard.Exceptions
{
    public class InsufficientStockException : Exception
    {
        public InsufficientStockException(string message)
            : base(message)
        {
        }

        public InsufficientStockException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}