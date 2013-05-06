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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using WeifenLuo.WinFormsUI.Docking;

namespace SuperPutty
{
    public partial class frmSuperPutty : Form
    {
		private SessionTreeview m_Sessions;
		private NativeMethods.LowLevelKMProc llkp;
		private NativeMethods.LowLevelKMProc llmp;		
		private static IntPtr kbHookID = IntPtr.Zero;
		private static IntPtr mHookID = IntPtr.Zero;
        private static string _PuttyExe;
		private bool tabsRestored = false;

        public static string PuttyExe
        {
            get { return _PuttyExe; }
            set
            {
                _PuttyExe = value;

                if (File.Exists(value))
                {
                    RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Jim Radford\SuperPuTTY\Settings");
                    key.SetValue("PuTTYExe", value);
                }
            }
        }

        private static string _PscpExe;

        public static string PscpExe
        {
            get { return _PscpExe; }
            set
            {
                _PscpExe = value;

                if (File.Exists(value))
                {
                    RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Jim Radford\SuperPuTTY\Settings");
                    key.SetValue("PscpExe", value);
                }
            }
        }

        public static bool IsScpEnabled
        {
            get { return File.Exists(PscpExe); }
        }

		public WeifenLuo.WinFormsUI.Docking.DockPanel getDockpanel()
		{
			return dockPanel;
		}

        public frmSuperPutty()
        {
            // Get Registry Entry for Putty Exe
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Jim Radford\SuperPuTTY\Settings");
            if (key != null)
            {
                string puttyExe = key.GetValue("PuTTYExe", "").ToString();
                if (File.Exists(puttyExe))
                {
                    PuttyExe = puttyExe;
                }

                string pscpExe = key.GetValue("PscpExe", "").ToString();
                if (File.Exists(pscpExe))
                {
                    PscpExe = pscpExe;
                }
            }

            if (String.IsNullOrEmpty(PuttyExe))
            {
                dlgFindPutty dialog = new dlgFindPutty();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    PuttyExe = dialog.PuttyLocation;
                    PscpExe = dialog.PscpLocation;
                }
            }

            if (String.IsNullOrEmpty(PuttyExe))
            {
                MessageBox.Show("Cannot find PuTTY installation. Please visit http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html to download a copy",
                    "PuTTY Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                System.Environment.Exit(1);
            }

            InitializeComponent();
#if DEBUG
            // Only show the option for the debug log viewer when we're compiled with DEBUG defined.
            debugLogToolStripMenuItem.Visible = true;
#endif            
			RestoreSize();
			RestoreTabs();
			ShowTabs();
			ChangeIcon();
			// open sessions window if no tabs are saved
			if (!tabsRestored) OpenSessionDC();
			BringToFront();

			dockPanel.ActiveDocumentChanged += dockPanel_ActiveDocumentChanged;
			menuStrip.LostFocus += new EventHandler(Menu_LostFocus);
			this.FormClosing += new FormClosingEventHandler(FormClosingMethod);
			llkp = KBHookCallback;
			kbHookID = SetKBHook(llkp);
			llmp = MHookCallback;
			mHookID = SetMHook(llmp);
        }

		private void ChangeIcon()
		{
			if (String.IsNullOrEmpty(frmSuperPutty.PuttyExe) || !File.Exists(frmSuperPutty.PuttyExe)) return;
			try {
				this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(frmSuperPutty.PuttyExe);
			} catch { }
		}

		private void FormClosingMethod(object sender, EventArgs e)
		{
			// save window position
			RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Jim Radford\SuperPuTTY\Settings");
			NativeMethods.RECT rct;
			NativeMethods.GetWindowRect(this.Handle, out rct);
			List<string> value = new List<string>();
			value.Add(rct.Left.ToString());
			value.Add(rct.Top.ToString());
			value.Add(rct.Right.ToString());
			value.Add(rct.Bottom.ToString());
			key.SetValue("Position", value.ToArray(), RegistryValueKind.MultiString);		

			// save tabs
			MemoryStream stream = new MemoryStream();
			dockPanel.SaveAsXml(stream, Encoding.UTF8);
			string text = System.Text.Encoding.UTF8.GetString(stream.GetBuffer());
			string[] textArray = text.Split(new string[] {"\n","\r\n"}, StringSplitOptions.RemoveEmptyEntries);
			key.SetValue("Layout", textArray);

			// free hooks
			NativeMethods.UnhookWindowsHookEx(kbHookID);
			NativeMethods.UnhookWindowsHookEx(mHookID);
		}

