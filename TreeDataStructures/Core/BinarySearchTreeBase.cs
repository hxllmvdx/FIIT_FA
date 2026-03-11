using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null)
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }

    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => new KeyCollection(this);
    public ICollection<TValue> Values => new ValueCollection(this);

    private class KeyCollection : ICollection<TKey>
    {
        private readonly BinarySearchTreeBase<TKey, TValue, TNode> _tree;
        public KeyCollection(BinarySearchTreeBase<TKey, TValue, TNode> tree) => _tree = tree;

        public int Count => _tree.Count;
        public bool IsReadOnly => true;

        public void Add(TKey item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Remove(TKey item) => throw new NotSupportedException();

        public bool Contains(TKey item) => _tree.ContainsKey(item);

        public void CopyTo(TKey[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Insufficient space");

            foreach (var key in this)
                array[arrayIndex++] = key;
        }

        public IEnumerator<TKey> GetEnumerator()
        {
            foreach (var entry in _tree.InOrder())
                yield return entry.Key;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class ValueCollection : ICollection<TValue>
    {
        private readonly BinarySearchTreeBase<TKey, TValue, TNode> _tree;
        public ValueCollection(BinarySearchTreeBase<TKey, TValue, TNode> tree) => _tree = tree;

        public int Count => _tree.Count;
        public bool IsReadOnly => true;

        public void Add(TValue item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Remove(TValue item) => throw new NotSupportedException();

        public bool Contains(TValue item)
        {
            foreach (var entry in _tree.InOrder())
                if (EqualityComparer<TValue>.Default.Equals(entry.Value, item))
                    return true;
            return false;
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Insufficient space");

            foreach (var value in this)
                array[arrayIndex++] = value;
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            foreach (var entry in _tree.InOrder())
                yield return entry.Value;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }


    public virtual void Add(TKey key, TValue value)
    {
        TNode newNode = CreateNode(key, value);

        if (Root == null)
        {
            Root = newNode;
            Count++;
            OnNodeAdded(newNode);
            return;
        }

        TNode current = Root;

        while (true)
        {
            int result = Comparer.Compare(key, current.Key);

            if (result == 0)
            {
                current.Value = value;
                return;
            }
            else if (result < 0)
            {
                if (current.Left != null)
                {
                    current = current.Left;
                }
                else
                {
                    current.Left = newNode;
                    newNode.Parent = current;
                    break;
                }
            }
            else
            {
                if (current.Right != null)
                {
                    current = current.Right;
                }
                else
                {
                    current.Right = newNode;
                    newNode.Parent = current;
                    break;
                }
            }
        }

        OnNodeAdded(newNode);
        Count++;
    }

    protected TNode FindMin(TNode root)
    {
        while (root.Left != null)
        {
            root = root.Left;
        }

        return root;
    }


    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }


    protected virtual void RemoveNode(TNode node)
    {
        TNode? parent;
        TNode? child;

        if (Count == 0)
        {
            throw new InvalidOperationException("The tree is empty.");
        }

        if (node.Left == null)
        {
            Transplant(node, node.Right);
            parent = node.Right?.Parent;
            child = node.Right;
        }
        else if (node.Right == null)
        {
            Transplant(node, node.Left);
            parent = node.Left?.Parent;
            child = node.Left;
        }
        else
        {
            TNode successor = FindMin(node.Right);

            if (successor.Parent != node)
            {
                Transplant(successor, successor.Right);
                successor.Right = node.Right;
                successor.Right.Parent = successor;
            }

            Transplant(node, successor);
            successor.Left = node.Left;
            successor.Left.Parent = successor;

            parent = successor.Parent;
            child = successor;
        }

        OnNodeRemoved(parent, child);
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;

    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }


    #region Hooks

    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }

    /// <summary>
    /// Вызывается после удаления.
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }

    #endregion


    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);


    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x.Right == null) return;

        TNode? y = x.Right;

        x.Right = y?.Left;
        if (y?.Left != null)
        {
            y.Left.Parent = x;
        }

        y?.Parent = x.Parent;

        if (x.Parent == null)
        {
            Root = y;
        }
        else if (x.IsLeftChild)
        {
            x.Parent.Left = y;
        }
        else
        {
            x.Parent.Right = y;
        }

        y?.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        if (y.Left == null) return;

        TNode? x = y.Left;

        y.Left = x?.Right;
        if (x?.Right != null)
        {
            x.Right.Parent = y;
        }

        x?.Parent = y.Parent;

        if (y.Parent == null)
        {
            Root = x;
        }
        else if (y.IsLeftChild)
        {
            y.Parent.Left = x;
        }
        else
        {
            y.Parent.Right = x;
        }

        x?.Right = y;
        y.Parent = x;
    }

    protected void RotateBigLeft(TNode x)
    {
        if (x.Right == null)
        {
            return;
        }

        RotateRight(x.Right);
        RotateLeft(x);
    }

    protected void RotateBigRight(TNode y)
    {
        if (y.Left == null)
        {
            return;
        }

        RotateLeft(y.Left);
        RotateRight(y);
    }

    protected void RotateDoubleLeft(TNode x)
    {
        RotateBigLeft(x);
    }

    protected void RotateDoubleRight(TNode y)
    {
        RotateBigRight(y);
    }

    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion

    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);

    /// <summary>
    /// Внутренний класс-итератор.
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private class TreeIterator(TNode? _root, TraversalStrategy _strategy) :
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private Stack<(TNode, int, bool)> _stack = new();
        private TreeEntry<TKey, TValue> _current;
        private bool _initialized;

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public TreeEntry<TKey, TValue> Current => _current;
        object IEnumerator.Current => Current;

        private void PushLeftBranch(TNode? startNode, int startDepth)
        {
            var depth = startDepth;
            var current = startNode;

            while (current != null)
            {
                _stack.Push((current, depth, false));
                current = current.Left;
                depth++;
            }
        }

        private void PushRightBranch(TNode? startNode, int startDepth)
        {
            var depth = startDepth;
            var current = startNode;

            while (current != null)
            {
                _stack.Push((current, depth, false));
                current = current.Right;
                depth++;
            }
        }

        private bool MoveNextInOrder()
        {
            if (!_initialized)
            {
                _initialized = true;
                PushLeftBranch(_root, 0);
            }

            if (_stack.Count == 0) return false;

            var (node, depth, _) = _stack.Pop();
            _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
            PushLeftBranch(node.Right, depth + 1);

            return true;
        }

        private bool MoveNextInOrderReverse()
        {
            if (!_initialized)
            {
                _initialized = true;
                PushRightBranch(_root, 0);
            }

            if (_stack.Count == 0) return false;

            var (node, depth, _) = _stack.Pop();
            _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
            PushRightBranch(node.Left, depth + 1);

            return true;
        }

        private bool MoveNextPreOrder()
        {
            if (!_initialized)
            {
                if (_root != null)
                {
                    _stack.Push((_root, 0, false));
                }
                _initialized = true;
            }

            if (_stack.Count == 0) return false;

            var (node, depth, _) = _stack.Pop();
            _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);

            if (node.Right != null) _stack.Push((node.Right, depth + 1, false));
            if (node.Left != null) _stack.Push((node.Left, depth + 1, false));

            return true;
        }

        private bool MoveNextPostOrderReverse()
        {
            if (!_initialized)
            {
                if (_root != null)
                {
                    _stack.Push((_root, 0, false));
                }
                _initialized = true;
            }

            if (_stack.Count == 0) return false;

            var (node, depth, _) = _stack.Pop();
            _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);

            if (node.Left != null) _stack.Push((node.Left, depth + 1, false));
            if (node.Right != null) _stack.Push((node.Right, depth + 1, false));

            return true;
        }

        private bool MoveNextPostOrder()
        {
            if (!_initialized)
            {
                if (_root != null)
                {
                    _stack.Push((_root, 0, false));
                }
                _initialized = true;
            }

            while (_stack.Count > 0)
            {
                var (node, depth, visited) = _stack.Pop();

                if (visited)
                {
                    _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
                    return true;
                }

                _stack.Push((node, depth, true));

                if (node.Right != null) _stack.Push((node.Right, depth + 1, false));
                if (node.Left != null) _stack.Push((node.Left, depth + 1, false));
            }

            return false;
        }

        private bool MoveNextPreOrderReverse()
        {
            if (!_initialized)
            {
                if (_root != null)
                {
                    _stack.Push((_root, 0, false));
                }
                _initialized = true;
            }

            while (_stack.Count > 0)
            {
                var (node, depth, visited) = _stack.Pop();

                if (visited)
                {
                    _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
                    return true;
                }

                _stack.Push((node, depth, true));

                if (node.Left != null) _stack.Push((node.Left, depth + 1, false));
                if (node.Right != null) _stack.Push((node.Right, depth + 1, false));
            }

            return false;
        }

        public bool MoveNext()
        {
            if (_strategy == TraversalStrategy.InOrder)
            {
                return MoveNextInOrder();
            }
            else if (_strategy == TraversalStrategy.InOrderReverse)
            {
                return MoveNextInOrderReverse();
            }
            else if (_strategy == TraversalStrategy.PreOrder)
            {
                return MoveNextPreOrder();
            }
            else if (_strategy == TraversalStrategy.PreOrderReverse)
            {
                return MoveNextPreOrderReverse();
            }
            else if (_strategy == TraversalStrategy.PostOrder)
            {
                return MoveNextPostOrder();
            }
            else if (_strategy == TraversalStrategy.PostOrderReverse)
            {
                return MoveNextPostOrderReverse();
            }

            throw new NotImplementedException("Strategy not implemented");
        }

        public void Reset()
        {
            _stack.Clear();
            _initialized = false;
            _current = default;
        }


        public void Dispose()
        {
            // TODO release managed resources here
        }
    }


    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }

    private class KeyValuePairEnumerator(TreeIterator _iterator) : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        public bool MoveNext() => _iterator.MoveNext();

        public void Reset() => _iterator.Reset();
        public void Dispose() => _iterator.Dispose();

        public KeyValuePair<TKey, TValue> Current =>
            new KeyValuePair<TKey, TValue>(_iterator.Current.Key, _iterator.Current.Value);
        object IEnumerator.Current => Current;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        var treeIterator = new TreeIterator(Root, TraversalStrategy.InOrder);
        return new KeyValuePairEnumerator(treeIterator);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (array.Length - arrayIndex < Count) throw new ArgumentException("Array is too small");

        foreach (var entry in InOrder())
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}
