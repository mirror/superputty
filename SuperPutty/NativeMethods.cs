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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace SuperPutty
{
	class NativeMethods
	{
		#region Win32 Constants/Enums
		public const int
			ERROR_FILE_NOT_FOUND =	2,
			ERROR_ACCESS_DENIED =	5,
			GWL_STYLE =				(-16);
		public const uint
			WH_KEYBOARD_LL =	0x000d,
			WH_MOUSE_LL =		0x000e,
			WS_CAPTION =		0x00C00000,
			WS_BORDER =			0x00800000,
			WS_VSCROLL =		0x00200000,
			WS_THICKFRAME =		0x00040000;
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		[Flags]
		public enum AnimateWindowFlags
		{
			AW_HOR_POSITIVE =	0x00000001,
			AW_HOR_NEGATIVE =	0x00000002,
			AW_VER_POSITIVE =	0x00000004,
			AW_VER_NEGATIVE =	0x00000008,
			AW_CENTER =			0x00000010,
			AW_HIDE =			0x00010000,
			AW_ACTIVATE =		0x00020000,
			AW_SLIDE =			0x00040000,
			AW_BLEND =			0x00080000
		}

		#region ShowWindow Style
		public const uint
			SW_MAXIMIZE =	0x03;
		#endregion
		#region Windows Messages
		public const uint
			WM_CLOSE =		0x0010,
			WM_SYSCOMMAND =	0x0112,
			WM_SYSKEYDOWN =	0x0104,
			WM_LBUTTONUP =	0x0202,
			WM_RBUTTONUP =	0x0205;
		#endregion
		#endregion

		#region Pinvoke/Win32 Methods
		public delegate bool CallBackPtr(int hwnd, int lParam);
		public delegate IntPtr LowLevelKMProc(int nCode, IntPtr wParam, IntPtr lParam);
		public delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("user32")]
		public static extern bool AnimateWindow(IntPtr hwnd, int time, AnimateWindowFlags flags);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("user32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);

		[DllImport("user32.dll")]
		public static extern int EnumWindows(CallBackPtr callPtr, int lPar);

		[DllImport("user32.dll")]
		public static extern IntPtr GetActiveWindow();

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SetWindowsHookEx(uint idHook, LowLevelKMProc lpfn, IntPtr hMod, uint dwThreadId);
	
		[DllImport("user32.dll", SetLastError = true)]
		public static extern long SetParent(IntPtr hWndChild, IntPtr hWndParent);

		[DllImport("user32.dll", EntryPoint = "GetWindowLongA", SetLastError = true)]
		public static extern long GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int cx, int cy, bool repaint);

		[DllImport("user32.dll", EntryPoint = "PostMessageA", SetLastError = true)]
		public static extern bool PostMessage(IntPtr hWnd, uint Msg, long wParam, long lParam);

		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, uint Msg, long wParam, long lParam);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetActiveWindow(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", EntryPoint = "SetWindowLongA", SetLastError = true)]
		public static extern long SetWindowLong(IntPtr hWnd, int nIndex, long dwNewLong);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
		#endregion
	}
}
