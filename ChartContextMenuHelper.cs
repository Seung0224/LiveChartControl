using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommonLibrary.Helpers
{
    public static class ChartContextMenuHelper
    {
        public static void ApplyDefaultContextMenu(Control chartControl, Action onResetZoom, Action onSaveCsv, Action onSaveImage, Action onResetAll)
        {
            var menu = new ContextMenuStrip();

            if (onResetZoom != null) menu.Items.Add("Zoom Reset", null, (s, e) => onResetZoom());
            if (onSaveCsv != null) menu.Items.Add("Save as CSV", null, (s, e) => onSaveCsv());
            if (onSaveImage != null) menu.Items.Add("Save as Image", null, (s, e) => onSaveImage());
            if (onResetAll != null) menu.Items.Add("Reset All Values", null, (s, e) => onResetAll());

            chartControl.ContextMenuStrip = menu;
        }
    }
}
