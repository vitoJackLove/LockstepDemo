using System;
using System.Collections.Generic;

/// <summary>
/// 最小堆，用于A*寻路的开放列表优化
/// </summary>
public class MinHeap<T> where T : IComparable<T>
{
    private List<T> _items;
    
    public int Count => _items.Count;
    
    public bool IsEmpty => _items.Count == 0;
    
    public MinHeap(int capacity = 16)
    {
        _items = new List<T>(capacity);
    }
    
    /// <summary>
    /// 添加元素
    /// </summary>
    public void Add(T item)
    {
        _items.Add(item);
        int index = _items.Count - 1;
        HeapifyUp(index);
    }
    
    /// <summary>
    /// 移除并返回最小元素
    /// </summary>
    public T RemoveMin()
    {
        if (_items.Count == 0)
        {
            throw new InvalidOperationException("堆为空");
        }
        
        T min = _items[0];
        int lastIndex = _items.Count - 1;
        _items[0] = _items[lastIndex];
        _items.RemoveAt(lastIndex);
        
        if (_items.Count > 0)
        {
            HeapifyDown(0);
        }
        
        return min;
    }
    
    /// <summary>
    /// 查看最小元素（不移除）
    /// </summary>
    public T Peek()
    {
        if (_items.Count == 0)
        {
            throw new InvalidOperationException("堆为空");
        }
        return _items[0];
    }
    
    /// <summary>
    /// 移除指定元素
    /// </summary>
    public bool Remove(T item)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (ReferenceEquals(_items[i], item))
            {
                // 将最后一个元素移到当前位置
                int lastIndex = _items.Count - 1;
                _items[i] = _items[lastIndex];
                _items.RemoveAt(lastIndex);
                
                // 如果移除的不是最后一个元素，需要重新调整堆
                if (i < _items.Count)
                {
                    // 先尝试向上调整（如果新值更小）
                    HeapifyUp(i);
                    // 再尝试向下调整（如果新值更大）
                    HeapifyDown(i);
                }
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// 更新元素（用于A*中的节点更新）
    /// 注意：这个方法通过移除和重新添加来更新，确保堆的正确性
    /// </summary>
    public void UpdateItem(T item)
    {
        // 移除旧节点
        Remove(item);
        // 重新添加（会重新计算位置）
        Add(item);
    }
    
    /// <summary>
    /// 检查元素是否在堆中
    /// </summary>
    public bool Contains(T item)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (ReferenceEquals(_items[i], item))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// 清空堆
    /// </summary>
    public void Clear()
    {
        _items.Clear();
    }
    
    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (_items[index].CompareTo(_items[parentIndex]) < 0)
            {
                Swap(index, parentIndex);
                index = parentIndex;
            }
            else
            {
                break;
            }
        }
    }
    
    private void HeapifyDown(int index)
    {
        while (true)
        {
            int smallest = index;
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;
            
            if (leftChild < _items.Count && _items[leftChild].CompareTo(_items[smallest]) < 0)
            {
                smallest = leftChild;
            }
            
            if (rightChild < _items.Count && _items[rightChild].CompareTo(_items[smallest]) < 0)
            {
                smallest = rightChild;
            }
            
            if (smallest != index)
            {
                Swap(index, smallest);
                index = smallest;
            }
            else
            {
                break;
            }
        }
    }
    
    private void Swap(int indexA, int indexB)
    {
        T temp = _items[indexA];
        _items[indexA] = _items[indexB];
        _items[indexB] = temp;
    }
}
