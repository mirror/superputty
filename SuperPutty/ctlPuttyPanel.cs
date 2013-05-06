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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Web;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace SuperPutty
{
    public partial class ctlPuttyPanel : ToolWindow
    {
        private string ApplicationName = String.Empty;
        private string ApplicationParameters = String.Empty;
		public frmSuperPutty m_Parent;
		public ApplicationPanel m_AppPanel;
        public SessionData m_Session;
        private PuttyClosedCallback m_ApplicationExit;
        public ctlPuttyPanel(frmSuperPutty parent, SessionData session, PuttyClosedCallback callback)
        {
			m_Parent = parent;
            m_Session = session;
            m_ApplicationExit = callback;

            string args = "-" + session.Proto.ToString().ToLower() + " ";            
            args += (!String.IsNullOrEmpty(m_Session.Password) && m_Session.Password.Length > 0) ? "-pw " + m_Session.Password + " " : "";
            args += "-P " + m_Session.Port + " ";
            args += (!String.IsNullOrEmpty(m_Session.PuttySession)) ? "-load \"" + m_Session.PuttySession + "\" " : "";
            args += (!String.IsNullOrEmpty(m_Session.Username) && m_Session.Username.Length > 0) ? m_Session.Username + "@" : "";
            args += m_Session.Host;
            ApplicationParameters = args;
            InitializeComponent();
			Text = session.SessionName;
            CreatePanel();
        }

		override protected void OnFormClosed(
			FormClosedEventArgs e
		) {
			// save the last dockstate (if it has been changed)
			if (m_Session.LastDockstate != this.DockState
				&& this.DockState != DockState.Unknown
				&& this.DockState != DockState.Hidden)
			{
				m_Session.LastDockstate = this.DockState;
				m_Session.SaveToRegistry();
			}
		}

        private void CreatePanel()
        {
			this.m_AppPanel = new ApplicationPanel(this);
			this.SuspendLayout();
            this.m_AppPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_AppPanel.ApplicationName = frmSuperPutty.PuttyExe;
            this.m_AppPanel.ApplicationParameters = this.ApplicationParameters;
            this.m_AppPanel.Location = new System.Drawing.Point(0, 0);
            this.m_AppPanel.Name = "applicationControl1";
			this.m_AppPanel.Size = new System.Drawing.Size(600, 300);
			this.m_AppPanel.TabIndex = 0;
			this.m_AppPanel.m_CloseCallback = this.m_ApplicationExit;
            this.Controls.Add(this.m_AppPanel);
			this.ResumeLayout();
        }

		void CreateMenu()
		{
			newSessionToolStripMenuItem.DropDownItems.Clear();
			foreach (SessionData session in SuperPuTTY.LoadSessions())
			{
				ToolStripMenuItem newSessionTSMI = new ToolStripMenuItem();
				newSessionTSMI.Tag = session;
				newSessionTSMI.Text = session.SessionName;
				newSessionTSMI.Click += new System.EventHandler(newSessionTSMI_Click);
				newSessionToolStripMenuItem.DropDownItems.Add(newSessionTSMI);
			}
		}

		protected override string GetPersistString()
		{
			string str = String.Format("{0}?SessionId={1}", this.GetType().FullName, HttpUtility.UrlEncodeUnicode(this.m_Session.SessionName));
			return str;
		}

		public static ctlPuttyPanel FromPersistString(frmSuperPutty parent, String persistString)
		{
			ctlPuttyPanel panel = null;
			if (persistString.StartsWith(typeof(ctlPuttyPanel).FullName))
			{
				int idx = persistString.IndexOf("?");
				if (idx != -1)
				{
					NameValueCollection data = HttpUtility.ParseQueryString(persistString.Substring(idx + 1));
					string sessionId = data["SessionId"];
					panel = parent.NewPuttyPanel(sessionId);
				}
				else
				{
					idx = persistString.IndexOf(":");
					if (idx != -1)
					{
						string sessionId = persistString.Substring(idx + 1);
						panel = parent.NewPuttyPanel(sessionId);
					}
				}
			}
			return panel;
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			CreateMenu();
		}

		private void newSessionTSMI_Click(object sender, EventArgs e)
		{
			SessionData session = (SessionData)((ToolStripMenuItem)sender).Tag;
			ctlPuttyPanel sessionPanel = new ctlPuttyPanel(m_Parent, session, null);
			sessionPanel.Show(this.DockPanel, session.LastDockstate);
		}

		private void duplicateSessionTSMI_Click(object sender, EventArgs e)
		{
			ctlPuttyPanel sessionPanel = new ctlPuttyPanel(m_Parent, m_Session, null);
			sessionPanel.Show(this.DockPanel, this.m_Session.LastDockstate);
		}

		private void puTTYMenuTSMI_Click(object sender, EventArgs e)
        {
			NativeMethods.SendMessage(m_AppPanel.AppWin, NativeMethods.WM_SYSCOMMAND, Convert.ToUInt32(((ToolStripMenuItem)sender).Tag.ToString(), 16), 0);
			m_Parent.BringToFront();
        }

        private void closeSessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Reset the focus to the child application window
        /// </summary>
        internal void SetFocusToChildApplication()
        {
			m_AppPanel.ReFocusPuTTY();
        }
    }
}
