

namespace Cashflow.Integration.Tests.Entities
{
    public class BalanceTotals
    {
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class BalanceResponse
    {
        public DateTime Date { get; set; }
        public BalanceTotals Totals { get; set; }
    }
}
