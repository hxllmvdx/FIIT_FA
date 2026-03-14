using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(
            TreapNode<TKey, TValue>? root,
            TKey key,
            bool placeEqualLeft)
    {
        if (root == null)
            return (null, null);

        int cmp = Comparer<TKey>.Default.Compare(key, root.Key);

        if (placeEqualLeft)
        {
            if (cmp < 0)
            {
                var (left, right) = Split(root.Left, key, true);
                root.Left = right;
                return (left, root);
            }
            else
            {
                var (left, right) = Split(root.Right, key, true);
                root.Right = left;
                return (root, right);
            }
        }
        else
        {
            if (cmp <= 0)
            {
                var (left, right) = Split(root.Left, key, false);
                root.Left = right;
                return (left, root);
            }
            else
            {
                var (left, right) = Split(root.Right, key, false);
                root.Right = left;
                return (root, right);
            }
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    public virtual TreapNode<TKey, TValue>? Merge(
        TreapNode<TKey, TValue>? left,
        TreapNode<TKey, TValue>? right)
    {
        if (left == null)
            return right;
        if (right == null)
            return left;

        if (left.Priority > right.Priority)
        {
            var (rightLeft, rightRight) = Split(right, left.Key, placeEqualLeft: false);

            left.Left = Merge(left.Left, rightLeft);
            left.Right = Merge(left.Right, rightRight);

            return left;
        }
        else
        {
            var (leftLeft, leftRight) = Split(left, right.Key, placeEqualLeft: false);

            right.Left = Merge(leftLeft, right.Left);
            right.Right = Merge(leftRight, right.Right);

            return right;
        }
    }


    public override void Add(TKey key, TValue value)
    {
        var (left, rest) = Split(Root, key, placeEqualLeft: false);

        var (middle, right) = Split(rest, key, placeEqualLeft: true);

        if (middle != null)
        {
            middle.Value = value;
            Root = Merge(Merge(left, middle), right);
        }
        else
        {
            var newNode = CreateNode(key, value);

            OnNodeAdded(newNode);
            Count++;

            Root = Merge(Merge(left, newNode), right);
        }
    }

    public override bool Remove(TKey key)
    {
        var (left, rest) = Split(Root, key, placeEqualLeft: false);

        var (middle, right) = Split(rest, key, placeEqualLeft: true);

        if (middle == null)
        {
            Root = Merge(left, rest);
            return false;
        }
        else
        {
            OnNodeRemoved(middle.Parent, middle);
            Count--;

            Root = Merge(left, right);
            return true;
        }
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode) { }

    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child) { }

}
