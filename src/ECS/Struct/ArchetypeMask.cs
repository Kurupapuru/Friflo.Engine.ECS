﻿using System.Runtime.Intrinsics;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

public readonly struct ArchetypeMask
{
    private readonly   Vector256<long>[]   masks;

    public override string ToString() => GetString();

    internal ArchetypeMask(StructHeap[] heaps, StructHeap newComp) {
        long l0 = 0, l1 = 0, l2 = 0, l3 = 0;
        if (newComp != null) {
            SetBit(newComp.structIndex, ref l0, ref l1, ref l2, ref l3);
        }
        foreach (var heap in heaps) {
            SetBit(heap.structIndex, ref l0, ref l1, ref l2, ref l3);
        }
        masks = new [] { Vector256.Create(l0, l1, l2, l3) };
    }
    
    public ArchetypeMask(int[] bits) {
        long l0 = 0, l1 = 0, l2 = 0, l3 = 0;
        foreach (var bit in bits) {
            SetBit(bit, ref l0, ref l1, ref l2, ref l3);
        }
        masks = new [] { Vector256.Create(l0, l1, l2, l3) };
    }
    
    private static void SetBit(int bit, ref long l0, ref long l1, ref long l2, ref long l3)
    {
        switch (bit) {
            case < 64:  l0 = 1L <<  bit;         return;
            case < 128: l1 = 1L << (bit - 64);   return;
            case < 192: l2 = 1L << (bit - 128);  return;
            default:    l3 = 1L << (bit - 192);  return;
        }
    }
    
    private string GetString() {
        var sb = new StringBuilder();
        foreach (var mask in masks) {
            sb.Append(mask.ToString());
        }
        return sb.ToString();
    }
}