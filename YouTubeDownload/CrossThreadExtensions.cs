using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


// https://stackoverflow.com/questions/10775367/cross-thread-operation-not-valid-control-textbox1-accessed-from-a-thread-othe


namespace YouTubeDownload
{
    /* Sample Usage:
        _frmx.PerformSafely(() => _frmx.Show());
        _frmx.PerformSafely(() => _frmx.Location = new Point(x,y)); */

    public static class CrossThreadExtensions
    {
        public static void PerformSafely(this Control target, Action action)
        {
            if (target.InvokeRequired)
            {
                target.Invoke(action);
            }
            else
            {
                action();
            }
        }

        public static void PerformSafely<T1>(this Control target, Action<T1> action, T1 parameter)
        {
            if (target.InvokeRequired)
            {
                target.Invoke(action, parameter);
            }
            else
            {
                action(parameter);
            }
        }

        public static void PerformSafely<T1, T2>(this Control target, Action<T1, T2> action, T1 p1, T2 p2)
        {
            if (target.InvokeRequired)
            {
                target.Invoke(action, p1, p2);
            }
            else
            {
                action(p1, p2);
            }
        }
    } // end of class CrossThreadExtensions

} // end of namespace
