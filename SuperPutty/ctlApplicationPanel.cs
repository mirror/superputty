/*
 * Copyright (c) 2009 Jim Radford http://www.jimradford.com
 * Copyright (c) 2012 John Peterson
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions: 
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;

namespace SuperPutty
{
    public delegate void PuttyClosedCallback(bool error);

    public class ApplicationPanel : System.Windows.Forms.Panel
    {
        #region Private Member Variables
		private ctlPuttyPanel m_Parent;
        private Process m_Process;
        private IntPtr m_AppWin;
        private string m_ApplicationName = "";
        private string m_ApplicationParameters = "";
		private bool m_Created = false;
		private FormWindowState fwsCache = FormWindowState.Normal;
        internal PuttyClosedCallback m_CloseCallback;

		private NativeMethods.CallBackPtr callBackPtr;

        /// <summary>Set the name of the application executable to launch</summary>
        [Category("Data"), Description("The path/file to launch"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string ApplicationName
        {
            get { return m_ApplicationName; }
            set { m_ApplicationName = value; }
        }
        
        [Category("Data"), Description("The parameters to pass to the application being launched"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string ApplicationParameters
        {
            get { return m_ApplicationParameters; }
            set { m_ApplicationParameters = value; }
        }

		[Category("Data"), Description("The application window handle"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public IntPtr AppWin
		{
			get { return m_AppWin; }
			set { m_AppWin = value; }
		}

		public ApplicationPanel(ctlPuttyPanel parent)
		{
			m_Parent = parent;
		}

		public static string GetClassText(IntPtr hWnd)
		{
			int nRet;
			StringBuilder ClassName = new StringBuilder(100);
			nRet = NativeMethods.GetClassName(hWnd, ClassName, ClassName.Capacity);
			if (nRet != 0)
				return ClassName.ToString();
			else
				return "";
		}

		private bool EnumWindowsCallback(int hwnd, int lParam)
		{
			if (m_Process == null) return true;
			uint lpdwProcessId;
			NativeMethods.GetWindowThreadProcessId(new IntPtr(hwnd), out lpdwProcessId);
			if (m_Process.Id != lpdwProcessId || GetClassText(new IntPtr(hwnd)) != "PuTTY") return true;
			m_AppWin = new IntPtr(hwnd);
			return true;
		}

		private void GetMainWindowHandle()
		{
			while (true)
			{
				callBackPtr = new NativeMethods.CallBackPtr(EnumWindowsCallback);
				NativeMethods.EnumWindows(callBackPtr, 0);
				if (m_AppWin != IntPtr.Zero) return;
				Thread.Sleep(10);
			}
		}

		public void ShowPanel()
		{
			// Set the application as a child of the parent form
			NativeMethods.SetParent(m_AppWin, this.Handle);

			// Show it! (must be done before we set the windows visibility parameters below             
			NativeMethods.ShowWindow(m_AppWin, NativeMethods.SW_MAXIMIZE);	

			// set window style
			long lStyle = NativeMethods.GetWindowLong(m_AppWin, NativeMethods.GWL_STYLE);
			lStyle &= ~(NativeMethods.WS_BORDER | NativeMethods.WS_THICKFRAME);
			NativeMethods.SetWindowLong(m_AppWin, NativeMethods.GWL_STYLE, lStyle);

			// fill parent
			NativeMethods.MoveWindow(m_AppWin, 0, 0, this.Width, this.Height, true);     
		}
        #endregion

        #region Base Overrides       
        /// <summary>
        /// Force redraw of control when size changes
        /// </summary>
        /// <param name="e">Not used</param>
        protected override void OnSizeChanged(EventArgs e)
        {
            this.Invalidate();
            base.OnSizeChanged(e);
        }
       
        public bool ReFocusPuTTY()
        {
			return (m_AppWin != null
				&& NativeMethods.GetForegroundWindow() != m_AppWin
				&& !NativeMethods.SetForegroundWindow(m_AppWin));
        }

        /// <summary>
        /// Create (start) the hosted application when the parent becomes visible
        /// </summary>
        /// <param name="e">Not used</param>
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (!m_Created && !String.IsNullOrEmpty(ApplicationName)) // only allow one instance of the child
            {
                m_Created = true;
                m_AppWin = IntPtr.Zero;

                try {
                    m_Process = new Process();
                    m_Process.EnableRaisingEvents = true;
                    //m_Process.Exited += new EventHandler(p_Exited);
                    m_Process.StartInfo.FileName = ApplicationName;
                    m_Process.StartInfo.Arguments = ApplicationParameters;
					m_Process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // show together with main window
                    m_Process.Exited += delegate(object sender, EventArgs ev)
                    {
						if (m_CloseCallback != null) m_CloseCallback(true);
                    };
                    m_Process.Start();
                    // Wait for application to start and become idle
					GetMainWindowHandle();
					ShowPanel();
                }
                catch (InvalidOperationException ex) {
                    /* Possible Causes:
                     * No file name was specified in the Process component's StartInfo.
                     * -or-
                     * The ProcessStartInfo.UseShellExecute member of the StartInfo property is true while ProcessStartInfo.RedirectStandardInput, 
                     * ProcessStartInfo.RedirectStandardOutput, or ProcessStartInfo.RedirectStandardError is true. 
                     */
                    MessageBox.Show(this, ex.Message, "Invalid Operation Error");
                    throw;
                }
                catch (Win32Exception ex) {
                    /*
                     * Checks are elsewhere to ensure these don't occur, but incase they do we're gonna bail with a nasty exception
                     * which will hopefully send users kicking and screaming at me to fix this (And hopefully they will include a 
                     * stacktrace!)
                     */
					if (ex.NativeErrorCode == NativeMethods.ERROR_ACCESS_DENIED)
                    {
                        throw;
                    }
					else if (ex.NativeErrorCode == NativeMethods.ERROR_FILE_NOT_FOUND)
                    {
                        throw;
                    }
                }
            }                  
            base.OnVisibleChanged(e);
        }
        
        /// <summary>
        /// Send a close message to the hosted application window when the parent is destroyed
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (m_AppWin != IntPtr.Zero)
            {
				m_Process.Close();
				m_AppWin = IntPtr.Zero;
            }
            base.OnHandleDestroyed(e);
        }

        /// <summary>
        /// Refresh the hosted applications window when the parent changes size
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
			if (this.m_AppWin == IntPtr.Zero) return;
			Form parent = m_Parent.m_Parent;
			if (parent.Visible == true && parent.WindowState != FormWindowState.Maximized && parent.WindowState != FormWindowState.Minimized
					&& !(fwsCache == FormWindowState.Minimized && parent.WindowState == FormWindowState.Normal))
			{
				NativeMethods.MoveWindow(m_AppWin, 0, 0, this.Width, this.Height, true);
			}
			fwsCache = parent.WindowState;
			base.OnResize(e);
        }

        #endregion
    }

}
