using System.Collections.Specialized;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace BluetoothTool.Behaviors;

/// <summary>
/// 列表项新增时自动滚动到底部的行为
/// </summary>
public class AutoScrollBehavior : Behavior<ListBox>
{
    /// <summary>
    /// 行为附加到控件时订阅集合变化事件
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();

        // 订阅列表项集合变化（Items 集合始终存在，避免 ItemsSource 绑定时机问题）
        ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged += OnCollectionChanged;
    }

    /// <summary>
    /// 行为从控件分离时取消订阅
    /// </summary>
    protected override void OnDetaching()
    {
        // 取消订阅列表项集合变化
        ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged -= OnCollectionChanged;

        base.OnDetaching();
    }

    /// <summary>
    /// 集合新增项时滚动到最后一项
    /// </summary>
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 仅在新增项时滚动到底部
        if (e.Action == NotifyCollectionChangedAction.Add && AssociatedObject.Items.Count > 0)
        {
            AssociatedObject.ScrollIntoView(AssociatedObject.Items[AssociatedObject.Items.Count - 1]);
        }
    }
}
