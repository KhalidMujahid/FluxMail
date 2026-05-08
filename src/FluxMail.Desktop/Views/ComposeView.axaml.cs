using Avalonia.Controls;
using Avalonia.Interactivity;
using FluxMail.Desktop.ViewModels;

namespace FluxMail.Desktop.Views;

public partial class ComposeView : UserControl
{
    private SendLogWindow? _logWindow;

    public ComposeView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ComposeViewModel vm)
        {
            vm.SendLogOpened += OnSendLogOpened;
            vm.ImageTagReady += OnImageTagReady;
        }
    }

    private void OnSendLogOpened(object? sender, SendLogViewModel logVm)
    {
        _logWindow?.Close();
        _logWindow = new SendLogWindow { DataContext = logVm };
        _logWindow.Show();
    }

    private void OnImageTagReady(object? sender, string tag)
    {
        var tb = Editor;
        if (tb is null) return;
        int s = tb.SelectionStart;
        var t = tb.Text ?? "";
        tb.Text = t[..s] + tag + t[s..];
        tb.SelectionStart = tb.SelectionEnd = s + tag.Length;
        tb.Focus();
    }

    // ── Editor routing ───────────────────────────────────────────────────────

    private bool IsHtmlMode => (DataContext as ComposeViewModel)?.IsHtmlMode ?? true;

    private TextBox? Editor => IsHtmlMode
        ? this.FindControl<TextBox>("HtmlBodyEditor")
        : this.FindControl<TextBox>("RichTextEditor");

    // ── Core helpers ─────────────────────────────────────────────────────────

    private void ApplyTag(string open, string close)
    {
        var tb = Editor;
        if (tb is null) return;

        int s = Math.Min(tb.SelectionStart, tb.SelectionEnd);
        int e = Math.Max(tb.SelectionStart, tb.SelectionEnd);
        var t = tb.Text ?? "";

        if (s == e)
        {
            tb.Text = t[..s] + open + close + t[s..];
            tb.SelectionStart = tb.SelectionEnd = s + open.Length;
        }
        else
        {
            var sel = t[s..e];
            tb.Text = t[..s] + open + sel + close + t[e..];
            tb.SelectionStart = s;
            tb.SelectionEnd = s + open.Length + sel.Length + close.Length;
        }

        tb.Focus();
    }

    private void InsertSelf(string tag)
    {
        var tb = Editor;
        if (tb is null) return;
        int s = tb.SelectionStart;
        var t = tb.Text ?? "";
        tb.Text = t[..s] + tag + t[s..];
        tb.SelectionStart = tb.SelectionEnd = s + tag.Length;
        tb.Focus();
    }

    // ── Text style ───────────────────────────────────────────────────────────

    private void OnFmtBold(object? s, RoutedEventArgs e)      => ApplyTag("<strong>", "</strong>");
    private void OnFmtItalic(object? s, RoutedEventArgs e)    => ApplyTag("<em>", "</em>");
    private void OnFmtUnderline(object? s, RoutedEventArgs e) => ApplyTag("<u>", "</u>");
    private void OnFmtStrike(object? s, RoutedEventArgs e)    => ApplyTag("<s>", "</s>");
    private void OnFmtCode(object? s, RoutedEventArgs e)      => ApplyTag("<code>", "</code>");
    private void OnFmtPre(object? s, RoutedEventArgs e)       => ApplyTag("<pre><code>", "</code></pre>\n");

    // ── Headings ─────────────────────────────────────────────────────────────

    private void OnFmtH1(object? s, RoutedEventArgs e)        => ApplyTag("<h1>", "</h1>\n");
    private void OnFmtH2(object? s, RoutedEventArgs e)        => ApplyTag("<h2>", "</h2>\n");
    private void OnFmtH3(object? s, RoutedEventArgs e)        => ApplyTag("<h3>", "</h3>\n");

    // ── Block elements ───────────────────────────────────────────────────────

    private void OnFmtUL(object? s, RoutedEventArgs e)        => ApplyTag("<ul>\n  <li>", "</li>\n</ul>\n");
    private void OnFmtOL(object? s, RoutedEventArgs e)        => ApplyTag("<ol>\n  <li>", "</li>\n</ol>\n");
    private void OnFmtQuote(object? s, RoutedEventArgs e)     => ApplyTag("<blockquote style=\"border-left:3px solid #ccc;padding-left:12px;color:#555;\">", "</blockquote>\n");
    private void OnFmtHR(object? s, RoutedEventArgs e)        => InsertSelf("<hr style=\"border:none;border-top:1px solid #e5e7eb;margin:20px 0;\"/>\n");
    private void OnFmtLink(object? s, RoutedEventArgs e)      => ApplyTag("<a href=\"\">", "</a>");
    private void OnFmtPara(object? s, RoutedEventArgs e)      => ApplyTag("<p>", "</p>\n");
    private void OnFmtDiv(object? s, RoutedEventArgs e)       => ApplyTag("<div>", "</div>\n");

    // ── Font size ────────────────────────────────────────────────────────────

    private void OnFmtSize12(object? s, RoutedEventArgs e)    => ApplyTag("<span style=\"font-size:12px\">", "</span>");
    private void OnFmtSize14(object? s, RoutedEventArgs e)    => ApplyTag("<span style=\"font-size:14px\">", "</span>");
    private void OnFmtSize16(object? s, RoutedEventArgs e)    => ApplyTag("<span style=\"font-size:16px\">", "</span>");
    private void OnFmtSize18(object? s, RoutedEventArgs e)    => ApplyTag("<span style=\"font-size:18px\">", "</span>");
    private void OnFmtSize20(object? s, RoutedEventArgs e)    => ApplyTag("<span style=\"font-size:20px\">", "</span>");
    private void OnFmtSize24(object? s, RoutedEventArgs e)    => ApplyTag("<span style=\"font-size:24px\">", "</span>");
    private void OnFmtSize28(object? s, RoutedEventArgs e)    => ApplyTag("<span style=\"font-size:28px\">", "</span>");
    private void OnFmtSize32(object? s, RoutedEventArgs e)    => ApplyTag("<span style=\"font-size:32px\">", "</span>");

    // ── Alignment ────────────────────────────────────────────────────────────

    private void OnFmtAlignLeft(object? s, RoutedEventArgs e)    => ApplyTag("<div style=\"text-align:left\">", "</div>\n");
    private void OnFmtAlignCenter(object? s, RoutedEventArgs e)  => ApplyTag("<div style=\"text-align:center\">", "</div>\n");
    private void OnFmtAlignRight(object? s, RoutedEventArgs e)   => ApplyTag("<div style=\"text-align:right\">", "</div>\n");
    private void OnFmtAlignJustify(object? s, RoutedEventArgs e) => ApplyTag("<div style=\"text-align:justify\">", "</div>\n");

    // ── Text color ───────────────────────────────────────────────────────────

    private void OnColorBlack(object? s, RoutedEventArgs e)   => ApplyTag("<span style=\"color:#1F2937\">", "</span>");
    private void OnColorGray(object? s, RoutedEventArgs e)    => ApplyTag("<span style=\"color:#6B7280\">", "</span>");
    private void OnColorRed(object? s, RoutedEventArgs e)     => ApplyTag("<span style=\"color:#EF4444\">", "</span>");
    private void OnColorOrange(object? s, RoutedEventArgs e)  => ApplyTag("<span style=\"color:#F97316\">", "</span>");
    private void OnColorYellow(object? s, RoutedEventArgs e)  => ApplyTag("<span style=\"color:#EAB308\">", "</span>");
    private void OnColorGreen(object? s, RoutedEventArgs e)   => ApplyTag("<span style=\"color:#22C55E\">", "</span>");
    private void OnColorBlue(object? s, RoutedEventArgs e)    => ApplyTag("<span style=\"color:#3B82F6\">", "</span>");
    private void OnColorPurple(object? s, RoutedEventArgs e)  => ApplyTag("<span style=\"color:#A855F7\">", "</span>");
    private void OnColorPink(object? s, RoutedEventArgs e)    => ApplyTag("<span style=\"color:#EC4899\">", "</span>");
    private void OnColorWhite(object? s, RoutedEventArgs e)   => ApplyTag("<span style=\"color:#F8FAFC\">", "</span>");

    // ── Highlight (background color) ─────────────────────────────────────────

    private void OnHighlightYellow(object? s, RoutedEventArgs e) => ApplyTag("<span style=\"background-color:#FEF08A\">", "</span>");
    private void OnHighlightGreen(object? s, RoutedEventArgs e)  => ApplyTag("<span style=\"background-color:#BBF7D0\">", "</span>");
    private void OnHighlightBlue(object? s, RoutedEventArgs e)   => ApplyTag("<span style=\"background-color:#BFDBFE\">", "</span>");
    private void OnHighlightPink(object? s, RoutedEventArgs e)   => ApplyTag("<span style=\"background-color:#FECDD3\">", "</span>");
}
