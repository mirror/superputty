/*
 * Copyright (c) 2009 Jim Radford http://www.jimradford.com
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
using System.Windows.Forms;
using System.Threading;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Win32;
using WeifenLuo.WinFormsUI.Docking;

namespace SuperPutty
{
	static class SuperPuTTY
    {
		static Dictionary<string, SessionData> sessions = new Dictionary<string, SessionData>();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool onlyInstance = false;
            Mutex mutex = new Mutex(true, "SuperPutty", out onlyInstance);
            if (!onlyInstance)
            {

            }
            
#if DEBUG
            Logger.OnLog += delegate(string logMessage)
            {
                Console.WriteLine(logMessage);
            };
#endif

			LoadSessions();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmSuperPutty());
        }

		public static bool GetSettingBool(string name)
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Jim Radford\SuperPuTTY\Settings");
			if (key == null) return false;
			return ((int)key.GetValue(name, 0) == 1 ? true : false);			
		}

		public static void SetSettingBool(string name, bool value)
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Jim Radford\SuperPuTTY\Settings", true);
			if (key == null) return;
			key.SetValue(name, (value ? 1 : 0));
		}

		/// <summary>
		/// Encrypt string
		/// </summary>
		public static byte[] EncryptString(string s)
		{
			byte[] plainBytes = Encoding.Unicode.GetBytes(s);
			return ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
		}

		/// <summary>
		/// Decrypt binary
		/// </summary>
		public static string DecryptString(object o)
		{
			try {
				byte[] b = (byte[])o;
				byte[] decryptedData = ProtectedData.Unprotect(b, null, DataProtectionScope.CurrentUser);
				return Encoding.Unicode.GetString(decryptedData);
			}
			catch { }
			return "";
		}

		/// <summary>
		/// Read any existing saved sessions from the registry, decode and populat a list containing the data
		/// </summary>
		/// <returns>A list containing the entries retrieved from the registry</returns>
		public static List<SessionData> LoadSessions()
		{
			sessions.Clear();
			List<SessionData> sessionList = new List<SessionData>();
			RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Jim Radford\SuperPuTTY\Sessions");
			if (key != null)
			{
				string[] sessionKeys = key.GetSubKeyNames();
				foreach (string session in sessionKeys)
				{
					SessionData sessionData = new SessionData();
					RegistryKey itemKey = key.OpenSubKey(session);
					if (itemKey != null)
					{
						sessionData.Host = (string)itemKey.GetValue("Host", "");
						sessionData.Port = (int)itemKey.GetValue("Port", 22);
						sessionData.Proto = (ConnectionProtocol)Enum.Parse(typeof(ConnectionProtocol), (string)itemKey.GetValue("Proto", "SSH"));
						sessionData.PuttySession = (string)itemKey.GetValue("PuttySession", "Default Session");
						sessionData.SessionName = session;
						sessionData.Username = (string)itemKey.GetValue("Login", "");
						sessionData.Password = DecryptString(itemKey.GetValue("Password", ""));
						sessionData.LastDockstate = (DockState)itemKey.GetValue("Last Dock", DockState.Document);
						sessionData.AutoStartSession = bool.Parse((string)itemKey.GetValue("Auto Start", "False"));
						sessionList.Add(sessionData);
						if (!sessions.ContainsKey(session)) sessions.Add(session, sessionData);
					}
				}
			}
			return sessionList;
		}

		public static SessionData GetSessionById(string sessionId)
		{
			SessionData session = null;
			//if (sessionId != null)
			//{
			sessions.TryGetValue(sessionId, out session);
			//}
			return session;
		}
    }
}
