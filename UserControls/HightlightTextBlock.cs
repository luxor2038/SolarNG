using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SolarNG.UserControls;

public class HightlightTextBlock : TextBlock
{
    private static readonly SolidColorBrush HighlightedBrush = Application.Current.Resources["fg3"] as SolidColorBrush;

    public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register("SearchText", typeof(string), typeof(HightlightTextBlock), new FrameworkPropertyMetadata(null, OnDataChanged));

    public string SearchText
    {
        get
        {
            return (string)GetValue(SearchTextProperty);
        }
        set
        {
            SetValue(SearchTextProperty, value);
        }
    }

    private static void OnDataChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
    {
        TextBlock textBlock = (TextBlock)source;
        if (textBlock.Text.Length != 0)
        {
            string text = textBlock.Text.ToUpper();
            string text2 = ((string)e.NewValue).ToUpper().Replace("SYSTEM.WINDOWS.CONTROLS.TEXTBOX: ", "");
            int num = text.IndexOf(text2, StringComparison.OrdinalIgnoreCase);
            if (num != -1)
            {
                string text3 = textBlock.Text.Substring(0, num);
                string text4 = textBlock.Text.Substring(num, text2.Length);
                string text5 = textBlock.Text.Substring(num + text2.Length, textBlock.Text.Length - (num + text2.Length));
                textBlock.Inlines.Clear();
                Run item = new Run
                {
                    Text = text3
                };
                textBlock.Inlines.Add(item);
                Run item2 = new Run
                {
                    Background = HighlightedBrush,
                    Text = text4
                };
                textBlock.Inlines.Add(item2);
                Run item3 = new Run
                {
                    Text = text5
                };
                textBlock.Inlines.Add(item3);
            }
        }
    }
}
