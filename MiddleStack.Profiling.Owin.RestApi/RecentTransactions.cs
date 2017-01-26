using System.Collections.Generic;

namespace MiddleStack.Profiling.Owin.RestApi
{
    internal class RecentTransactions
    {
        public IList<TransactionSnapshot> Transactions { get; set; }
    }
}