

namespace Cashflow.Integration.Tests.Entities
{
    public class BalanceTotals
    {
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class BalanceResponse
    {
        public string Date { get; set; }
        public required BalanceTotals Totals { get; set; }
    }
}
