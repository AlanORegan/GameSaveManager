using System.Drawing;
using System.Windows.Forms;

public static class MessageUtils
{
    // Log level:
    // 0 - Red      - Error     - Reports only Error messagess
    // 1 - Blue     - User      - Reports User (Task complete) and Error messages
    // 2 - Green    - Info      - SubTask complete messages, User messages and Error messages
    // 3 - Grey     - Log info  - Log and all other
    
    private static int LogLevel = 1;
    public static void SetErrorMessage(Label label, string message)
    {
        appendMessage(label, message);
        label.ForeColor = Color.Red;
    }

    public static void SetUserMessage(Label label, string message)
    {
        if (label != null && LogLevel >= 1 && label.ForeColor != Color.Red)
        {
            appendMessage(label, message);

            if (label.ForeColor != Color.Red)
            {
                label.ForeColor = Color.Blue;
            }
        }
    }

    public static void SetInfoMessage(Label label, string message)
    {
        if (label != null && LogLevel >= 2)
        {
            appendMessage(label, message);

            if (label.ForeColor != Color.Red && label.ForeColor != Color.Blue)
            {
                label.ForeColor = Color.Green;
            }
        }
    }

    public static void ClearMessage(Label label)
    {
        if (label != null)
        {
            label.Text = string.Empty;
            label.ForeColor = Color.Black; // Reset to default color
        }
        
    }

    private static void appendMessage(Label label, string message)
    {
        if (label != null)
        {
            if (!string.IsNullOrEmpty(label.Text))
            {
                label.Text += "  " + message;
            }
            else
            {
                label.Text = message;
            }
        }
    }
}