﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    internal class TransactionFinishEvent: ProfilerEventBase, ITransactionFinishEvent
    {
        public TransactionFinishEvent(Transaction transaction, int version) : base(transaction, version)
        {
            Duration = transaction.Duration;
            IsSuccess = transaction.State == TransactionState.Success;
            CorrelationId = transaction.CorrelationId;
            Result = transaction.Result;
        }

        public TimeSpan Duration { get; }
        public bool IsSuccess { get; }
        public object Result { get; set; }
        public string CorrelationId { get; }
        public override ProfilerEventType Type => ProfilerEventType.TransactionFinish;
    }
}
