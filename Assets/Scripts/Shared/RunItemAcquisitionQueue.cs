using System.Collections.Generic;
using UnityEngine;

/// <summary>Display receipts only. Acknowledging a receipt never grants an item again.</summary>
public sealed class RunItemAcquisitionReceipt
{
    public Object Item { get; }
    public int Amount { get; }
    public bool Acknowledged { get; internal set; }

    internal RunItemAcquisitionReceipt(Object item, int amount) { Item = item; Amount = amount; }
}

public sealed class RunItemAcquisitionBatch
{
    internal readonly List<RunItemAcquisitionReceipt> receipts = new List<RunItemAcquisitionReceipt>();
    public Object Item { get; internal set; }
    public int Amount { get; internal set; }
}

public sealed class RunItemAcquisitionQueue
{
    private readonly List<RunItemAcquisitionReceipt> receipts = new List<RunItemAcquisitionReceipt>();

    public int PendingCount
    {
        get { Prune(); return receipts.Count; }
    }

    public void Record(Object item, int amount)
    {
        if (item == null || amount <= 0 || !(item is Relic_SO || item is CraftingBlueprint_SO)) return;
        receipts.Add(new RunItemAcquisitionReceipt(item, amount));
    }

    public bool TryGetNext(out RunItemAcquisitionBatch batch)
    {
        Prune();
        batch = null;
        if (receipts.Count == 0) return false;
        batch = new RunItemAcquisitionBatch { Item = receipts[0].Item };
        long amount = 0;
        foreach (RunItemAcquisitionReceipt receipt in receipts)
        {
            if (receipt.Item != batch.Item) break;
            batch.receipts.Add(receipt);
            amount += receipt.Amount;
        }
        batch.Amount = (int)System.Math.Min(int.MaxValue, amount);
        return true;
    }

    public int CountAfter(RunItemAcquisitionBatch current)
    {
        Prune();
        int count = 0;
        Object previous = null;
        foreach (RunItemAcquisitionReceipt receipt in receipts)
        {
            if (current != null && current.receipts.Contains(receipt)) continue;
            if (previous != receipt.Item) count++;
            previous = receipt.Item;
        }
        return count;
    }

    public void Acknowledge(RunItemAcquisitionBatch batch)
    {
        if (batch == null) return;
        foreach (RunItemAcquisitionReceipt receipt in batch.receipts) receipt.Acknowledged = true;
        Prune();
    }

    public RunItemAcquisitionQueue Clone()
    {
        Prune();
        var clone = new RunItemAcquisitionQueue();
        // Snapshot copies share acknowledgements, but newly granted items stay in their own queue.
        clone.receipts.AddRange(receipts);
        return clone;
    }

    private void Prune() => receipts.RemoveAll(receipt => receipt.Acknowledged || receipt.Item == null);
}