		private void RestoreSize()
		{
			RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Jim Radford\SuperPuTTY\Settings");
			string[] value = (string[])key.GetValue("Position");
			if (value == null) return;
			if (value.Length != 4) return;
			NativeMethods.MoveWindow(this.Handle, Convert.ToInt32(value[0]), Convert.ToInt32(value[1]), Convert.ToInt32(value[2]), Convert.ToInt32(value[3]), true);
		}

		private void RestoreTabs()
		{
			RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Jim Radford\SuperPuTTY\Settings");
			string[] value = (string[])key.GetValue("Layout");
			if (value == null) return;
			StringBuilder sb = new StringBuilder();
			foreach (string s in value) sb.Append(s);
			byte[] byteArray = Encoding.UTF8.GetBytes(sb.ToString());
			MemoryStream stream = new MemoryStream(byteArray);
			dockPanel.LoadFromXml(stream, RestoreLayoutFromPersistString);
			DockingSettings();
		}

		private void DockingSettings()
		{
			List<string> sessions = new List<string>();
			System.Collections.IEnumerator i = dockPanel.Contents.GetEnumerator();
			while (i.MoveNext())
			{
				if (!(i.Current is ctlPuttyPanel)) continue;
				ctlPuttyPanel panel = (ctlPuttyPanel)i.Current;
				// use document docking for PuTTY panels
				panel.DockState = DockState.Document;
				panel.DockAreas = DockAreas.Document;
			}
		}

		// Open the session treeview and dock it on the right
		private void OpenSessionDC()
		{
			m_Sessions = new SessionTreeview(this, dockPanel);
			m_Sessions.Show(dockPanel, WeifenLuo.WinFormsUI.Docking.DockState.DockRight);
		}

		private void ShowTabs()
		{
			System.Collections.IEnumerator i = dockPanel.Contents.GetEnumerator();
			while (i.MoveNext())
			{
				if (!(i.Current is ctlPuttyPanel)) continue;
				ctlPuttyPanel panel = (ctlPuttyPanel)i.Current;
				panel.m_AppPanel.ShowPanel();
			}
		}

		public ctlPuttyPanel NewPuttyPanel(string sessionId)
		{
			SessionData session = SuperPuTTY.GetSessionById(sessionId);
			return session == null ? null : NewPuttyPanel(session);
		}

		public ctlPuttyPanel NewPuttyPanel(SessionData sessionData)
		{
			ctlPuttyPanel puttyPanel = null;
			// This is the callback fired when the panel containing the terminal is closed
			// We use this to save the last docking location
			PuttyClosedCallback callback = delegate(bool closed)
			{
				if (puttyPanel != null)
				{
					// save the last dockstate (if it has been changed)
					if (sessionData.LastDockstate != puttyPanel.DockState
						&& puttyPanel.DockState != DockState.Unknown
						&& puttyPanel.DockState != DockState.Hidden)
					{
						sessionData.LastDockstate = puttyPanel.DockState;
						sessionData.SaveToRegistry();
					}

					if (puttyPanel.InvokeRequired)
					{
						this.BeginInvoke((MethodInvoker)delegate()
						{
							puttyPanel.Close();
						});
					}
					else
					{
						puttyPanel.Close();
					}
				}
			};
			puttyPanel = new ctlPuttyPanel(this, sessionData, callback);
			return puttyPanel;
		}

		private IDockContent RestoreLayoutFromPersistString(String persistString)
		{
			if (typeof(SessionTreeview).FullName == persistString)
			{
				// session tree
				return this.m_Sessions;
			}
			else
			{
				// putty session
				ctlPuttyPanel puttyPanel = ctlPuttyPanel.FromPersistString(this, persistString);
				if (puttyPanel != null)
				{
					tabsRestored = true;
					return puttyPanel;
				}
			}
			return null;
		}

		private static IntPtr SetKBHook(NativeMethods.LowLevelKMProc proc)
		{
			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule)
			{
				return NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		private IntPtr KBHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			Console.WriteLine("{0} {1} {2}", nCode, wParam, Marshal.ReadInt32(lParam));
			if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN && IsForegroundWindow(this.Handle))			
			{
				int vkCode = Marshal.ReadInt32(lParam);
				if ((Keys)vkCode == Keys.ShiftKey || (Keys)vkCode == Keys.LShiftKey)
				{
					menuStrip.Visible = true;
					menuStrip.Focus();
				}
			}
			return NativeMethods.CallNextHookEx(kbHookID, nCode, wParam, lParam);
		}

		private static IntPtr SetMHook(NativeMethods.LowLevelKMProc proc)
		{
			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule)
			{
				return NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		private IntPtr MHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0 && (wParam == (IntPtr)NativeMethods.WM_LBUTTONUP || wParam == (IntPtr)NativeMethods.WM_RBUTTONUP) && IsForegroundWindow(this.Handle))
			{
				this.BringToFront();
				if (!Menu_IsMouseOver()) dockPanel.Focus();
			}
			return NativeMethods.CallNextHookEx(mHookID, nCode, wParam, lParam);
		}

		private static bool IsForegroundWindow(IntPtr parent)
		{
			if (parent == NativeMethods.GetForegroundWindow()) return true;
			List<IntPtr> result = new List<IntPtr>();
			GCHandle listHandle = GCHandle.Alloc(result);
			try
			{
				NativeMethods.EnumWindowProc childProc = new NativeMethods.EnumWindowProc(EnumWindow);
				NativeMethods.EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
			}
			finally
			{
				if (listHandle.IsAllocated)
					listHandle.Free();
			}
			return result.Count > 0;
		}

		private static bool EnumWindow(IntPtr handle, IntPtr pointer)
		{
			GCHandle gch = GCHandle.FromIntPtr(pointer);
			List<IntPtr> list = gch.Target as List<IntPtr>;
			if (handle == NativeMethods.GetForegroundWindow()) list.Add(handle);
			if (list.Count == 0) return true; else return false;
		}

		private void Menu_LostFocus(object sender, EventArgs e)
		{
			menuStrip.Visible = false;
		}

		private bool Menu_IsMouseOver()
		{
			System.Drawing.Point point = this.PointToClient(Form.MousePosition);
			Region region = new Region(new Rectangle(menuStrip.Left, menuStrip.Top, menuStrip.Width, menuStrip.Height));
			System.Collections.IEnumerator i = menuStrip.Items.GetEnumerator();
			while (i.MoveNext())
			{
				ToolStripMenuItem item = (ToolStripMenuItem)i.Current;
				if (item.Pressed) region.Union(new Region(new Rectangle(item.Bounds.Left, item.Bounds.Top, item.Width, item.Height)));
				System.Collections.IEnumerator j = item.DropDownItems.GetEnumerator();
				while (j.MoveNext())
				{
					if (!(j.Current is ToolStripMenuItem)) continue;
					ToolStripMenuItem subItem = (ToolStripMenuItem)j.Current;
					if (subItem.Pressed) region.Union(new Region(new Rectangle(subItem.Bounds.Left, subItem.Bounds.Top + item.Height, subItem.Width, subItem.Height)));
				}
			}
			return region.IsVisible(point);
		}

        /// <summary>
        /// Handles focusing on tabs/windows which host PuTTY
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dockPanel_ActiveDocumentChanged(object sender, EventArgs e)
        {
            if (dockPanel.ActiveDocument is ctlPuttyPanel)
            {
                ctlPuttyPanel p = (ctlPuttyPanel)dockPanel.ActiveDocument;
				p.SetFocusToChildApplication();
            }
        }

        private void frmSuperPutty_Activated(object sender, EventArgs e)
        {
            //dockPanel1_ActiveDocumentChanged(null, null);
        }

        private void aboutSuperPuttyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 about = new AboutBox1();
            about.ShowDialog();
            about = null;
        }

        private void superPuttyWebsiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://code.google.com/p/superputty/");
        }

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + @"\superputty.chm"))
            {
                Process.Start(Application.StartupPath + @"\superputty.chm");
            }
            else
            {
                DialogResult result = MessageBox.Show("Local documentation could not be found. Would you like to view the documentation online instead?", "Documentation Not Found", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Process.Start("http://code.google.com/p/superputty/wiki/Documentation");
                }
            }
        }

        private void puTTYScpLocationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dlgFindPutty dialog = new dlgFindPutty();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                PuttyExe = dialog.PuttyLocation;
                PscpExe = dialog.PscpLocation;
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "XML Files|*.xml";
            saveDialog.FileName = "Sessions.XML";
            saveDialog.InitialDirectory = Application.StartupPath;
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                SessionTreeview.ExportSessionsToXml(saveDialog.FileName);
            }
        }

		private void sessionsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenSessionDC();
		}

        private void importSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "XML Files|*.xml";
            openDialog.FileName = "Sessions.XML";
            openDialog.CheckFileExists = true;
            openDialog.InitialDirectory = Application.StartupPath;
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                SessionTreeview.ImportSessionsFromXml(openDialog.FileName);
                m_Sessions.LoadSessions();
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Exit SuperPuTTY?", "Confirm Exit", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Yes)
            {
                System.Environment.Exit(0);
            }
        }

        private void puTTYConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = PuttyExe;
            p.Start();
        }

        private void debugLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DebugLogViewer logView = new DebugLogViewer();
            logView.Show(dockPanel, WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide);
        }

    }
}
